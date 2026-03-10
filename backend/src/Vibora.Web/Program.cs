using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Hangfire;
using Hangfire.PostgreSql;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Sentry;
using Sentry.OpenTelemetry;
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

    // Sentry — error tracking + distributed tracing
    builder.WebHost.UseSentry(options =>
    {
        options.Dsn = builder.Configuration["Sentry:Dsn"] ?? "";
        options.Environment = builder.Environment.EnvironmentName;
        options.TracesSampleRate = 1.0;
        options.SendDefaultPii = false;
        options.Debug = builder.Environment.IsDevelopment();
        options.EnableLogs = true;
        options.UseOpenTelemetry();
    });

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

    // --- SRE Demo endpoints ---
    // Healthy endpoint
    app.MapGet("/api/sre/health-check", () =>
    {
        SentrySdk.Logger.LogInfo("SRE health check OK at {0}", DateTime.UtcNow);
        return Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    });

    // Crash endpoint — NullReferenceException (demo scenario 1)
    app.MapGet("/api/sre/crash", () =>
    {
        SentrySdk.Logger.LogError("Crash endpoint triggered — about to throw NullReferenceException");
        string? value = null;
        return Results.Ok(value!.Length); // NullReferenceException
    });

    // Silent bug endpoint — wrong calculation with warning log (demo scenario 2)
    app.MapGet("/api/sre/calculate-discount", (int price, int quantity) =>
    {
        // BUG: discount should be price * quantity * 0.1, but uses addition instead of multiplication
        var discount = (price + quantity) * 0.1;
        if (discount > price)
        {
            SentrySdk.Logger.LogWarning(
                "Discount {0} exceeds price {1} for quantity {2} — potential calculation error",
                discount, price, quantity);
        }
        return Results.Ok(new { price, quantity, discount, total = price * quantity - discount });
    });

    // User search endpoint — performance degradation with retry storm (demo scenario 3)
    app.MapGet("/api/sre/users/search", (string? query) =>
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var retryCount = 0;
        const int maxRetries = 3;

        while (retryCount < maxRetries)
        {
            try
            {
                // Simulate unstable downstream service call
                Thread.Sleep(1200 + retryCount * 800);

                // 70% chance of failure on each attempt
                if (Random.Shared.NextDouble() < 0.7)
                {
                    retryCount++;
                    SentrySdk.Logger.LogWarning(
                        "User search: downstream timeout on attempt {0}/{1} for query '{2}' — retrying in {3}ms",
                        retryCount, maxRetries, query ?? "null", retryCount * 500);
                    Thread.Sleep(retryCount * 500); // Exponential-ish backoff
                    continue;
                }

                stopwatch.Stop();
                SentrySdk.Logger.LogInfo("User search completed in {0}ms for query '{1}'", stopwatch.ElapsedMilliseconds, query ?? "null");
                return Results.Ok(new { query, results = new[] { "user1@test.com", "user2@test.com" }, responseTimeMs = stopwatch.ElapsedMilliseconds });
            }
            catch (Exception ex)
            {
                SentrySdk.Logger.LogError("User search unexpected error: {0}", ex.Message);
                throw;
            }
        }

        stopwatch.Stop();
        SentrySdk.Logger.LogError(
            "User search: all {0} retries exhausted for query '{1}' — total time {2}ms. Downstream service may be degraded.",
            maxRetries, query ?? "null", stopwatch.ElapsedMilliseconds);
        return Results.Json(new { error = "Service temporarily unavailable", retriesExhausted = maxRetries, totalTimeMs = stopwatch.ElapsedMilliseconds }, statusCode: 503);
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
