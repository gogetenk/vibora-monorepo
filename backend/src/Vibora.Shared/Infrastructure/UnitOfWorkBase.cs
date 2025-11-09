using MediatR;
using Microsoft.EntityFrameworkCore;
using Vibora.Shared.Domain;

namespace Vibora.Shared.Infrastructure;

/// <summary>
/// Concrete implementation of Unit of Work pattern.
/// This is a SHARED INFRASTRUCTURE class used by all modules.
/// Each module wraps this with their own Application-level interface.
/// Manages transaction boundaries and domain event publishing via MediatR.
/// </summary>
public sealed class UnitOfWork<TDbContext>
    where TDbContext : DbContext
{
    private readonly TDbContext _dbContext;
    private readonly IPublisher _publisher;

    public UnitOfWork(TDbContext dbContext, IPublisher publisher)
    {
        _dbContext = dbContext;
        _publisher = publisher;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // 1. Collect all domain events from aggregate roots (in order)
        var aggregateRoots = _dbContext.ChangeTracker
            .Entries<AggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();

        var domainEvents = aggregateRoots
            .SelectMany(a => a.DomainEvents)
            .OrderBy(e => e.OccurredOn)
            .ToList();

        // 2. Save changes to database (transaction)
        var result = await _dbContext.SaveChangesAsync(cancellationToken);

        // 3. Publish domain events via MediatR (after successful transaction)
        // Handlers can transform them to integration events if needed
        foreach (var domainEvent in domainEvents)
        {
            await _publisher.Publish(domainEvent, cancellationToken);
        }

        // 4. Clear domain events from aggregate roots
        foreach (var aggregateRoot in aggregateRoots)
        {
            aggregateRoot.ClearDomainEvents();
        }

        return result;
    }
}
