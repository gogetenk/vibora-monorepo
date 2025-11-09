namespace Vibora.Notifications.Domain;

/// <summary>
/// Represents the current status of a notification
/// </summary>
public enum NotificationStatus
{
    /// <summary>
    /// Notification has been queued for sending
    /// </summary>
    Pending = 1,

    /// <summary>
    /// Notification is currently being sent
    /// </summary>
    Sending = 2,

    /// <summary>
    /// Notification was successfully delivered
    /// </summary>
    Sent = 3,

    /// <summary>
    /// Notification delivery failed
    /// </summary>
    Failed = 4,

    /// <summary>
    /// Notification was cancelled before being sent
    /// </summary>
    Cancelled = 5
}
