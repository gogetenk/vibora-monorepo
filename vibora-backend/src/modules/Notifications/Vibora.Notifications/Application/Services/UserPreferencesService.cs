using Microsoft.Extensions.Logging;
using Vibora.Users.Contracts.Services;

namespace Vibora.Notifications.Application.Services;

/// <summary>
/// Service to fetch user notification preferences from Users module
/// Encapsulates cross-module communication via IUsersServiceClient abstraction
/// Supports both monolith (in-process) and microservices (HTTP) modes
/// </summary>
public sealed class UserPreferencesService
{
    private readonly IUsersServiceClient _usersServiceClient;
    private readonly ILogger<UserPreferencesService> _logger;

    public UserPreferencesService(
        IUsersServiceClient usersServiceClient, 
        ILogger<UserPreferencesService> logger)
    {
        _usersServiceClient = usersServiceClient;
        _logger = logger;
    }

    /// <summary>
    /// Gets the device token for a user for push notifications
    /// Returns null if user has no preferences or push is disabled
    /// </summary>
    public async Task<string?> GetUserDeviceTokenAsync(string userExternalId, CancellationToken cancellationToken)
    {
        try
        {
            var settingsResult = await _usersServiceClient.GetUserNotificationSettingsAsync(
                userExternalId, 
                cancellationToken);

            if (!settingsResult.IsSuccess)
            {
                _logger.LogWarning(
                    "No notification settings found for user {UserExternalId}. Status: {Status}, Errors: {Errors}",
                    userExternalId,
                    settingsResult.Status,
                    string.Join(", ", settingsResult.Errors));
                return null;
            }

            var settings = settingsResult.Value;

            // Only return device token if push is enabled
            if (!settings.PushEnabled)
            {
                _logger.LogDebug(
                    "Push notifications disabled for user {UserExternalId}",
                    userExternalId);
                return null;
            }

            if (string.IsNullOrWhiteSpace(settings.DeviceToken))
            {
                _logger.LogWarning(
                    "User {UserExternalId} has push enabled but no device token registered",
                    userExternalId);
                return null;
            }

            return settings.DeviceToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to fetch device token for user {UserExternalId}. Notification will be skipped.",
                userExternalId);
            return null;
        }
    }

    /// <summary>
    /// Check if user has push notifications enabled
    /// Returns false if preferences not found or push disabled
    /// </summary>
    public async Task<bool> IsPushEnabledAsync(string userExternalId, CancellationToken cancellationToken)
    {
        try
        {
            var settingsResult = await _usersServiceClient.GetUserNotificationSettingsAsync(
                userExternalId, 
                cancellationToken);

            if (!settingsResult.IsSuccess)
            {
                _logger.LogDebug(
                    "No notification settings found for user {UserExternalId} - defaulting to disabled. Status: {Status}",
                    userExternalId,
                    settingsResult.Status);
                return false;
            }

            return settingsResult.Value.PushEnabled;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check push enabled for user {UserExternalId}", userExternalId);
            return false;
        }
    }

    /// <summary>
    /// Gets device tokens for multiple users in a single batch call
    /// More efficient for scenarios where multiple users need to be notified
    /// Returns dictionary with userId -> deviceToken (only for users with push enabled)
    /// </summary>
    public async Task<Dictionary<string, string>> GetDeviceTokensBatchAsync(
        IEnumerable<string> userExternalIds, 
        CancellationToken cancellationToken)
    {
        try
        {
            var settingsBatch = await _usersServiceClient.GetUserNotificationSettingsBatchAsync(
                userExternalIds, 
                cancellationToken);

            var result = new Dictionary<string, string>();

            foreach (var (userId, settings) in settingsBatch)
            {
                // Only include if push enabled and has device token
                if (settings.PushEnabled && !string.IsNullOrWhiteSpace(settings.DeviceToken))
                {
                    result[userId] = settings.DeviceToken;
                }
                else
                {
                    _logger.LogDebug(
                        "Skipping user {UserExternalId} - push disabled or no device token",
                        userId);
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch device tokens batch");
            return new Dictionary<string, string>();
        }
    }
}
