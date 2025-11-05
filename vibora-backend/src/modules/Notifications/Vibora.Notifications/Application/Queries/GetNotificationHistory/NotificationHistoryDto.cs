namespace Vibora.Notifications.Application.Queries.GetNotificationHistory;

/// <summary>
/// DTO for notification history response
/// Flat structure for easy UI binding
/// </summary>
public sealed record NotificationHistoryDto(
    Guid NotificationId,
    string Type,
    string Channel,
    string Title,
    string Body,
    string Status,
    DateTime CreatedAt,
    DateTime? SentAt,
    bool IsRead
);
