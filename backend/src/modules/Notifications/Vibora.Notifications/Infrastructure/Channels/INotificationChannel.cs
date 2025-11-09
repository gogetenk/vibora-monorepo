using Ardalis.Result;
using Vibora.Notifications.Domain;

namespace Vibora.Notifications.Infrastructure.Channels;

/// <summary>
/// Strategy interface for sending notifications through different channels
/// Each channel (Push, Email, SMS) implements this interface
/// </summary>
internal interface INotificationChannel
{
    /// <summary>
    /// The channel type this implementation handles
    /// </summary>
    NotificationChannel ChannelType { get; }

    /// <summary>
    /// Send a notification through this channel
    /// </summary>
    /// <param name="recipientAddress">Recipient address (device token, email, or phone number)</param>
    /// <param name="content">Notification content (title, body, data)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating success or failure with error message</returns>
    Task<Result> SendAsync(
        string recipientAddress,
        NotificationContent content,
        CancellationToken cancellationToken = default);
}
