using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Vibora.Games.Infrastructure.Data;
using Vibora.Notifications.Infrastructure.Data;
using Vibora.Users.Infrastructure.Data;

namespace Vibora.MigrationService;

public class Worker : BackgroundService
{
    public const string ActivitySourceName = "Migrations";
    private static readonly ActivitySource s_activitySource = new(ActivitySourceName);

    private readonly IServiceProvider _serviceProvider;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly ILogger<Worker> _logger;

    public Worker(
        IServiceProvider serviceProvider,
        IHostApplicationLifetime hostApplicationLifetime,
        ILogger<Worker> logger)
    {
        _serviceProvider = serviceProvider;
        _hostApplicationLifetime = hostApplicationLifetime;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var activity = s_activitySource.StartActivity("Migrating database", ActivityKind.Client);

        _logger.LogInformation("Starting database migration service at {Time}", DateTimeOffset.Now);

        try
        {
            await using var scope = _serviceProvider.CreateAsyncScope();

            var gamesDbContext = scope.ServiceProvider.GetRequiredService<GamesDbContext>();
            var usersDbContext = scope.ServiceProvider.GetRequiredService<UsersDbContext>();
            var notificationsDbContext = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();

            // Apply migrations for Games module
            await ApplyMigrationsAsync(gamesDbContext, "GamesDbContext", stoppingToken);

            // Apply migrations for Users module
            await ApplyMigrationsAsync(usersDbContext, "UsersDbContext", stoppingToken);

            // Apply migrations for Notifications module
            await ApplyMigrationsAsync(notificationsDbContext, "NotificationsDbContext", stoppingToken);

            _logger.LogInformation("All migrations completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while applying migrations");
            activity?.AddException(ex);
            throw;
        }
        finally
        {
            // Stop the worker after migrations are complete
            _logger.LogInformation("Migration service completed - stopping application");
            _hostApplicationLifetime.StopApplication();
        }
    }

    private async Task ApplyMigrationsAsync(DbContext dbContext, string contextName, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Applying migrations for {ContextName}", contextName);

        try
        {
            // Ensure database and migrations history table exist
            _logger.LogInformation("Ensuring database exists for {ContextName}", contextName);
            
            // Create __EFMigrationsHistory table if it doesn't exist
            // This prevents the "relation does not exist" error
            await dbContext.Database.ExecuteSqlRawAsync(
                @"CREATE TABLE IF NOT EXISTS ""__EFMigrationsHistory"" (
                    ""MigrationId"" character varying(150) NOT NULL,
                    ""ProductVersion"" character varying(32) NOT NULL,
                    CONSTRAINT ""PK___EFMigrationsHistory"" PRIMARY KEY (""MigrationId"")
                );",
                cancellationToken);
            
            _logger.LogInformation("Migrations history table ensured for {ContextName}", contextName);

            // Now apply all pending migrations
            _logger.LogInformation("Running MigrateAsync for {ContextName}", contextName);
            await dbContext.Database.MigrateAsync(cancellationToken);
            _logger.LogInformation("Successfully applied all migrations for {ContextName}", contextName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying migrations for {ContextName}", contextName);
            throw;
        }
    }
}
