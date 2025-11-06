using Ardalis.Result;
using MediatR;
using Vibora.Notifications.Domain;

namespace Vibora.Notifications.Application.Commands.DeleteNotification;

/// <summary>
/// Handler for soft deleting a notification
/// Validates ownership and sets DeletedAt timestamp
/// </summary>
internal sealed class DeleteNotificationCommandHandler : IRequestHandler<DeleteNotificationCommand, Result>
{
    private readonly INotificationRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteNotificationCommandHandler(
        INotificationRepository repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(
        DeleteNotificationCommand request,
        CancellationToken cancellationToken)
    {
        var notification = await _repository.GetByIdAsync(request.NotificationId, cancellationToken);

        if (notification == null)
            return Result.NotFound($"Notification {request.NotificationId} not found");

        if (notification.UserId != request.UserExternalId)
            return Result.Forbidden("You do not have permission to delete this notification");

        var deleteResult = notification.SoftDelete();
        if (!deleteResult.IsSuccess)
            return deleteResult;

        _repository.Update(notification);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
