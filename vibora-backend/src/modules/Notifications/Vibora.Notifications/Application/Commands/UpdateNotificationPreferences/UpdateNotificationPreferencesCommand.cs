using Ardalis.Result;
using MediatR;

namespace Vibora.Notifications.Application.Commands.UpdateNotificationPreferences;

/// <summary>
/// Command to update user notification preferences
/// Handles partial updates (only specified fields are updated)
/// Uses lazy creation - creates default preferences if they don't exist
/// </summary>
internal sealed record UpdateNotificationPreferencesCommand(
    string UserExternalId,
    string? Email = null, // For lazy creation
    bool? PushEnabled = null,
    bool? SmsEnabled = null,
    bool? EmailEnabled = null
) : IRequest<Result>;
