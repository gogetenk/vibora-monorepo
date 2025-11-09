using Ardalis.Result;
using Vibora.Notifications.Domain;

namespace Vibora.Notifications.Infrastructure.Services;

/// <summary>
/// Interface for notification dispatcher
/// Allows for testing and extensibility
/// </summary>
internal interface INotificationDispatcher
{
    /// <summary>
    /// Dispatch a notification through the specified channel
    /// </summary>
    /// <param name="notification">The notification to send</param>
    /// <param name="recipientAddress">Recipient address (device token, email, or phone)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating success or failure</returns>
    Task<Result> DispatchAsync(
        Notification notification,
        string recipientAddress,
        CancellationToken cancellationToken = default);
}
