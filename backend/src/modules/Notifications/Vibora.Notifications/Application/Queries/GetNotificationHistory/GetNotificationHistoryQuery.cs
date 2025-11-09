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
