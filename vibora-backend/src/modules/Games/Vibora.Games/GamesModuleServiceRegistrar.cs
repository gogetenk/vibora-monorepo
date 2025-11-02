using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Vibora.Games.Api;
using Vibora.Games.Contracts.Services;
using Vibora.Games.Domain;
using Vibora.Games.Infrastructure.Data;
using Vibora.Games.Infrastructure.Persistence;
using Vibora.Games.Infrastructure.Services;

namespace Vibora.Games;

/// <summary>
/// Service registrar for the Games module.
/// This is the ONLY public class in the module (Ardalis pattern + Clean Architecture).
/// </summary>
public static class GamesModuleServiceRegistrar
{
    public static IServiceCollection AddGamesModule(
        this IServiceCollection services,
        IHostApplicationBuilder builder)
    {
        // Infrastructure: Register DbContext using Aspire PostgreSQL integration with PostGIS support
        builder.AddNpgsqlDbContext<GamesDbContext>("viboradb",
            configureDbContextOptions: options =>
            {
                options.UseNpgsql(npgsqlOptions =>
                {
                    npgsqlOptions.UseNetTopologySuite(); // Enable PostGIS/NetTopologySuite
                });
            });

        // Infrastructure: Register Repositories (scoped per request)
        services.AddScoped<IGameRepository, GameRepository>();
        services.AddScoped<IGameShareRepository, GameShareRepository>();

        // Infrastructure: Register Unit of Work (Application interface, Infrastructure implementation)
        // Clean Architecture: Application defines the contract, Infrastructure implements it
        services.AddScoped<Games.Application.IUnitOfWork, GamesUnitOfWork>();

        // Application: Register MediatR for CQRS and domain events
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(GamesModuleServiceRegistrar).Assembly);
        });

        // Configure Output Cache policies for Games module
        ConfigureOutputCachePolicies(services);

        // NOTE: Cross-module service clients are registered by the Host (Program.cs)
        // - IUsersServiceClient (used by Games module)
        // Host chooses between:
        // - UsersServiceInProcessClient (Monolith mode - direct MediatR calls)
        // - UsersServiceHttpClient (Microservices mode - HTTP calls)

        return services;
    }

    private static void ConfigureOutputCachePolicies(IServiceCollection services)
    {
        services.AddOptions<OutputCacheOptions>()
            .Configure<IServiceProvider>((options, sp) =>
            {
                // Policy for authenticated "my games" endpoint (GET /games/me)
                // IMPORTANT: Varies by Authorization header to isolate cache per user
                options.AddPolicy("GamesMyGames", builder => builder
                    .Expire(TimeSpan.FromSeconds(30))
                    .SetVaryByHeader("Authorization") // Each JWT gets its own cache
                    .SetLocking(true)); // Prevent cache stampede

                // Policy for public game list with query parameters
                options.AddPolicy("GamesAvailable", builder => builder
                    .Expire(TimeSpan.FromSeconds(60))
                    .SetVaryByQuery("location", "skillLevel", "fromDate", "toDate", "pageNumber", "pageSize")
                    .SetLocking(true));

                // Policy for game search with all parameters
                options.AddPolicy("GamesSearch", builder => builder
                    .Expire(TimeSpan.FromSeconds(60))
                    .SetVaryByQuery("when", "where", "level", "latitude", "longitude", "radiusKm")
                    .SetLocking(true));

                // Policy for game details (public, by ID)
                options.AddPolicy("GameDetails", builder => builder
                    .Expire(TimeSpan.FromSeconds(60))
                    .SetLocking(true));

                // Policy for shares (magic links) - longer cache
                options.AddPolicy("Shares", builder => builder
                    .Expire(TimeSpan.FromMinutes(10))
                    .SetLocking(true));

                // Policy for user game count (cross-module endpoint)
                options.AddPolicy("GamesUserCount", builder => builder
                    .Expire(TimeSpan.FromSeconds(60))
                    .SetLocking(true));
            });
    }

    public static IEndpointRouteBuilder MapGamesEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // API: Map all Games endpoints
        endpoints.MapGameEndpoints();

        return endpoints;
    }
}
