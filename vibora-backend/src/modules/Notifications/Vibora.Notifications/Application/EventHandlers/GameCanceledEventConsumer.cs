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
/// Consumes GameCanceledEvent from Games module
/// Sends notification to all participants when a game is canceled
/// </summary>
internal sealed class GameCanceledEventConsumer : IConsumer<GameCanceledEvent>
{
    private readonly ISender _sender;
    private readonly NotificationTemplateService _templateService;
    private readonly UserPreferencesService _userPreferencesService;
    private readonly ILogger<GameCanceledEventConsumer> _logger;

    public GameCanceledEventConsumer(
        ISender sender,
        NotificationTemplateService templateService,
        UserPreferencesService userPreferencesService,
        ILogger<GameCanceledEventConsumer> logger)
    {
        _sender = sender;
        _templateService = templateService;
        _userPreferencesService = userPreferencesService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<GameCanceledEvent> context)
    {
        var @event = context.Message;

        _logger.LogInformation(
            "Processing GameCanceledEvent for game {GameId} with {TotalParticipants} participants - {RegisteredCount} registered, {GuestCount} guests",
            @event.GameId, @event.TotalParticipants, @event.Participants.Count, @event.GuestParticipants.Count);

        // Build notification content
        var content = _templateService.BuildGameCanceledContent(
            @event.Location,
            @event.GameDateTime);

        var successCount = 0;
        var failureCount = 0;

        // Optimize: Fetch all device tokens in a single batch call
        var participantIds = @event.Participants.Select(p => p.UserExternalId).ToList();
        var deviceTokens = await _userPreferencesService.GetDeviceTokensBatchAsync(
            participantIds, 
            context.CancellationToken);

        // Notify all participants who have device tokens
        foreach (var participant in @event.Participants)
        {
            if (!deviceTokens.TryGetValue(participant.UserExternalId, out var deviceToken))
            {
                _logger.LogWarning(
                    "Skipping GameCanceled notification for {UserExternalId} - no device token or push disabled",
                    participant.UserExternalId);
                continue;
            }

            var command = new SendNotificationCommand(
                participant.UserExternalId,
                NotificationType.GameCancelled,
                NotificationChannel.Push,
                deviceToken,
                content);

            var result = await _sender.Send(command, context.CancellationToken);

            if (result.IsSuccess)
            {
                successCount++;
                _logger.LogInformation(
                    "GameCanceled notification sent to {UserExternalId} for game {GameId}",
                    participant.UserExternalId, @event.GameId);
            }
            else
            {
                failureCount++;
                _logger.LogError(
                    "Failed to send GameCanceled notification to {UserExternalId} for game {GameId}: {Errors}",
                    participant.UserExternalId, @event.GameId, string.Join(", ", result.Errors));
            }
        }

        // TODO: Notify guest participants via SMS/Email
        // Guests don't have device tokens, so we'd need to send via their contact info
        // This will be implemented post-MVP once SMS/Email channels are functional
        foreach (var guest in @event.GuestParticipants)
        {
            _logger.LogInformation(
                "Skipping notification for guest {GuestName} (ID: {GuestId}) - SMS/Email not implemented yet",
                guest.GuestName, guest.GuestId);
        }

        _logger.LogInformation(
            "GameCanceled notifications processed for game {GameId}: {SuccessCount} succeeded, {FailureCount} failed",
            @event.GameId, successCount, failureCount);
    }
}
