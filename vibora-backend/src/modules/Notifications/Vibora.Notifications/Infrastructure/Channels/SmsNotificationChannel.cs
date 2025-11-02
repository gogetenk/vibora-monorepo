using Ardalis.Result;
using Microsoft.Extensions.Logging;
using Vibora.Notifications.Domain;

namespace Vibora.Notifications.Infrastructure.Channels;

/// <summary>
/// SMS notification channel implementation
/// STUB VERSION: Just logs, doesn't send real SMS yet
/// TODO: Implement real SMS service (Twilio/AWS SNS) post-MVP
/// </summary>
internal sealed class SmsNotificationChannel : INotificationChannel
{
    private readonly ILogger<SmsNotificationChannel> _logger;

    public NotificationChannel ChannelType => NotificationChannel.Sms;

    public SmsNotificationChannel(ILogger<SmsNotificationChannel> logger)
    {
        _logger = logger;
    }

    public async Task<Result> SendAsync(
        string phoneNumber,
        NotificationContent content,
        CancellationToken cancellationToken = default)
    {
        // STUB: Just log for now
        _logger.LogInformation(
            "SMS Notification (STUB): To={PhoneNumber}, Message={Body}",
            phoneNumber, content.Body);

        // Simulate network call
        await Task.Delay(200, cancellationToken);

        // Always succeed in stub mode
        return Result.Success();
    }
}
