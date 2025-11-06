using Microsoft.Extensions.Logging;
using Vibora.Games.Infrastructure.Services;

namespace Vibora.Games.Infrastructure.Jobs;

/// <summary>
/// Hangfire job that checks for games starting soon (within 2 hours)
/// Delegates to GameReminderService which publishes GameStartingSoonEvent
/// Runs every 5 minutes to ensure notifications are timely
/// Must be public for Hangfire dependency injection
/// </summary>
public sealed class GameReminderJob
{
    private readonly GameReminderService _gameReminderService;
    private readonly ILogger<GameReminderJob> _logger;

    public GameReminderJob(
        GameReminderService gameReminderService,
        ILogger<GameReminderJob> logger)
    {
        _gameReminderService = gameReminderService;
        _logger = logger;
    }

    /// <summary>
    /// Execute the reminder job
    /// Queries games starting within the next 2 hours and publishes reminder events
    /// </summary>
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTime.UtcNow;
            var twoHoursLater = now.AddHours(2);
            var twoHoursAndFiveMinutesLater = now.AddHours(2).AddMinutes(5);

            _logger.LogInformation(
                "GameReminderJob started - checking for games between {FromTime} and {ToTime}",
                twoHoursLater, twoHoursAndFiveMinutesLater);

            // Delegate to service
            await _gameReminderService.PublishGameRemindersAsync(
                twoHoursLater,
                twoHoursAndFiveMinutesLater,
                cancellationToken);

            _logger.LogInformation("GameReminderJob completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GameReminderJob failed with exception");
            throw;
        }
    }
}
