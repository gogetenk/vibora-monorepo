using Ardalis.Result;
using MediatR;
using Vibora.Notifications.Domain;

namespace Vibora.Notifications.Application.Queries.GetNotificationHistory;

/// <summary>
/// Handler for retrieving notification history
/// Returns paginated list of notifications for a user
/// </summary>
internal sealed class GetNotificationHistoryQueryHandler
    : IRequestHandler<GetNotificationHistoryQuery, Result<List<NotificationHistoryDto>>>
{
    private readonly INotificationRepository _repository;

    public GetNotificationHistoryQueryHandler(INotificationRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<List<NotificationHistoryDto>>> Handle(
        GetNotificationHistoryQuery request,
        CancellationToken cancellationToken)
    {
        // Validate pagination parameters
        if (request.PageNumber < 1)
        {
            return Result<List<NotificationHistoryDto>>.Invalid(
                new ValidationError("PageNumber must be greater than 0", nameof(request.PageNumber)));
        }

        if (request.PageSize < 1 || request.PageSize > 100)
        {
            return Result<List<NotificationHistoryDto>>.Invalid(
                new ValidationError("PageSize must be between 1 and 100", nameof(request.PageSize)));
        }

        // Fetch notifications from repository
        var notifications = await _repository.GetHistoryByUserIdAsync(
            request.UserExternalId,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        // Map to DTOs
        var dtos = notifications.Select(n => new NotificationHistoryDto(
            n.NotificationId,
            n.Type.ToString(),
            n.Channel.ToString(),
            n.Content.Title,
            n.Content.Body,
            n.Status.ToString(),
            n.CreatedAt,
            n.SentAt
        )).ToList();

        return Result<List<NotificationHistoryDto>>.Success(dtos);
    }
}
