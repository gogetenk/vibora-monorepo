using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Hangfire;
using Hangfire.PostgreSql;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using Vibora.Games;
using Vibora.Games.Infrastructure.Services;
using Vibora.Notifications;
using Vibora.Users;
using Vibora.Web.Infrastructure;

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.AddServiceDefaults();

    // Initialize Firebase Admin SDK
    var firebaseCredPath = builder.Configuration["Firebase:CredentialsPath"] ?? "firebase-adminsdk-mock.json";
    if (File.Exists(firebaseCredPath))
    {
        try
        {
            FirebaseApp.Create(new AppOptions
            {
                Credential = GoogleCredential.FromFile(firebaseCredPath)
            });
            Console.WriteLine($"[FIREBASE] Admin SDK initialized from {firebaseCredPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FIREBASE] Warning: Failed to initialize Firebase. Error: {ex.Message}");
        }
    }
    else
    {
        Console.WriteLine($"[FIREBASE] Warning: Firebase credentials file not found at {firebaseCredPath}");
    }

    // Add services to the container
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "Vibora API",
            Version = "v1",
            Description = "Vibora Game Management API"
        });
        
        // Use full type names to avoid schema ID conflicts
        c.CustomSchemaIds(type => type.FullName?.Replace("+", "."));
    });

    // Configure MassTransit InMemory
    builder.Services.AddMassTransit(x =>
    {
        // Register consumers from Games module
        x.AddConsumers(typeof(GamesModuleServiceRegistrar).Assembly);

        // Register consumers from Notifications module
        NotificationsModuleServiceRegistrar.ConfigureNotificationsConsumers(x);

        x.UsingInMemory((context, cfg) =>
        {
            cfg.ConfigureEndpoints(context);
        });
    });

    // Configure Hangfire - Background jobs for game reminders
    // Can be disabled in tests with Hangfire:Enabled = false
    var hangfireEnabled = builder.Configuration.GetValue<bool>("Hangfire:Enabled", defaultValue: true);
    if (hangfireEnabled)
    {
        var hangfireConnectionString = builder.Configuration.GetConnectionString("viboradb");
        builder.Services.AddHangfire(config =>
        {
            config.UsePostgreSqlStorage(hangfireConnectionString);
            config.SetDataCompatibilityLevel(CompatibilityLevel.Version_170);
            config.UseSimpleAssemblyNameTypeSerializer();
            config.UseRecommendedSerializerSettings();
        });
        builder.Services.AddHangfireServer();
    }

    // Global Exception Handler
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

    // Configure JWT Secret and Logging
    var jwtSecret = builder.Configuration["Jwt:Secret"];
    Console.WriteLine($"[JWT CONFIG] JWT Secret configured: {!string.IsNullOrEmpty(jwtSecret)}");
    Console.WriteLine($"[JWT CONFIG] Environment: {builder.Environment.EnvironmentName}");

    if (string.IsNullOrEmpty(jwtSecret))
    {
        // In tests or development without configuration, log a warning and use a default test key
        // This prevents the app from crashing during test initialization
        builder.Logging.AddConsole().AddFilter(level => level >= LogLevel.Warning);
        jwtSecret = "test-secret-key-for-integration-tests-only-minimum-256-bits";
        Console.WriteLine($"[JWT CONFIG] Using fallback test key");
    }
    else
    {
        Console.WriteLine($"[JWT CONFIG] Using configured key (first 10 chars): {jwtSecret.Substring(0, Math.Min(10, jwtSecret.Length))}");
    }

    // JWT Authentication from Supabase
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {

            options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    System.Text.Encoding.UTF8.GetBytes(jwtSecret)),
                ValidateIssuer = false, // Supabase doesn't require strict issuer validation
                ValidateAudience = false, // Supabase doesn't require strict audience validation
                ValidateLifetime = false, // Disable for now to debug
                ClockSkew = TimeSpan.FromMinutes(5)
            };

            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    // Log authentication failures for debugging
                    Console.WriteLine($"[JWT AUTH FAILED] {context.Exception?.Message}");
                    Console.WriteLine($"[JWT AUTH FAILED] Exception: {context.Exception}");
                    return Task.CompletedTask;
                },
                OnTokenValidated = context =>
                {
                    var externalId = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                    Console.WriteLine($"[JWT AUTH SUCCESS] User: {externalId}");
                    return Task.CompletedTask;
                },
                OnForbidden = context =>
                {
                    Console.WriteLine($"[JWT AUTH FORBIDDEN] Access forbidden: {context?.Request?.Path}");
                    return Task.CompletedTask;
                }
            };
        });

    builder.Services.AddAuthorization();

    // Configure Output Cache (in-memory caching for GET requests)
    // Module-specific policies are configured in each module's ServiceRegistrar
    builder.Services.AddOutputCache();

    // Configure CORS for Frontend
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend", policy =>
        {
            policy.WithOrigins(
                    "http://localhost:3000",
                    "http://localhost:3001",
                    "https://localhost:3000",
                    "https://localhost:3001"
                )
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        });
    });

    // Register Modules (Ardalis pattern)
    builder.Services.AddUsersModule(builder);
    builder.Services.AddGamesModule(builder);
    builder.Services.AddNotificationsModule(builder);
    // builder.Services.AddCommunicationModule(builder);

    // Configure cross-module communication strategy (Monolith vs Microservices)
    ConfigureCrossModuleCommunication(builder.Services, builder.Configuration);

    var app = builder.Build();

    app.MapDefaultEndpoints();

    // Configure middleware pipeline
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Vibora API v1");
            c.RoutePrefix = "swagger"; // URL: /swagger
        });
    }

    app.UseExceptionHandler();
    app.UseHttpsRedirection();

    // CORS - MUST be before Authentication
    app.UseCors("AllowFrontend");

    // Authentication & Authorization
    app.UseAuthentication();
    app.UseAuthorization();

    // Output Cache - MUST be after Authentication to access User claims
    app.UseOutputCache();

    // Configure Hangfire Dashboard (only if enabled)
    var isHangfireEnabled = app.Configuration.GetValue<bool>("Hangfire:Enabled", defaultValue: true);
    if (isHangfireEnabled)
    {
        app.UseHangfireDashboard("/hangfire");

        // Register recurring Hangfire jobs - game reminders every 5 minutes
        RecurringJob.AddOrUpdate<GameReminderService>(
            "game-reminders-2h",
            service => service.PublishGameRemindersAsync(),
            "*/5 * * * *");
    }

    // Map Module Endpoints (Ardalis pattern)
    app.MapUsersEndpoints();
    app.MapGamesEndpoints();
    app.MapNotificationsEndpoints();
    // app.MapCommunicationEndpoints();

    // SRE Demo Endpoints for testing error monitoring
    var sreGroup = app.MapGroup("/api/sre").WithTags("SRE");

    // Endpoint that demonstrates SRE health check
    sreGroup.MapGet("/crash", () =>
    {
        return Results.Ok(new { status = "ok", message = "SRE crash endpoint fixed", timestamp = DateTime.UtcNow });
    });

    // Endpoint for calculating discount on product purchases
    sreGroup.MapPost("/calculate-discount", (decimal price, int quantity) =>
    {
        // Calculate discount: (price * quantity) * 0.1
        var discount = (price * quantity) * 0.1m;
        return Results.Ok(new { discount = discount });
    });

    await app.RunAsync();

    // Configure cross-module communication strategy
    static void ConfigureCrossModuleCommunication(IServiceCollection services, IConfiguration configuration)
    {
        var deploymentMode = configuration.GetValue<string>("DeploymentMode") ?? "Monolith";

        if (deploymentMode.Equals("Monolith", StringComparison.OrdinalIgnoreCase))
        {
            // MONOLITH MODE: Direct in-process calls via MediatR

            // IUsersServiceClient: Used by Games module (for user metadata)
            services.AddScoped<Vibora.Users.Contracts.Services.IUsersServiceClient,
                Vibora.Users.Infrastructure.Services.UsersServiceInProcessClient>();

            // IGamesServiceClient: Used by Users module (for guest conversion during signup)
            services.AddScoped<Vibora.Games.Contracts.Services.IGamesServiceClient,
                Vibora.Games.Infrastructure.Services.GamesServiceInProcessClient>();
        }
        else if (deploymentMode.Equals("Microservices", StringComparison.OrdinalIgnoreCase))
        {
            // MICROSERVICES MODE: HTTP calls to separate services

            var usersServiceUrl = configuration.GetValue<string>("Services:UsersService:Url")
                ?? "http://localhost:5001";
            var gamesServiceUrl = configuration.GetValue<string>("Services:GamesService:Url")
                ?? "http://localhost:5002";

            // IUsersServiceClient: HTTP client for Users service
            services.AddHttpClient<Vibora.Users.Contracts.Services.IUsersServiceClient,
                Vibora.Users.Infrastructure.Services.UsersServiceHttpClient>(client =>
            {
                client.BaseAddress = new Uri(usersServiceUrl);
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            // IGamesServiceClient: HTTP client for Games service
            services.AddHttpClient<Vibora.Games.Contracts.Services.IGamesServiceClient,
                Vibora.Games.Contracts.Services.GamesServiceHttpClient>(client =>
            {
                client.BaseAddress = new Uri(gamesServiceUrl);
                client.Timeout = TimeSpan.FromSeconds(30);
            });
        }
        else
        {
            throw new InvalidOperationException($"Invalid DeploymentMode '{deploymentMode}'. Valid values: 'Monolith', 'Microservices'");
        }
    }
}
catch (Exception exc)
{
    throw;
}

// Expose Program class for WebApplicationFactory in integration tests
public partial class Program { }
