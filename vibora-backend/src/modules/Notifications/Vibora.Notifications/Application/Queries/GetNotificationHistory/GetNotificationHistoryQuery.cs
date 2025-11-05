using Ardalis.Result;
using MediatR;

namespace Vibora.Notifications.Application.Queries.GetNotificationHistory;

/// <summary>
/// Query to get notification history for a user
/// Supports pagination for large result sets
/// </summary>
internal sealed record GetNotificationHistoryQuery(
    string UserExternalId,
    int PageNumber = 1,
    int PageSize = 20
) : IRequest<Result<List<NotificationHistoryDto>>>;

/// <summary>
/// DTO for notification history response
/// Flat structure for easy UI binding
/// </summary>
public sealed record NotificationHistoryDto(
    Guid Id,
    string Type,
    string Channel,
    string Title,
    string Body,
    string Status,
    DateTime CreatedAt,
    DateTime? SentAt,
    bool IsRead
);
