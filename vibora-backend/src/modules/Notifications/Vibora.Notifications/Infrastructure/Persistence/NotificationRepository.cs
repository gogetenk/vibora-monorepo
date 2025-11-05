using Microsoft.EntityFrameworkCore;
using Vibora.Notifications.Domain;
using Vibora.Notifications.Infrastructure.Data;

namespace Vibora.Notifications.Infrastructure.Persistence;

/// <summary>
/// Repository implementation for Notification aggregate
/// Handles data access for notifications
/// </summary>
internal sealed class NotificationRepository : INotificationRepository
{
    private readonly NotificationsDbContext _dbContext;

    public NotificationRepository(NotificationsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Notification?> GetByIdAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Notifications
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.NotificationId == notificationId, cancellationToken);
    }

    public async Task<List<Notification>> GetPendingByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId && n.Status == NotificationStatus.Pending)
            .OrderBy(n => n.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Notification>> GetPendingForRetryAsync(int limit = 100, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Notifications
            .AsNoTracking()
            .Where(n => n.Status == NotificationStatus.Pending)
            .OrderBy(n => n.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Notification>> GetHistoryByUserIdAsync(
        string userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Notifications
            .AsNoTracking() // Read-only query for history
            .Where(n => n.UserId == userId && n.DeletedAt == null)
            .OrderByDescending(n => n.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        await _dbContext.Notifications.AddAsync(notification, cancellationToken);
    }

    public void Add(Notification notification)
    {
        _dbContext.Notifications.Add(notification);
    }

    public void Update(Notification notification)
    {
        _dbContext.Notifications.Update(notification);
    }

    public void Delete(Notification notification)
    {
        _dbContext.Notifications.Remove(notification);
    }
}
