using MassTransit;
using Vibora.Games.Contracts.Events;
using Vibora.Games.Domain;

namespace Vibora.Games.Infrastructure.Services;

/// <summary>
/// Public service for game reminders
/// Handles fetching upcoming games and publishing reminder events
/// Used by Hangfire jobs in main application
/// </summary>
public sealed class GameReminderService
{
    private readonly IGameRepository _gameRepository;
    private readonly IPublishEndpoint _publishEndpoint;

    public GameReminderService(
        IGameRepository gameRepository,
        IPublishEndpoint publishEndpoint)
    {
        _gameRepository = gameRepository;
        _publishEndpoint = publishEndpoint;
    }

    /// <summary>
    /// Get games starting within a time window and publish reminder events
    /// </summary>
    public async Task PublishGameRemindersAsync(
        DateTime fromTime,
        DateTime toTime,
        CancellationToken cancellationToken = default)
    {
        var games = await _gameRepository.GetGamesStartingInTimeWindowAsync(
            fromTime,
            toTime,
            cancellationToken);

        if (!games.Any())
            return;

        var now = DateTime.UtcNow;

        foreach (var game in games)
        {
            var timeUntilStart = (game.DateTime - now).TotalMinutes;

            var integrationEvent = new GameStartingSoonEvent
            {
                GameId = game.Id,
                GameDateTime = game.DateTime,
                Location = game.Location,
                HostExternalId = game.HostExternalId,
                CurrentPlayers = game.CurrentPlayers,
                MaxPlayers = game.MaxPlayers,
                TimeUntilStartMinutes = (int)timeUntilStart,
                Participants = game.Participations
                    .Select(p => new ParticipantInfo
                    {
                        UserExternalId = p.UserExternalId,
                        UserName = p.UserName,
                        UserSkillLevel = p.UserSkillLevel,
                        JoinedAt = p.JoinedAt
                    })
                    .ToList(),
                GuestParticipants = game.GuestParticipants
                    .Select(gp => new GuestParticipantInfo
                    {
                        GuestId = gp.Id,
                        GuestName = gp.Name,
                        PhoneNumber = gp.PhoneNumber,
                        Email = gp.Email,
                        JoinedAt = gp.JoinedAt
                    })
                    .ToList(),
                PublishedAt = DateTime.UtcNow
            };

            await _publishEndpoint.Publish(integrationEvent, cancellationToken);
        }
    }

    /// <summary>
    /// Parameterless overload for Hangfire recurring jobs
    /// Publishes reminders for games starting 1.5-2.5 hours from now
    /// </summary>
    public async Task PublishGameRemindersAsync()
    {
        var now = DateTime.UtcNow;
        await PublishGameRemindersAsync(
            now.AddHours(1.5),
            now.AddHours(2.5),
            CancellationToken.None);
    }
}
