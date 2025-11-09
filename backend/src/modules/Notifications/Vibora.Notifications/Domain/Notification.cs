using Ardalis.Result;
using Vibora.Notifications.Domain.Events;
using Vibora.Shared.Domain;

namespace Vibora.Notifications.Domain;

/// <summary>
/// Notification aggregate root - Represents a single notification to be sent to a user
/// Manages notification lifecycle: Pending -> Sending -> Sent/Failed
/// </summary>
public sealed class Notification : AggregateRoot
{
    private const int MaxRetries = 3;

    public Guid NotificationId { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public NotificationType Type { get; private set; }
    public NotificationChannel Channel { get; private set; }
    public NotificationStatus Status { get; private set; }
    public NotificationContent Content { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? SentAt { get; private set; }
    public int RetryCount { get; private set; }
    public string? ErrorMessage { get; private set; }
    public bool IsRead { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    // EF Core constructor
    private Notification() { }

    /// <summary>
    /// Create a new notification to be sent
    /// </summary>
    public static Result<Notification> Create(
        string userId,
        NotificationType type,
        NotificationChannel channel,
        NotificationContent content)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Result<Notification>.Invalid(new ValidationError(nameof(userId), "UserId cannot be empty"));

        if (content == null)
            return Result<Notification>.Invalid(new ValidationError(nameof(content), "Content cannot be null"));

        var notification = new Notification
        {
            NotificationId = Guid.NewGuid(),
            UserId = userId,
            Type = type,
            Channel = channel,
            Status = NotificationStatus.Pending,
            Content = content,
            CreatedAt = DateTime.UtcNow,
            RetryCount = 0
        };

        return Result<Notification>.Success(notification);
    }

    /// <summary>
    /// Mark notification as being sent (status transition)
    /// </summary>
    public Result MarkAsSending()
    {
        if (Status != NotificationStatus.Pending)
            return Result.Error($"Cannot mark as sending. Current status: {Status}");

        Status = NotificationStatus.Sending;
        return Result.Success();
    }

    /// <summary>
    /// Mark notification as successfully sent
    /// Raises NotificationSentDomainEvent
    /// </summary>
    public Result MarkAsSent()
    {
        if (Status != NotificationStatus.Sending)
            return Result.Error($"Cannot mark as sent. Current status: {Status}");

        Status = NotificationStatus.Sent;
        SentAt = DateTime.UtcNow;

        AddDomainEvent(new NotificationSentDomainEvent(
            NotificationId,
            UserId,
            Type,
            Channel,
            SentAt.Value
        ));

        return Result.Success();
    }

    /// <summary>
    /// Mark notification as failed
    /// Will retry if under max retry limit, otherwise raises NotificationFailedDomainEvent
    /// </summary>
    public Result MarkAsFailed(string errorMessage)
    {
        if (Status != NotificationStatus.Sending)
            return Result.Error($"Cannot mark as failed. Current status: {Status}");

        RetryCount++;
        ErrorMessage = errorMessage;

        if (RetryCount >= MaxRetries)
        {
            Status = NotificationStatus.Failed;

            AddDomainEvent(new NotificationFailedDomainEvent(
                NotificationId,
                UserId,
                Type,
                Channel,
                errorMessage,
                RetryCount
            ));
        }
        else
        {
            // Reset to Pending for retry
            Status = NotificationStatus.Pending;
        }

        return Result.Success();
    }

    /// <summary>
    /// Cancel notification before sending
    /// </summary>
    public Result Cancel()
    {
        if (Status != NotificationStatus.Pending)
            return Result.Error($"Cannot cancel notification. Current status: {Status}");

        Status = NotificationStatus.Cancelled;
        return Result.Success();
    }

    /// <summary>
    /// Check if notification can be retried
    /// </summary>
    public bool CanRetry()
    {
        return Status == NotificationStatus.Pending && RetryCount < MaxRetries;
    }

    /// <summary>
    /// Mark notification as read
    /// </summary>
    public Result MarkAsRead()
    {
        if (DeletedAt.HasValue)
            return Result.Error("Cannot mark deleted notification as read");

        IsRead = true;
        return Result.Success();
    }

    /// <summary>
    /// Soft delete notification
    /// </summary>
    public Result SoftDelete()
    {
        if (DeletedAt.HasValue)
            return Result.Error("Notification is already deleted");

        DeletedAt = DateTime.UtcNow;
        return Result.Success();
    }
}
