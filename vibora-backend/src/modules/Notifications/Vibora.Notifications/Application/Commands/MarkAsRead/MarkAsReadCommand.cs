using Ardalis.Result;
using MediatR;

namespace Vibora.Notifications.Application.Commands.MarkAsRead;

/// <summary>
/// Command to mark a notification as read
/// </summary>
internal sealed record MarkAsReadCommand(
    Guid NotificationId,
    string UserExternalId
) : IRequest<Result>;
