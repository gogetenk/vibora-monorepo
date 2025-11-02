using Ardalis.Result;
using MediatR;

namespace Vibora.Users.Contracts.Queries;

/// <summary>
/// Query to get user notification settings
/// Exposed publicly for cross-module communication (in-process calls)
/// </summary>
public sealed record GetUserNotificationSettingsQuery(string UserExternalId) 
    : IRequest<Result<UserNotificationSettingsResult>>;

/// <summary>
/// Result DTO for GetUserNotificationSettingsQuery
/// Contains notification preferences and contact information
/// </summary>
public sealed record UserNotificationSettingsResult(
    string UserExternalId,
    string? DeviceToken,
    string? PhoneNumber,
    string? Email,
    bool PushEnabled,
    bool SmsEnabled,
    bool EmailEnabled
);
