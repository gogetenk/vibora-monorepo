using Ardalis.Result;
using MediatR;

namespace Vibora.Notifications.Application.Commands.DeleteNotification;

/// <summary>
/// Command to soft delete a notification
/// </summary>
internal sealed record DeleteNotificationCommand(
    Guid NotificationId,
    string UserExternalId
) : IRequest<Result>;
