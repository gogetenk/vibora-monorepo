using Microsoft.Extensions.Logging;
using Vibora.Notifications.Domain;

namespace Vibora.Notifications.Application.Services;

/// <summary>
/// Service to fetch user notification preferences
/// Uses local UserNotificationPreferencesRepository (no cross-module dependency)
/// </summary>
internal sealed class UserPreferencesService
{
    private readonly IUserNotificationPreferencesRepository _preferencesRepository;
    private readonly ILogger<UserPreferencesService> _logger;

    public UserPreferencesService(
        IUserNotificationPreferencesRepository preferencesRepository,
        ILogger<UserPreferencesService> logger)
    {
        _preferencesRepository = preferencesRepository;
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
            var preferences = await _preferencesRepository.GetByUserIdAsync(
                userExternalId,
                cancellationToken);

            if (preferences == null)
            {
                _logger.LogWarning(
                    "No notification preferences found for user {UserExternalId}",
                    userExternalId);
                return null;
            }

            // Only return device token if push is enabled
            if (!preferences.PushEnabled)
            {
                _logger.LogDebug(
                    "Push notifications disabled for user {UserExternalId}",
                    userExternalId);
                return null;
            }

            if (string.IsNullOrWhiteSpace(preferences.DeviceToken))
            {
                _logger.LogWarning(
                    "User {UserExternalId} has push enabled but no device token registered",
                    userExternalId);
                return null;
            }

            return preferences.DeviceToken;
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
            var preferences = await _preferencesRepository.GetByUserIdAsync(
                userExternalId,
                cancellationToken);

            if (preferences == null)
            {
                _logger.LogDebug(
                    "No notification preferences found for user {UserExternalId} - defaulting to disabled",
                    userExternalId);
                return false;
            }

            return preferences.PushEnabled;
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
            var preferencesBatch = await _preferencesRepository.GetBatchAsync(
                userExternalIds,
                cancellationToken);

            var result = new Dictionary<string, string>();

            foreach (var (userId, preferences) in preferencesBatch)
            {
                // Only include if push enabled and has device token
                if (preferences.PushEnabled && !string.IsNullOrWhiteSpace(preferences.DeviceToken))
                {
                    result[userId] = preferences.DeviceToken;
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
