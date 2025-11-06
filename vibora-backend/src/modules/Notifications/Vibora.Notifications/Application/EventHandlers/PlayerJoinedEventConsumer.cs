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
/// Consumes PlayerJoinedEvent from Games module
/// Sends notifications to the host and all existing participants when a registered player joins
/// </summary>
internal sealed class PlayerJoinedEventConsumer : IConsumer<PlayerJoinedEvent>
{
    private readonly ISender _sender;
    private readonly NotificationTemplateService _templateService;
    private readonly UserPreferencesService _userPreferencesService;
    private readonly IGamesServiceClient _gamesServiceClient;
    private readonly ILogger<PlayerJoinedEventConsumer> _logger;

    public PlayerJoinedEventConsumer(
        ISender sender,
        NotificationTemplateService templateService,
        UserPreferencesService userPreferencesService,
        IGamesServiceClient gamesServiceClient,
        ILogger<PlayerJoinedEventConsumer> logger)
    {
        _sender = sender;
        _templateService = templateService;
        _userPreferencesService = userPreferencesService;
        _gamesServiceClient = gamesServiceClient;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PlayerJoinedEvent> context)
    {
        var @event = context.Message;

        _logger.LogInformation(
            "Processing PlayerJoinedEvent: {UserName} joined game {GameId}",
            @event.UserName, @event.GameId);

        // Build notification content
        var content = _templateService.BuildPlayerJoinedContent(
            @event.UserName,
            @event.Location,
            @event.GameDateTime);

        // Notify host
        await NotifyUserAsync(@event.HostExternalId, content, context.CancellationToken);

        // Notify all existing participants (except the user who just joined)
        var participantIds = await _gamesServiceClient.GetGameParticipantIdsAsync(
            @event.GameId,
            excludeUserId: @event.UserExternalId,
            cancellationToken: context.CancellationToken);

        foreach (var participantId in participantIds)
        {
            await NotifyUserAsync(participantId, content, context.CancellationToken);
        }

        _logger.LogInformation(
            "PlayerJoined notifications sent to host and {ParticipantCount} existing participants",
            participantIds.Count);
    }

    private async Task NotifyUserAsync(
        string userExternalId,
        NotificationContent content,
        CancellationToken cancellationToken)
    {
        // Fetch user's device token from Users module (supports monolith & microservices)
        var deviceToken = await _userPreferencesService.GetUserDeviceTokenAsync(
            userExternalId,
            cancellationToken);

        if (string.IsNullOrWhiteSpace(deviceToken))
        {
            _logger.LogWarning(
                "Skipping PlayerJoined notification for user {UserExternalId} - no device token or push disabled",
                userExternalId);
            return;
        }

        // Send notification
        var command = new SendNotificationCommand(
            userExternalId,
            NotificationType.PlayerJoined,
            NotificationChannel.Push,
            deviceToken,
            content);

        var result = await _sender.Send(command, cancellationToken);

        if (result.IsSuccess)
        {
            _logger.LogInformation(
                "PlayerJoined notification sent successfully to user {UserExternalId}",
                userExternalId);
        }
        else
        {
            _logger.LogError(
                "Failed to send PlayerJoined notification to user {UserExternalId}: {Errors}",
                userExternalId,
                string.Join(", ", result.Errors));
        }
    }
}
