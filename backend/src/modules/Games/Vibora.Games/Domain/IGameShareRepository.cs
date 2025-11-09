namespace Vibora.Games.Domain;

/// <summary>
/// Repository interface for GameShare aggregate
/// </summary>
public interface IGameShareRepository
{
    Task<GameShare?> GetByTokenAsync(string shareToken, CancellationToken cancellationToken = default);
    Task<GameShare?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<GameShare>> GetByGameIdAsync(Guid gameId, CancellationToken cancellationToken = default);
    Task AddAsync(GameShare gameShare, CancellationToken cancellationToken = default);
}
