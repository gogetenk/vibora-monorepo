using Ardalis.Result;
using MediatR;
using Vibora.Notifications.Domain;

namespace Vibora.Notifications.Application.Commands.SendNotification;

/// <summary>
/// Command to send a notification to a user via a specific channel
/// </summary>
internal sealed record SendNotificationCommand(
    string UserExternalId,
    NotificationType Type,
    NotificationChannel Channel,
    string Recipient, // Device token, email, or phone
    NotificationContent Content
) : IRequest<Result<Guid>>;
