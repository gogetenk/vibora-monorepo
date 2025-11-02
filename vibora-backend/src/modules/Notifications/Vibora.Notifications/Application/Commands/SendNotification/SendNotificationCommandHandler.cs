using Ardalis.Result;
using MediatR;
using Microsoft.Extensions.Logging;
using Vibora.Notifications.Domain;
using Vibora.Notifications.Infrastructure.Services;

namespace Vibora.Notifications.Application.Commands.SendNotification;

/// <summary>
/// Handler for sending notifications
/// Follows the pattern: Create -> Persist -> Dispatch -> Update Status
/// </summary>
internal sealed class SendNotificationCommandHandler
    : IRequestHandler<SendNotificationCommand, Result<Guid>>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly INotificationDispatcher _dispatcher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SendNotificationCommandHandler> _logger;

    public SendNotificationCommandHandler(
        INotificationRepository notificationRepository,
        INotificationDispatcher dispatcher,
        IUnitOfWork unitOfWork,
        ILogger<SendNotificationCommandHandler> logger)
    {
        _notificationRepository = notificationRepository;
        _dispatcher = dispatcher;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(
        SendNotificationCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Sending {Type} notification to {UserExternalId} via {Channel}",
            request.Type, request.UserExternalId, request.Channel);

        return await Task.FromResult(CreateNotification(request))
            .BindAsync(notification => Task.FromResult(MarkAsSending(notification)))
            .BindAsync(notification => PersistNotification(notification, cancellationToken))
            .BindAsync(notification => DispatchNotification(notification, request.Recipient, cancellationToken))
            .BindAsync(notification => SaveChangesAsync(notification, cancellationToken))
            .MapAsync(notification => notification.NotificationId);
    }

    private static Result<Notification> CreateNotification(SendNotificationCommand request)
    {
        return Notification.Create(
            request.UserExternalId,
            request.Type,
            request.Channel,
            request.Content);
    }

    private static Result<Notification> MarkAsSending(Notification notification)
    {
        var markSendingResult = notification.MarkAsSending();

        return !markSendingResult.IsSuccess
            ? Result<Notification>.Error(string.Join(", ", markSendingResult.Errors))
            : Result<Notification>.Success(notification);
    }

    private async Task<Result<Notification>> PersistNotification(
        Notification notification,
        CancellationToken cancellationToken)
    {
        await _notificationRepository.AddAsync(notification, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<Notification>.Success(notification);
    }

    private async Task<Result<Notification>> DispatchNotification(
        Notification notification,
        string recipient,
        CancellationToken cancellationToken)
    {
        var sendResult = await _dispatcher.DispatchAsync(notification, recipient, cancellationToken);

        if (sendResult.IsSuccess)
        {
            var markSentResult = notification.MarkAsSent();
            if (markSentResult.IsSuccess)
            {
                _logger.LogInformation(
                    "Notification {NotificationId} sent successfully",
                    notification.NotificationId);
            }
            else
            {
                _logger.LogWarning(
                    "Failed to mark notification as sent: {Error}",
                    string.Join(", ", markSentResult.Errors));
            }
        }
        else
        {
            var errorMessage = string.Join(", ", sendResult.Errors);
            _logger.LogError(
                "Failed to send notification {NotificationId}: {Error}",
                notification.NotificationId,
                errorMessage);

            var markFailedResult = notification.MarkAsFailed(errorMessage);
            if (!markFailedResult.IsSuccess)
            {
                _logger.LogWarning(
                    "Failed to mark notification as failed: {Error}",
                    string.Join(", ", markFailedResult.Errors));
            }
        }

        // Always return success - notification has been handled (sent or marked as failed)
        return Result<Notification>.Success(notification);
    }

    private async Task<Result<Notification>> SaveChangesAsync(
        Notification notification,
        CancellationToken cancellationToken)
    {
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<Notification>.Success(notification);
    }
}
