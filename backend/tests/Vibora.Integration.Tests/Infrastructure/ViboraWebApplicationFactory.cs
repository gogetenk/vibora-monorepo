using DotNet.Testcontainers.Builders;
using MassTransit;
using MassTransit.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;
using Vibora.Games.Infrastructure.Data;
using Vibora.Notifications.Infrastructure.Data;
using Vibora.Users.Infrastructure.Data;

namespace Vibora.Integration.Tests.Infrastructure;

/// <summary>
/// Custom WebApplicationFactory that uses TestContainers for PostgreSQL
/// and allows mocking external services like Auth0
/// </summary>
public class ViboraWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private PostgreSqlContainer? _postgresContainer;
    private bool _databaseCreated = false;

    public string ConnectionString => _postgresContainer?.GetConnectionString()
        ?? throw new InvalidOperationException("PostgreSQL container not initialized");

    public async Task InitializeAsync()
    {
        // Create and start PostgreSQL container with PostGIS extension
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgis/postgis:17-3.5")
            .WithDatabase("viboradb_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithCleanUp(true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(5432))
            .Build();

        await _postgresContainer.StartAsync();
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        // First, create the host
        var host = base.CreateHost(builder);

        // Create database schema ONLY ONCE per test run
        // Between tests, we use DELETE queries (see IntegrationTestBase.DisposeAsync)
        if (!_databaseCreated)
        {
            using var scope = host.Services.CreateScope();
            var gamesDb = scope.ServiceProvider.GetRequiredService<GamesDbContext>();
            var usersDb = scope.ServiceProvider.GetRequiredService<UsersDbContext>();
            var notificationsDb = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();

            // Apply all migrations (creates database + PostGIS extension + tables + triggers + indexes)
            // Order matters: Games first as it creates PostGIS extension
            gamesDb.Database.Migrate();
            usersDb.Database.Migrate();
            notificationsDb.Database.Migrate();

            _databaseCreated = true;
        }

        return host;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Use Development environment to disable JWT validation (Program.cs lines 61-72)
        builder.UseEnvironment("Development");

        // Configure connection strings for test database
        builder.UseSetting("ConnectionStrings:viboradb", ConnectionString);
        builder.UseSetting("Aspire:Npgsql:EntityFrameworkCore:PostgreSQL:GamesDbContext:ConnectionString", ConnectionString);
        builder.UseSetting("Aspire:Npgsql:EntityFrameworkCore:PostgreSQL:UsersDbContext:ConnectionString", ConnectionString);
        builder.UseSetting("Aspire:Npgsql:EntityFrameworkCore:PostgreSQL:NotificationsDbContext:ConnectionString", ConnectionString);
        builder.UseSetting("Aspire:Npgsql:EntityFrameworkCore:PostgreSQL:ConnectionString", ConnectionString);
        builder.UseSetting("DeploymentMode", "Monolith");
        builder.UseSetting("Jwt:Secret", "test-secret-key-for-integration-tests-only-minimum-256-bits");
        builder.UseSetting("Jwt:Issuer", "https://test.supabase.co/auth/v1");

        // Also add via ConfigureAppConfiguration to ensure it's available
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Insert our configuration at the BEGINNING (highest priority)
            // Do NOT clear sources - just add ours first
            config.Sources.Insert(0, new Microsoft.Extensions.Configuration.Memory.MemoryConfigurationSource
            {
                InitialData = new Dictionary<string, string?>
                {
                    ["ConnectionStrings:viboradb"] = ConnectionString,
                    ["Aspire:Npgsql:EntityFrameworkCore:PostgreSQL:GamesDbContext:ConnectionString"] = ConnectionString,
                    ["Aspire:Npgsql:EntityFrameworkCore:PostgreSQL:UsersDbContext:ConnectionString"] = ConnectionString,
                    ["Aspire:Npgsql:EntityFrameworkCore:PostgreSQL:NotificationsDbContext:ConnectionString"] = ConnectionString,
                    ["Aspire:Npgsql:EntityFrameworkCore:PostgreSQL:ConnectionString"] = ConnectionString,
                    ["DeploymentMode"] = "Monolith",
                    ["Jwt:Secret"] = "test-secret-key-for-integration-tests-only-minimum-256-bits", // Same key as TestJwtGenerator
                    ["Jwt:Issuer"] = "https://test.supabase.co/auth/v1",
                    ["Logging:LogLevel:Default"] = "Warning",
                    ["Logging:LogLevel:Microsoft.AspNetCore"] = "Warning",
                    ["Hangfire:Enabled"] = "false" // Disable Hangfire in tests
                }
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // Keep the production MassTransit configuration (in-memory bus)
            // This allows consumers to actually execute and create notifications in the database
            // Add test harness on top for observation (Published.Any verification)
            services.AddMassTransitTestHarness();

            // Remove Hangfire server (background jobs not needed in tests)
            var hangfireServerDescriptor = services.FirstOrDefault(d =>
                d.ServiceType.FullName == "Hangfire.IBackgroundProcessingServer");
            if (hangfireServerDescriptor != null)
            {
                services.Remove(hangfireServerDescriptor);
            }

            // Configure JSON options to be case-insensitive for tests (allows camelCase in test requests)
            services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
            {
                options.SerializerOptions.PropertyNameCaseInsensitive = true;
            });
            services.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(options =>
            {
                options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
            });
        });
    }

    public new async Task DisposeAsync()
    {
        if (_postgresContainer != null)
        {
            await _postgresContainer.DisposeAsync();
        }
        
        await base.DisposeAsync();
    }
}
