namespace Vibora.Users.Domain;

/// <summary>
/// Repository interface for UserNotificationSettings aggregate root
/// </summary>
internal interface IUserNotificationSettingsRepository
{
    /// <summary>
    /// Gets notification settings for a user by their external ID
    /// </summary>
    Task<UserNotificationSettings?> GetByUserExternalIdAsync(string userExternalId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds new notification settings
    /// </summary>
    void Add(UserNotificationSettings settings);

    /// <summary>
    /// Updates existing notification settings
    /// </summary>
    void Update(UserNotificationSettings settings);

    /// <summary>
    /// Removes notification settings
    /// </summary>
    void Remove(UserNotificationSettings settings);
}
