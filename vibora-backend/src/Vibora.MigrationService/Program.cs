using Microsoft.EntityFrameworkCore;
using Vibora.Games.Infrastructure.Data;
using Vibora.MigrationService;
using Vibora.Notifications.Infrastructure.Data;
using Vibora.Users.Infrastructure.Data;

var builder = Host.CreateApplicationBuilder(args);

// Add service defaults (Aspire configuration)
builder.AddServiceDefaults();

// Register the migration worker
builder.Services.AddHostedService<Worker>();

// Add OpenTelemetry for tracing migrations
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource(Worker.ActivitySourceName));

// Register DbContexts with PostgreSQL connection
builder.AddNpgsqlDbContext<GamesDbContext>("viboradb",
    configureDbContextOptions: options =>
    {
        options.UseNpgsql(npgsqlOptions =>
        {
            npgsqlOptions.UseNetTopologySuite(); // Enable PostGIS/NetTopologySuite for Games module
        });
    });
builder.AddNpgsqlDbContext<UsersDbContext>("viboradb");
builder.AddNpgsqlDbContext<NotificationsDbContext>("viboradb");

var host = builder.Build();
host.Run();
