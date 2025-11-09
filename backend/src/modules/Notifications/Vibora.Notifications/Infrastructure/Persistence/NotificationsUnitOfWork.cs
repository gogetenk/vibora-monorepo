using MediatR;
using Vibora.Notifications.Application;
using Vibora.Notifications.Infrastructure.Data;
using Vibora.Shared.Infrastructure;

namespace Vibora.Notifications.Infrastructure.Persistence;

/// <summary>
/// Implementation of IUnitOfWork for the Notifications module.
/// Wraps the shared generic UnitOfWork with module-specific DbContext.
/// </summary>
internal sealed class NotificationsUnitOfWork : IUnitOfWork
{
    private readonly UnitOfWork<NotificationsDbContext> _sharedUnitOfWork;

    public NotificationsUnitOfWork(NotificationsDbContext dbContext, IPublisher publisher)
    {
        _sharedUnitOfWork = new UnitOfWork<NotificationsDbContext>(dbContext, publisher);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _sharedUnitOfWork.SaveChangesAsync(cancellationToken);
    }
}
