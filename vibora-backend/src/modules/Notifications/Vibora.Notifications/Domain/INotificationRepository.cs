namespace Vibora.Notifications.Domain;

/// <summary>
/// Repository interface for Notification aggregate
/// Follows repository pattern - only aggregate roots have repositories
/// </summary>
internal interface INotificationRepository
{
    /// <summary>
    /// Get a notification by its ID
    /// </summary>
    Task<Notification?> GetByIdAsync(Guid notificationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all pending notifications for a specific user
    /// </summary>
    Task<List<Notification>> GetPendingByUserIdAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all pending notifications ready for retry
    /// </summary>
    Task<List<Notification>> GetPendingForRetryAsync(int limit = 100, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get notification history for a user (for UI display)
    /// </summary>
    Task<List<Notification>> GetHistoryByUserIdAsync(
        string userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Add a new notification (async for consistency with modern patterns)
    /// </summary>
    Task AddAsync(Notification notification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add a new notification (legacy sync method)
    /// </summary>
    void Add(Notification notification);

    /// <summary>
    /// Update an existing notification (for status changes)
    /// </summary>
    void Update(Notification notification);

    /// <summary>
    /// Delete a notification (for cleanup/GDPR)
    /// </summary>
    void Delete(Notification notification);
}
