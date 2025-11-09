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
/// Consumes GameStartingSoonEvent from Games module
/// Sends reminder notifications to all participants (registered + guests)
/// Triggered 2 hours before game start via Hangfire
/// </summary>
internal sealed class GameStartingSoonEventConsumer : IConsumer<GameStartingSoonEvent>
{
    private readonly ISender _sender;
    private readonly NotificationTemplateService _templateService;
    private readonly UserPreferencesService _userPreferencesService;
    private readonly ILogger<GameStartingSoonEventConsumer> _logger;

    public GameStartingSoonEventConsumer(
        ISender sender,
        NotificationTemplateService templateService,
        UserPreferencesService userPreferencesService,
        ILogger<GameStartingSoonEventConsumer> logger)
    {
        _sender = sender;
        _templateService = templateService;
        _userPreferencesService = userPreferencesService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<GameStartingSoonEvent> context)
    {
        var @event = context.Message;

        _logger.LogInformation(
            "Processing GameStartingSoonEvent: Game {GameId} starting in {MinutesUntil} minutes",
            @event.GameId, @event.TimeUntilStartMinutes);

        // Build participant names list
        var participantNames = new List<string> { @event.HostExternalId };
        participantNames.AddRange(@event.Participants.Select(p => p.UserName));

        // Build notification content
        var content = _templateService.BuildGameStartingSoonContent(
            @event.Location,
            @event.GameDateTime,
            participantNames);

        // Notify host
        await NotifyParticipantAsync(
            @event.HostExternalId,
            content,
            "host",
            context.CancellationToken);

        // Notify all registered participants
        foreach (var participant in @event.Participants)
        {
            await NotifyParticipantAsync(
                participant.UserExternalId,
                content,
                $"participant:{participant.UserName}",
                context.CancellationToken);
        }

        // Note: Guest participants may not have device tokens registered yet
        // (they join anonymously). We skip push notifications for guests
        // but could send SMS/email if contact info is available in future

        _logger.LogInformation(
            "GameStartingSoon reminders sent to host + {ParticipantCount} participants",
            @event.Participants.Count);
    }

    private async Task NotifyParticipantAsync(
        string userExternalId,
        NotificationContent content,
        string participantType,
        CancellationToken cancellationToken)
    {
        // Fetch participant's device token
        var deviceToken = await _userPreferencesService.GetUserDeviceTokenAsync(
            userExternalId,
            cancellationToken);

        if (string.IsNullOrWhiteSpace(deviceToken))
        {
            _logger.LogDebug(
                "Skipping GameStartingSoon notification for {ParticipantType} {UserExternalId} - no device token",
                participantType, userExternalId);
            return;
        }

        // Send push notification
        var command = new SendNotificationCommand(
            userExternalId,
            NotificationType.GameStartingSoon,
            NotificationChannel.Push,
            deviceToken,
            content);

        var result = await _sender.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogError(
                "Failed to send GameStartingSoon notification to {ParticipantType}: {Errors}",
                participantType, string.Join(", ", result.Errors));
        }
    }
}
