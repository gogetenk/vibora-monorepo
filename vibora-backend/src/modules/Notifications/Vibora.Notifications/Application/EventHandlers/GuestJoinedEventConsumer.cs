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
/// Consumes GuestJoinedEvent from Games module
/// Sends notification to the host when a guest joins their game
/// </summary>
public sealed class GuestJoinedEventConsumer : IConsumer<GuestJoinedEvent>
{
    private readonly ISender _sender;
    private readonly NotificationTemplateService _templateService;
    private readonly UserPreferencesService _userPreferencesService;
    private readonly ILogger<GuestJoinedEventConsumer> _logger;

    public GuestJoinedEventConsumer(
        ISender sender,
        NotificationTemplateService templateService,
        UserPreferencesService userPreferencesService,
        ILogger<GuestJoinedEventConsumer> logger)
    {
        _sender = sender;
        _templateService = templateService;
        _userPreferencesService = userPreferencesService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<GuestJoinedEvent> context)
    {
        var @event = context.Message;

        _logger.LogInformation(
            "Processing GuestJoinedEvent: Guest {GuestName} joined game {GameId}",
            @event.GuestName, @event.GameId);

        // Build notification content for host
        var content = _templateService.BuildGuestJoinedContent(
            @event.GuestName,
            @event.Location,
            @event.GameDateTime);

        // Fetch host's device token from Users module (supports monolith & microservices)
        var deviceToken = await _userPreferencesService.GetUserDeviceTokenAsync(
            @event.HostExternalId, 
            context.CancellationToken);

        if (string.IsNullOrWhiteSpace(deviceToken))
        {
            _logger.LogWarning(
                "Skipping GuestJoined notification for host {HostExternalId} - no device token or push disabled",
                @event.HostExternalId);
            return;
        }

        // Notify host
        var command = new SendNotificationCommand(
            @event.HostExternalId,
            NotificationType.PlayerJoined, // Reuse same type
            NotificationChannel.Push,
            deviceToken,
            content);

        var result = await _sender.Send(command, context.CancellationToken);

        if (result.IsSuccess)
        {
            _logger.LogInformation("GuestJoined notification sent to host successfully");
        }
        else
        {
            _logger.LogError("Failed to send GuestJoined notification: {Errors}",
                string.Join(", ", result.Errors));
        }
    }
}
