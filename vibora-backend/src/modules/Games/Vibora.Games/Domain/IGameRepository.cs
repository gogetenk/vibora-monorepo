using Ardalis.Result;
using Vibora.Games.Domain;

namespace Vibora.Games.Domain;

/// <summary>
/// Repository interface for Game aggregate root
/// </summary>
internal interface IGameRepository
{
    /// <summary>
    /// Gets a game by its ID, returning Result.NotFound if not found
    /// </summary>
    Task<Result<Game>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all open games scheduled after the specified date
    /// </summary>
    Task<List<Game>> GetOpenGamesAsync(DateTime afterDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets games where the specified user is participating
    /// </summary>
    Task<List<Game>> GetGamesByUserAsync(string userExternalId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a game with all its participations loaded, returning Result.NotFound if not found
    /// </summary>
    Task<Result<Game>> GetByIdWithParticipationsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new game to the repository
    /// </summary>
    void Add(Game game);

    /// <summary>
    /// Adds a new participation to the repository
    /// </summary>
    void AddParticipation(Participation participation);

    /// <summary>
    /// Adds a new guest participant to the repository
    /// </summary>
    void AddGuestParticipant(GuestParticipant guestParticipant);

    /// <summary>
    /// Updates an existing game
    /// </summary>
    void Update(Game game);

    /// <summary>
    /// Removes a game from the repository
    /// </summary>
    void Remove(Game game);

    /// <summary>
    /// Gets available games (Open or Full status, future dates) with optional filters and pagination
    /// </summary>
    Task<(List<Game> Games, int TotalCount)> GetAvailableGamesAsync(
        string? location = null,
        string? skillLevel = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts the number of unique games a user has participated in
    /// </summary>
    Task<int> CountUserParticipationsAsync(string userExternalId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all guest participants matching the provided contact information (phone or email)
    /// Used for Phase 3B automatic guest reconciliation during signup
    /// </summary>
    Task<List<GuestParticipant>> GetGuestParticipantsByContactAsync(
        string? phoneNumber,
        string? email,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a guest participant from the repository
    /// </summary>
    void RemoveGuestParticipant(GuestParticipant guestParticipant);
}
