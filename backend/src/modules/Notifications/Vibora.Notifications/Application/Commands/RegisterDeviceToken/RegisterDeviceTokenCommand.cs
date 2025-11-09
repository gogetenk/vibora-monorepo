using Ardalis.Result;
using MediatR;

namespace Vibora.Notifications.Application.Commands.RegisterDeviceToken;

/// <summary>
/// Command to register a device token for push notifications
/// Creates notification preferences if they don't exist (lazy creation)
/// </summary>
internal sealed record RegisterDeviceTokenCommand(
    string UserExternalId,
    string DeviceToken,
    string? Email = null // For lazy creation
) : IRequest<Result>;
