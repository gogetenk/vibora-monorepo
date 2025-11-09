namespace Vibora.Notifications.Domain;

/// <summary>
/// Repository for UserNotificationPreferences aggregate
/// Handles persistence of user notification settings
/// </summary>
internal interface IUserNotificationPreferencesRepository
{
    /// <summary>
    /// Get preferences by user external ID (returns null if not found)
    /// </summary>
    Task<UserNotificationPreferences?> GetByUserIdAsync(string userExternalId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get or create preferences for a user (lazy creation pattern)
    /// Creates default preferences if they don't exist
    /// </summary>
    Task<UserNotificationPreferences> GetOrCreateAsync(string userExternalId, string? email = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get preferences for multiple users in a batch
    /// Returns only existing preferences (no lazy creation)
    /// </summary>
    Task<Dictionary<string, UserNotificationPreferences>> GetBatchAsync(IEnumerable<string> userExternalIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add new preferences
    /// </summary>
    Task AddAsync(UserNotificationPreferences preferences, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update existing preferences (tracked by EF Core, call UnitOfWork.SaveChangesAsync)
    /// </summary>
    void Update(UserNotificationPreferences preferences);
}
