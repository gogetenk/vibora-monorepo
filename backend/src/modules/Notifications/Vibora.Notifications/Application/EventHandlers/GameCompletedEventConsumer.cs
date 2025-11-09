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
/// Consumes GameCompletedEvent from Games module
/// Sends notification to the host when their game reaches max players
/// </summary>
internal sealed class GameCompletedEventConsumer : IConsumer<GameCompletedEvent>
{
    private readonly ISender _sender;
    private readonly NotificationTemplateService _templateService;
    private readonly UserPreferencesService _userPreferencesService;
    private readonly ILogger<GameCompletedEventConsumer> _logger;

    public GameCompletedEventConsumer(
        ISender sender,
        NotificationTemplateService templateService,
        UserPreferencesService userPreferencesService,
        ILogger<GameCompletedEventConsumer> logger)
    {
        _sender = sender;
        _templateService = templateService;
        _userPreferencesService = userPreferencesService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<GameCompletedEvent> context)
    {
        var @event = context.Message;

        _logger.LogInformation(
            "Processing GameCompletedEvent: Game {GameId} is now full",
            @event.GameId);

        // Build notification content for host
        var content = _templateService.BuildGameCompletedContent(
            @event.Location,
            @event.GameDateTime,
            @event.MaxPlayers);

        // Fetch host's device token from Users module (supports monolith & microservices)
        var deviceToken = await _userPreferencesService.GetUserDeviceTokenAsync(
            @event.HostExternalId,
            context.CancellationToken);

        if (string.IsNullOrWhiteSpace(deviceToken))
        {
            _logger.LogWarning(
                "Skipping GameCompleted notification for host {HostExternalId} - no device token or push disabled",
                @event.HostExternalId);
            return;
        }

        // Notify host
        var command = new SendNotificationCommand(
            @event.HostExternalId,
            NotificationType.GameCompleted,
            NotificationChannel.Push,
            deviceToken,
            content);

        var result = await _sender.Send(command, context.CancellationToken);

        if (result.IsSuccess)
        {
            _logger.LogInformation("GameCompleted notification sent to host successfully");
        }
        else
        {
            _logger.LogError("Failed to send GameCompleted notification: {Errors}",
                string.Join(", ", result.Errors));
        }
    }
}
