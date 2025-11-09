using Microsoft.EntityFrameworkCore;
using Vibora.Notifications.Domain;
using Vibora.Notifications.Infrastructure.Data;

namespace Vibora.Notifications.Infrastructure.Persistence;

/// <summary>
/// Repository implementation for UserNotificationPreferences
/// Uses NotificationsDbContext for data access
/// </summary>
internal sealed class UserNotificationPreferencesRepository : IUserNotificationPreferencesRepository
{
    private readonly NotificationsDbContext _dbContext;

    public UserNotificationPreferencesRepository(NotificationsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UserNotificationPreferences?> GetByUserIdAsync(
        string userExternalId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserNotificationPreferences
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserExternalId == userExternalId, cancellationToken);
    }

    public async Task<UserNotificationPreferences> GetOrCreateAsync(
        string userExternalId,
        string? email = null,
        CancellationToken cancellationToken = default)
    {
        // Try to get existing preferences
        var existing = await _dbContext.UserNotificationPreferences
            .FirstOrDefaultAsync(p => p.UserExternalId == userExternalId, cancellationToken);

        if (existing != null)
        {
            return existing;
        }

        // Create default preferences (lazy creation)
        var newPreferences = UserNotificationPreferences.CreateDefault(userExternalId, email);
        await _dbContext.UserNotificationPreferences.AddAsync(newPreferences, cancellationToken);

        // Save immediately to avoid multiple creation attempts
        await _dbContext.SaveChangesAsync(cancellationToken);

        return newPreferences;
    }

    public async Task<Dictionary<string, UserNotificationPreferences>> GetBatchAsync(
        IEnumerable<string> userExternalIds,
        CancellationToken cancellationToken = default)
    {
        var userIds = userExternalIds.ToList();

        var preferences = await _dbContext.UserNotificationPreferences
            .AsNoTracking()
            .Where(p => userIds.Contains(p.UserExternalId))
            .ToListAsync(cancellationToken);

        return preferences.ToDictionary(p => p.UserExternalId);
    }

    public async Task AddAsync(UserNotificationPreferences preferences, CancellationToken cancellationToken = default)
    {
        await _dbContext.UserNotificationPreferences.AddAsync(preferences, cancellationToken);
    }

    public void Update(UserNotificationPreferences preferences)
    {
        _dbContext.UserNotificationPreferences.Update(preferences);
    }
}
