using Ardalis.Result;
using Microsoft.EntityFrameworkCore;
using Vibora.Games.Domain;
using Vibora.Games.Infrastructure.Data;

namespace Vibora.Games.Infrastructure.Persistence;

internal sealed class GameRepository : IGameRepository
{
    private readonly GamesDbContext _dbContext;

    public GameRepository(GamesDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<Game>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var game = await _dbContext.Games
            .Include(g => g.Participations)
            .Include(g => g.GuestParticipants)
            .AsSplitQuery() // Avoid cartesian explosion by splitting into multiple queries
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

        return game == null
            ? Result<Game>.NotFound($"Game with ID '{id}' not found")
            : Result.Success(game);
    }

    public async Task<Result<Game>> GetByIdWithParticipationsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var game = await _dbContext.Games
            .Include(g => g.Participations)
            .Include(g => g.GuestParticipants)
            .AsSplitQuery() // Avoid cartesian explosion by splitting into multiple queries
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

        return game == null
            ? Result<Game>.NotFound($"Game with ID '{id}' not found")
            : Result.Success(game);
    }

    public async Task<List<Game>> GetOpenGamesAsync(DateTime afterDate, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Games
            .Include(g => g.Participations)
            .AsNoTracking() // Read-only query
            .AsSplitQuery() // Avoid cartesian explosion
            .Where(g => g.Status == GameStatus.Open && g.DateTime >= afterDate)
            .OrderBy(g => g.DateTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Game>> GetGamesByUserAsync(string userExternalId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Games
            .Include(g => g.Participations)
            .Include(g => g.GuestParticipants) // Include guests
            .AsSplitQuery() // Avoid cartesian explosion by splitting into multiple queries
            .Where(g => 
                g.Participations.Any(p => p.UserExternalId == userExternalId) || // Normal user
                g.GuestParticipants.Any(gp => gp.GuestExternalId == userExternalId) || // Guest user
                g.HostExternalId == userExternalId) // Host (may not have participation yet)
            .OrderByDescending(g => g.DateTime)
            .ToListAsync(cancellationToken);
    }

    public void Add(Game game)
    {
        _dbContext.Games.Add(game);
    }

    public void AddParticipation(Participation participation)
    {
        _dbContext.Participations.Add(participation);
    }

    public void AddGuestParticipant(GuestParticipant guestParticipant)
    {
        _dbContext.GuestParticipants.Add(guestParticipant);
    }

    public void Update(Game game)
    {
        _dbContext.Games.Update(game);
    }

    public void Remove(Game game)
    {
        _dbContext.Games.Remove(game);
    }

    public async Task<(List<Game> Games, int TotalCount)> GetAvailableGamesAsync(
        string? location = null,
        string? skillLevel = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        // Build query with filters
        var query = _dbContext.Games
            .AsNoTracking() // Read-only query
            .Where(g => g.Status == GameStatus.Open || g.Status == GameStatus.Full) // Available games only
            .Where(g => g.DateTime > DateTime.UtcNow); // Future games only

        // Apply optional filters
        if (!string.IsNullOrWhiteSpace(location))
        {
            query = query.Where(g => g.Location.Contains(location));
        }

        if (!string.IsNullOrWhiteSpace(skillLevel))
        {
            query = query.Where(g => g.SkillLevel == skillLevel);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(g => g.DateTime >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(g => g.DateTime <= toDate.Value);
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply pagination and ordering
        var games = await query
            .OrderBy(g => g.DateTime) // Nearest games first
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (games, totalCount);
    }

    public async Task<int> CountUserParticipationsAsync(string userExternalId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Participations
            .AsNoTracking()
            .Where(p => p.UserExternalId == userExternalId)
            .Select(p => p.GameId)
            .Distinct()
            .CountAsync(cancellationToken);
    }

    public async Task<List<GuestParticipant>> GetGuestParticipantsByContactAsync(
        string? phoneNumber,
        string? email,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email?.Trim().ToLowerInvariant();

        return await _dbContext.GuestParticipants
            .AsNoTracking()
            .Where(gp =>
                (!string.IsNullOrWhiteSpace(phoneNumber) && gp.PhoneNumber == phoneNumber) ||
                (!string.IsNullOrWhiteSpace(normalizedEmail) && gp.Email == normalizedEmail))
            .ToListAsync(cancellationToken);
    }

    public void RemoveGuestParticipant(GuestParticipant guestParticipant)
    {
        _dbContext.GuestParticipants.Remove(guestParticipant);
    }

    public async Task<List<string>> GetGameParticipantUserIdsAsync(
        Guid gameId,
        string? excludeUserExternalId = null,
        CancellationToken cancellationToken = default)
    {
        var userIds = await _dbContext.Participations
            .AsNoTracking()
            .Where(p => p.GameId == gameId)
            .Select(p => p.UserExternalId)
            .Where(id => excludeUserExternalId == null || id != excludeUserExternalId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return userIds;
    }

    public async Task<List<Game>> GetGamesStartingInTimeWindowAsync(
        DateTime fromTime,
        DateTime toTime,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Games
            .Include(g => g.Participations)
            .Include(g => g.GuestParticipants)
            .AsNoTracking() // Read-only query
            .AsSplitQuery() // Avoid cartesian explosion
            .Where(g => g.DateTime >= fromTime && g.DateTime <= toTime && g.Status != GameStatus.Canceled)
            .OrderBy(g => g.DateTime)
            .ToListAsync(cancellationToken);
    }
}
