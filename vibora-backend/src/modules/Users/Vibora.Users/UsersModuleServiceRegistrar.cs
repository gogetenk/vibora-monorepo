using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Vibora.Users.Api;
using Vibora.Users.Application.Services;
using Vibora.Users.Domain;
using Vibora.Users.Infrastructure.Authentication;
using Vibora.Users.Infrastructure.Data;
using Vibora.Users.Infrastructure.Persistence;
using Vibora.Users.Infrastructure.Services;

namespace Vibora.Users;

/// <summary>
/// Service registrar for the Users module.
/// This is the ONLY public class in the module (Ardalis pattern + Clean Architecture).
/// </summary>
public static class UsersModuleServiceRegistrar
{
    public static IServiceCollection AddUsersModule(
        this IServiceCollection services,
        IHostApplicationBuilder builder)
    {
        // Infrastructure: Register DbContext using Aspire PostgreSQL integration
        builder.AddNpgsqlDbContext<UsersDbContext>("viboradb");

        // Infrastructure: Register Repositories (scoped per request)
        services.AddScoped<IUserRepository, UserRepository>();

        // Infrastructure: Register Unit of Work (Application interface, Infrastructure implementation)
        // Clean Architecture: Application defines the contract, Infrastructure implements it
        services.AddScoped<Users.Application.IUnitOfWork, UsersUnitOfWork>();

        // Infrastructure: Register Photo Storage Service (Strategy Pattern)
        RegisterPhotoStorageService(services, builder.Configuration);

        // Infrastructure: Register JWT Token Generator (for guest users)
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

        // Application: Register MediatR for CQRS and domain events
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(UsersModuleServiceRegistrar).Assembly);
        });

        // Configure Output Cache policies for Users module
        ConfigureOutputCachePolicies(services);

        // NOTE: Cross-module service clients are registered by the Host (Program.cs)
        // - IGamesServiceClient (used by Users module for guest conversion)
        // - IUsersServiceClient (used by Games module for user metadata)
        // Host chooses between:
        // - InProcessClient (Monolith mode - direct MediatR calls)
        // - HttpClient (Microservices mode - HTTP calls)

        return services;
    }

    private static void ConfigureOutputCachePolicies(IServiceCollection services)
    {
        services.AddOptions<OutputCacheOptions>()
            .Configure<IServiceProvider>((options, sp) =>
            {
                // Policy for authenticated user profile (GET /users/me, GET /users/profile)
                // IMPORTANT: Varies by Authorization header to isolate cache per user
                options.AddPolicy("UsersMyProfile", builder => builder
                    .Expire(TimeSpan.FromMinutes(5))
                    .SetVaryByHeader("Authorization") // Each JWT gets its own cache
                    .SetLocking(true));

                // Policy for public user profiles (GET /users/{externalId}/profile)
                options.AddPolicy("UsersPublicProfile", builder => builder
                    .Expire(TimeSpan.FromMinutes(5))
                    .SetLocking(true));
            });
    }

    private static void RegisterPhotoStorageService(
        IServiceCollection services,
        IConfiguration configuration)
    {
        var provider = configuration["PhotoStorage:Provider"] ?? "FileSystem";

        switch (provider)
        {
            case "AzureBlob":
                services.AddScoped<IPhotoStorageService, AzureBlobPhotoStorageService>();
                break;
            case "FileSystem":
            default:
                services.AddScoped<IPhotoStorageService, FileSystemPhotoStorageService>();
                break;
        }
    }

    public static IEndpointRouteBuilder MapUsersEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // API: Map all Users endpoints
        endpoints.MapUserEndpoints();

        return endpoints;
    }
}
