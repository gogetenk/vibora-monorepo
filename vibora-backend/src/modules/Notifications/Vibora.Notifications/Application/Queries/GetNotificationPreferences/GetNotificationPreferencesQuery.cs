using Ardalis.Result;
using MediatR;

namespace Vibora.Notifications.Application.Queries.GetNotificationPreferences;

/// <summary>
/// Query to get user notification preferences
/// Uses lazy creation pattern - creates default preferences if not found
/// </summary>
internal sealed record GetNotificationPreferencesQuery(
    string UserExternalId,
    string? Email = null // Optional email for lazy creation
) : IRequest<Result<GetNotificationPreferencesResult>>;

/// <summary>
/// Result DTO for GetNotificationPreferencesQuery
/// Contains notification preferences and contact information
/// </summary>
public sealed record GetNotificationPreferencesResult(
    string? DeviceToken,
    string? PhoneNumber,
    string? Email,
    bool PushEnabled,
    bool SmsEnabled,
    bool EmailEnabled
);
