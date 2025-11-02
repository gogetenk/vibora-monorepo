using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Vibora.Games.Contracts.Events;
using Vibora.Notifications.Application.Commands.SendNotification;
using Vibora.Notifications.Application.Services;
using Vibora.Notifications.Domain;
using Vibora.Notifications.Infrastructure.Services;

namespace Vibora.Notifications.Application.EventHandlers;

/// <summary>
/// Consumes PlayerJoinedEvent from Games module
/// Sends notification to the host when a registered player joins their game
/// </summary>
public sealed class PlayerJoinedEventConsumer : IConsumer<PlayerJoinedEvent>
{
    private readonly ISender _sender;
    private readonly NotificationTemplateService _templateService;
    private readonly UserPreferencesService _userPreferencesService;
    private readonly ILogger<PlayerJoinedEventConsumer> _logger;

    public PlayerJoinedEventConsumer(
        ISender sender,
        NotificationTemplateService templateService,
        UserPreferencesService userPreferencesService,
        ILogger<PlayerJoinedEventConsumer> logger)
    {
        _sender = sender;
        _templateService = templateService;
        _userPreferencesService = userPreferencesService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PlayerJoinedEvent> context)
    {
        var @event = context.Message;

        _logger.LogInformation(
            "Processing PlayerJoinedEvent: {UserName} joined game {GameId}",
            @event.UserName, @event.GameId);

        // Build notification content for host
        var content = _templateService.BuildPlayerJoinedContent(
            @event.UserName,
            @event.Location,
            @event.GameDateTime);

        // Fetch host's device token from Users module (supports monolith & microservices)
        var deviceToken = await _userPreferencesService.GetUserDeviceTokenAsync(
            @event.HostExternalId, 
            context.CancellationToken);

        if (string.IsNullOrWhiteSpace(deviceToken))
        {
            _logger.LogWarning(
                "Skipping PlayerJoined notification for host {HostExternalId} - no device token or push disabled",
                @event.HostExternalId);
            return;
        }

        // Notify host
        var command = new SendNotificationCommand(
            @event.HostExternalId,
            NotificationType.PlayerJoined,
            NotificationChannel.Push,
            deviceToken,
            content);

        var result = await _sender.Send(command, context.CancellationToken);

        if (result.IsSuccess)
        {
            _logger.LogInformation("PlayerJoined notification sent to host successfully");
        }
        else
        {
            _logger.LogError("Failed to send PlayerJoined notification: {Errors}",
                string.Join(", ", result.Errors));
        }
    }
}
