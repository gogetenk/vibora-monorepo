using Ardalis.Result;
using MediatR;

namespace Vibora.Users.Application.Commands.UpdateNotificationSettings;

/// <summary>
/// Command to update user notification settings
/// Creates settings if they don't exist
/// </summary>
public sealed record UpdateNotificationSettingsCommand(
    string UserExternalId,
    string? DeviceToken = null,
    string? PhoneNumber = null,
    string? Email = null,
    bool? PushEnabled = null,
    bool? SmsEnabled = null,
    bool? EmailEnabled = null
) : IRequest<Result>;
