namespace Vibora.Notifications.Domain;

/// <summary>
/// Value object representing user's notification preferences
/// Determines which channels are enabled for notifications
/// </summary>
public sealed class NotificationPreferences
{
    public bool PushEnabled { get; private init; }
    public bool EmailEnabled { get; private init; }
    public bool SmsEnabled { get; private init; }

    // EF Core constructor
    private NotificationPreferences()
    {
    }

    public NotificationPreferences(bool pushEnabled, bool emailEnabled, bool smsEnabled)
    {
        PushEnabled = pushEnabled;
        EmailEnabled = emailEnabled;
        SmsEnabled = smsEnabled;
    }

    /// <summary>
    /// Default preferences for new users (only Push enabled)
    /// </summary>
    public static NotificationPreferences Default() => new(
        pushEnabled: true,
        emailEnabled: false,
        smsEnabled: false
    );

    /// <summary>
    /// Check if a specific channel is enabled
    /// </summary>
    public bool IsChannelEnabled(NotificationChannel channel)
    {
        return channel switch
        {
            NotificationChannel.Push => PushEnabled,
            NotificationChannel.Email => EmailEnabled,
            NotificationChannel.Sms => SmsEnabled,
            _ => false
        };
    }
}
