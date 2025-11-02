namespace Vibora.Notifications.Domain;

/// <summary>
/// Represents the delivery channel for a notification
/// </summary>
public enum NotificationChannel
{
    /// <summary>
    /// Firebase Cloud Messaging (mobile push notification)
    /// </summary>
    Push = 1,

    /// <summary>
    /// Email notification
    /// </summary>
    Email = 2,

    /// <summary>
    /// SMS notification
    /// </summary>
    Sms = 3
}
