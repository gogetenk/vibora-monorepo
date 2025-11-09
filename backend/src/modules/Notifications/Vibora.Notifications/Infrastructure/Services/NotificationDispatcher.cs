using Ardalis.Result;
using Microsoft.Extensions.Logging;
using Vibora.Notifications.Domain;
using Vibora.Notifications.Infrastructure.Channels;

namespace Vibora.Notifications.Infrastructure.Services;

/// <summary>
/// Service responsible for dispatching notifications to the appropriate channel
/// Uses Strategy Pattern to select the right channel implementation
/// </summary>
internal sealed class NotificationDispatcher : INotificationDispatcher
{
    private readonly IEnumerable<INotificationChannel> _channels;
    private readonly ILogger<NotificationDispatcher> _logger;

    public NotificationDispatcher(
        IEnumerable<INotificationChannel> channels,
        ILogger<NotificationDispatcher> logger)
    {
        _channels = channels;
        _logger = logger;
    }

    /// <summary>
    /// Dispatch a notification through the specified channel
    /// </summary>
    /// <param name="notification">The notification to send</param>
    /// <param name="recipientAddress">Recipient address (device token, email, or phone)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating success or failure</returns>
    public async Task<Result> DispatchAsync(
        Notification notification,
        string recipientAddress,
        CancellationToken cancellationToken = default)
    {
        // Find the appropriate channel implementation
        var channel = _channels.FirstOrDefault(c => c.ChannelType == notification.Channel);

        if (channel == null)
        {
            var errorMessage = $"No channel implementation found for {notification.Channel}";
            _logger.LogError(
                "Failed to dispatch notification {NotificationId}: {Error}",
                notification.NotificationId,
                errorMessage);
            return Result.Error(errorMessage);
        }

        try
        {
            _logger.LogInformation(
                "Dispatching notification {NotificationId} via {Channel} to {Recipient}",
                notification.NotificationId,
                notification.Channel,
                recipientAddress);

            // Send through the selected channel
            var result = await channel.SendAsync(recipientAddress, notification.Content, cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "Successfully dispatched notification {NotificationId} via {Channel}",
                    notification.NotificationId,
                    notification.Channel);
            }
            else
            {
                _logger.LogWarning(
                    "Failed to dispatch notification {NotificationId} via {Channel}: {Error}",
                    notification.NotificationId,
                    notification.Channel,
                    result.Errors);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Exception while dispatching notification {NotificationId} via {Channel}",
                notification.NotificationId,
                notification.Channel);

            return Result.Error($"Exception: {ex.Message}");
        }
    }
}
