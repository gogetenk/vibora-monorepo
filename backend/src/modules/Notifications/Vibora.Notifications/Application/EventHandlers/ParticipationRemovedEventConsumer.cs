using Ardalis.Result;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Vibora.Games.Contracts.Events;
using Vibora.Games.Contracts.Services;
using Vibora.Notifications.Application.Commands.SendNotification;
using Vibora.Notifications.Application.Services;
using Vibora.Notifications.Domain;
using Vibora.Notifications.Infrastructure.Services;

namespace Vibora.Notifications.Application.EventHandlers;

/// <summary>
/// Consumes ParticipationRemovedEvent from Games module
/// Sends notification to the host and all remaining participants when a player leaves
/// </summary>
internal sealed class ParticipationRemovedEventConsumer : IConsumer<ParticipationRemovedEvent>
{
    private readonly ISender _sender;
    private readonly NotificationTemplateService _templateService;
    private readonly UserPreferencesService _userPreferencesService;
    private readonly IGamesServiceClient _gamesServiceClient;
    private readonly ILogger<ParticipationRemovedEventConsumer> _logger;

    public ParticipationRemovedEventConsumer(
        ISender sender,
        NotificationTemplateService templateService,
        UserPreferencesService userPreferencesService,
        IGamesServiceClient gamesServiceClient,
        ILogger<ParticipationRemovedEventConsumer> logger)
    {
        _sender = sender;
        _templateService = templateService;
        _userPreferencesService = userPreferencesService;
        _gamesServiceClient = gamesServiceClient;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ParticipationRemovedEvent> context)
    {
        var @event = context.Message;

        _logger.LogInformation(
            "Processing ParticipationRemovedEvent: {UserName} left game {GameId}",
            @event.UserName, @event.GameId);

        // Build notification content
        var content = _templateService.BuildPlayerLeftContent(
            @event.UserName,
            @event.Location,
            @event.GameDateTime);

        // Collect all user IDs to notify: host + remaining participants
        var userIdsToNotify = new List<string> { @event.HostExternalId };

        // Get remaining participant IDs (excluding the user who just left)
        var remainingParticipantIds = await _gamesServiceClient.GetGameParticipantIdsAsync(
            @event.GameId,
            @event.UserExternalId,
            context.CancellationToken);

        userIdsToNotify.AddRange(remainingParticipantIds);

        // Fetch device tokens for all users in a single batch call
        var deviceTokens = await _userPreferencesService.GetDeviceTokensBatchAsync(
            userIdsToNotify,
            context.CancellationToken);

        if (deviceTokens.Count == 0)
        {
            _logger.LogWarning(
                "No device tokens found for any participants in game {GameId} - skipping notifications",
                @event.GameId);
            return;
        }

        // Send notifications to all users with valid device tokens
        var notificationTasks = new List<Task<Result<Guid>>>();

        foreach (var (userId, token) in deviceTokens)
        {
            var command = new SendNotificationCommand(
                userId,
                NotificationType.PlayerLeft,
                NotificationChannel.Push,
                token,
                content);

            notificationTasks.Add(_sender.Send(command, context.CancellationToken));
        }

        await Task.WhenAll(notificationTasks);

        _logger.LogInformation(
            "ParticipationRemoved notifications sent: {NotificationCount} recipients notified for game {GameId}",
            deviceTokens.Count,
            @event.GameId);
    }
}
