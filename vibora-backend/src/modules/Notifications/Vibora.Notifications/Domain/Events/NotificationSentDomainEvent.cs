using Vibora.Shared.Domain;

namespace Vibora.Notifications.Domain.Events;

/// <summary>
/// Domain event raised when a notification is successfully sent
/// Can be used for analytics, auditing, or triggering follow-up actions
/// </summary>
public sealed record NotificationSentDomainEvent(
    Guid NotificationId,
    string UserId,
    NotificationType Type,
    NotificationChannel Channel,
    DateTime SentAt
) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
