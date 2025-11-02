using MediatR;
using Vibora.Shared.Infrastructure;
using Vibora.Users.Application;
using Vibora.Users.Infrastructure.Data;

namespace Vibora.Users.Infrastructure.Persistence;

/// <summary>
/// Implementation of IUnitOfWork for the Users module.
/// Wraps the shared generic UnitOfWork with module-specific DbContext.
/// </summary>
internal sealed class UsersUnitOfWork : IUnitOfWork
{
    private readonly UnitOfWork<UsersDbContext> _sharedUnitOfWork;

    public UsersUnitOfWork(UsersDbContext dbContext, IPublisher publisher)
    {
        _sharedUnitOfWork = new UnitOfWork<UsersDbContext>(dbContext, publisher);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _sharedUnitOfWork.SaveChangesAsync(cancellationToken);
    }
}
