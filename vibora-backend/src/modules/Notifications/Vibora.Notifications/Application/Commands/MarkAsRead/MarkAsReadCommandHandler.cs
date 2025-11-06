using Ardalis.Result;
using MediatR;
using Vibora.Notifications.Domain;

namespace Vibora.Notifications.Application.Commands.MarkAsRead;

/// <summary>
/// Handler for marking a notification as read
/// Validates ownership and updates IsRead flag
/// </summary>
internal sealed class MarkAsReadCommandHandler : IRequestHandler<MarkAsReadCommand, Result>
{
    private readonly INotificationRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public MarkAsReadCommandHandler(
        INotificationRepository repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(
        MarkAsReadCommand request,
        CancellationToken cancellationToken)
    {
        var notification = await _repository.GetByIdAsync(request.NotificationId, cancellationToken);

        if (notification == null)
            return Result.NotFound($"Notification {request.NotificationId} not found");

        if (notification.UserId != request.UserExternalId)
            return Result.Forbidden("You do not have permission to update this notification");

        var markResult = notification.MarkAsRead();
        if (!markResult.IsSuccess)
            return markResult;

        _repository.Update(notification);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
