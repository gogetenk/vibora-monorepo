using Microsoft.EntityFrameworkCore;
using Vibora.Games.Domain;

namespace Vibora.Games.Infrastructure.Data;

internal sealed class GameShareRepository : IGameShareRepository
{
    private readonly GamesDbContext _context;

    public GameShareRepository(GamesDbContext context)
    {
        _context = context;
    }

    public async Task<GameShare?> GetByTokenAsync(string shareToken, CancellationToken cancellationToken = default)
    {
        return await _context.GameShares
            .Include(gs => gs.Game)
            .FirstOrDefaultAsync(gs => gs.ShareToken == shareToken, cancellationToken);
    }

    public async Task<GameShare?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.GameShares
            .Include(gs => gs.Game)
            .FirstOrDefaultAsync(gs => gs.Id == id, cancellationToken);
    }

    public async Task<List<GameShare>> GetByGameIdAsync(Guid gameId, CancellationToken cancellationToken = default)
    {
        return await _context.GameShares
            .Where(gs => gs.GameId == gameId)
            .OrderByDescending(gs => gs.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(GameShare gameShare, CancellationToken cancellationToken = default)
    {
        await _context.GameShares.AddAsync(gameShare, cancellationToken);
    }
}
