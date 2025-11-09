using Ardalis.Result;
using FirebaseAdmin.Messaging;
using Microsoft.Extensions.Logging;
using Vibora.Notifications.Domain;
using FcmNotification = FirebaseAdmin.Messaging.Notification;

namespace Vibora.Notifications.Infrastructure.Channels;

/// <summary>
/// Firebase Cloud Messaging (FCM) implementation for push notifications
/// Sends real FCM messages with error handling for invalid/unregistered tokens
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
        try
        {
            var message = new Message
            {
                Token = deviceToken,
                Notification = new FcmNotification
                {
                    Title = content.Title,
                    Body = content.Body
                },
                Data = content.Data ?? new Dictionary<string, string>()
            };

            var messageId = await FirebaseMessaging.DefaultInstance.SendAsync(message, cancellationToken);

            _logger.LogInformation(
                "FCM Message sent successfully. MessageId={MessageId}, DeviceToken={DeviceToken}",
                messageId, deviceToken);

            return Result.Success();
        }
        catch (FirebaseMessagingException ex) when (
            ex.MessagingErrorCode == MessagingErrorCode.Unregistered ||
            ex.MessagingErrorCode == MessagingErrorCode.InvalidArgument)
        {
            _logger.LogWarning(
                "FCM Token invalid/unregistered. DeviceToken={DeviceToken}, ErrorCode={ErrorCode}",
                deviceToken, ex.MessagingErrorCode);

            return Result.Error("Device token is invalid or unregistered");
        }
        catch (FirebaseMessagingException ex)
        {
            _logger.LogError(
                ex,
                "FCM error sending to DeviceToken={DeviceToken}, ErrorCode={ErrorCode}",
                deviceToken, ex.MessagingErrorCode);

            return Result.Error($"FCM error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error sending FCM notification to DeviceToken={DeviceToken}",
                deviceToken);

            return Result.Error($"Unexpected error: {ex.Message}");
        }
    }
}
