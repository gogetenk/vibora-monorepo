using MediatR;
using Vibora.Games.Application;
using Vibora.Games.Infrastructure.Data;
using Vibora.Shared.Infrastructure;

namespace Vibora.Games.Infrastructure.Persistence;

/// <summary>
/// Implementation of IUnitOfWork for the Games module.
/// Wraps the shared generic UnitOfWork with module-specific DbContext.
/// </summary>
internal sealed class GamesUnitOfWork : IUnitOfWork
{
    private readonly UnitOfWork<GamesDbContext> _sharedUnitOfWork;

    public GamesUnitOfWork(GamesDbContext dbContext, IPublisher publisher)
    {
        _sharedUnitOfWork = new UnitOfWork<GamesDbContext>(dbContext, publisher);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _sharedUnitOfWork.SaveChangesAsync(cancellationToken);
    }
}
