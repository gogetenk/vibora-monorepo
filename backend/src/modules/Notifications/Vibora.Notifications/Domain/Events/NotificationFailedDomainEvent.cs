using Vibora.Shared.Domain;

namespace Vibora.Notifications.Domain.Events;

/// <summary>
/// Domain event raised when a notification fails after all retries
/// Can be used for alerting, logging, or compensating actions
/// </summary>
public sealed record NotificationFailedDomainEvent(
    Guid NotificationId,
    string UserId,
    NotificationType Type,
    NotificationChannel Channel,
    string ErrorMessage,
    int RetryCount
) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
