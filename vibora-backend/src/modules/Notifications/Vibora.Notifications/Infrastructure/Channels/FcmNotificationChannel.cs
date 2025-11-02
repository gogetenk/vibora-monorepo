using Ardalis.Result;
using Microsoft.Extensions.Logging;
using Vibora.Notifications.Domain;

namespace Vibora.Notifications.Infrastructure.Channels;

/// <summary>
/// Firebase Cloud Messaging (FCM) implementation for push notifications
/// STUB VERSION: Just logs, doesn't send real notifications yet
/// TODO: Implement real FCM integration post-MVP
/// </summary>
internal sealed class FcmNotificationChannel : INotificationChannel
{
    private readonly ILogger<FcmNotificationChannel> _logger;

    public NotificationChannel ChannelType => NotificationChannel.Push;

    public FcmNotificationChannel(ILogger<FcmNotificationChannel> logger)
    {
        _logger = logger;
    }

    public async Task<Result> SendAsync(
        string deviceToken,
        NotificationContent content,
        CancellationToken cancellationToken = default)
    {
        // STUB: Just log for now
        _logger.LogInformation(
            "FCM Notification (STUB): To={DeviceToken}, Title={Title}, Body={Body}",
            deviceToken, content.Title, content.Body);

        // Simulate network call
        await Task.Delay(100, cancellationToken);

        // Always succeed in stub mode
        return Result.Success();
    }
}
