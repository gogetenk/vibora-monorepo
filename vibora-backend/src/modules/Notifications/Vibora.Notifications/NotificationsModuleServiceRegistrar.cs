using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Vibora.Notifications.Api;
using Vibora.Notifications.Application;
using Vibora.Notifications.Domain;
using Vibora.Notifications.Infrastructure.Channels;
using Vibora.Notifications.Infrastructure.Data;
using Vibora.Notifications.Infrastructure.Persistence;
using Vibora.Notifications.Infrastructure.Services;

namespace Vibora.Notifications;

/// <summary>
/// Service registrar for the Notifications module.
/// This is the ONLY public class in the module (Ardalis pattern + Clean Architecture).
/// </summary>
public static class NotificationsModuleServiceRegistrar
{
    public static IServiceCollection AddNotificationsModule(
        this IServiceCollection services,
        IHostApplicationBuilder builder)
    {
        // Infrastructure: Register DbContext using Aspire PostgreSQL integration
        builder.AddNpgsqlDbContext<NotificationsDbContext>("viboradb");

        // Infrastructure: Register Repository (scoped per request)
        services.AddScoped<INotificationRepository, NotificationRepository>();

        // Infrastructure: Register Unit of Work (CRITICAL: isolated per module)
        services.AddScoped<IUnitOfWork, NotificationsUnitOfWork>();

        // Infrastructure: Register Notification Channels (Strategy Pattern)
        services.AddScoped<INotificationChannel, FcmNotificationChannel>();
        services.AddScoped<INotificationChannel, EmailNotificationChannel>();
        services.AddScoped<INotificationChannel, SmsNotificationChannel>();

        // Infrastructure: Register Services
        services.AddScoped<INotificationDispatcher, NotificationDispatcher>();
        services.AddScoped<NotificationTemplateService>();

        // Application: Register Services
        services.AddScoped<Application.Services.UserPreferencesService>();

        // NOTE: IUsersServiceClient implementation is registered by the Host
        // Host chooses between:
        // - UsersServiceInProcessClient (Monolith mode - direct MediatR calls)
        // - UsersServiceHttpClient (Microservices mode - HTTP calls)

        // Application: Register MediatR for CQRS and domain events
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(NotificationsModuleServiceRegistrar).Assembly);
        });

        return services;
    }

    /// <summary>
    /// Configure MassTransit consumers for this module.
    /// Called separately to integrate with main MassTransit configuration.
    /// </summary>
    public static void ConfigureNotificationsConsumers(IBusRegistrationConfigurator configurator)
    {
        // Register all consumers from Notifications assembly
        configurator.AddConsumers(typeof(NotificationsModuleServiceRegistrar).Assembly);
    }

    public static IEndpointRouteBuilder MapNotificationsEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        // API: Map all Notifications endpoints
        endpoints.MapNotificationEndpoints();

        return endpoints;
    }
}
