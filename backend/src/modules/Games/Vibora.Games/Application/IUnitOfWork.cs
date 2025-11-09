namespace Vibora.Games.Application;

/// <summary>
/// Unit of Work abstraction for the Games module.
/// Belongs to the Application layer to maintain Clean Architecture principles.
/// Implementation details (DbContext) are hidden in Infrastructure.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Saves all pending changes and publishes domain events
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
