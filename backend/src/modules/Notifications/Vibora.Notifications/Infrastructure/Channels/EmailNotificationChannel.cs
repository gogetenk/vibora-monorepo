using Ardalis.Result;
using Microsoft.Extensions.Logging;
using Vibora.Notifications.Domain;

namespace Vibora.Notifications.Infrastructure.Channels;

/// <summary>
/// Email notification channel implementation
/// STUB VERSION: Just logs, doesn't send real emails yet
/// TODO: Implement real email service (SendGrid/AWS SES) post-MVP
/// </summary>
internal sealed class EmailNotificationChannel : INotificationChannel
{
    private readonly ILogger<EmailNotificationChannel> _logger;

    public NotificationChannel ChannelType => NotificationChannel.Email;

    public EmailNotificationChannel(ILogger<EmailNotificationChannel> logger)
    {
        _logger = logger;
    }

    public async Task<Result> SendAsync(
        string emailAddress,
        NotificationContent content,
        CancellationToken cancellationToken = default)
    {
        // STUB: Just log for now
        _logger.LogInformation(
            "Email Notification (STUB): To={EmailAddress}, Subject={Title}, Body={Body}",
            emailAddress, content.Title, content.Body);

        // Simulate network call
        await Task.Delay(150, cancellationToken);

        // Always succeed in stub mode
        return Result.Success();
    }
}
