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
/// Consumes ParticipationRemovedEvent from Games module
/// Sends notification to the host when a player leaves their game
/// </summary>
public sealed class ParticipationRemovedEventConsumer : IConsumer<ParticipationRemovedEvent>
{
    private readonly ISender _sender;
    private readonly NotificationTemplateService _templateService;
    private readonly UserPreferencesService _userPreferencesService;
    private readonly ILogger<ParticipationRemovedEventConsumer> _logger;

    public ParticipationRemovedEventConsumer(
        ISender sender,
        NotificationTemplateService templateService,
        UserPreferencesService userPreferencesService,
        ILogger<ParticipationRemovedEventConsumer> logger)
    {
        _sender = sender;
        _templateService = templateService;
        _userPreferencesService = userPreferencesService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ParticipationRemovedEvent> context)
    {
        var @event = context.Message;

        _logger.LogInformation(
            "Processing ParticipationRemovedEvent: {UserName} left game {GameId}",
            @event.UserName, @event.GameId);

        // Build notification content for host
        var content = _templateService.BuildPlayerLeftContent(
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
                "Skipping ParticipationRemoved notification for host {HostExternalId} - no device token or push disabled",
                @event.HostExternalId);
            return;
        }

        // Notify host
        var command = new SendNotificationCommand(
            @event.HostExternalId,
            NotificationType.PlayerLeft,
            NotificationChannel.Push,
            deviceToken,
            content);

        var result = await _sender.Send(command, context.CancellationToken);

        if (result.IsSuccess)
        {
            _logger.LogInformation("ParticipationRemoved notification sent to host successfully");
        }
        else
        {
            _logger.LogError("Failed to send ParticipationRemoved notification: {Errors}",
                string.Join(", ", result.Errors));
        }
    }
}
