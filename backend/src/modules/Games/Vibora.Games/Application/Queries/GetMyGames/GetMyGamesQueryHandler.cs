using Ardalis.Result;
using MediatR;
using Vibora.Games.Domain;

namespace Vibora.Games.Application.Queries.GetMyGames;

/// <summary>
/// Handler to get user's upcoming games (created or joined)
/// Uses Repository pattern (Clean Architecture)
/// </summary>
internal sealed class GetMyGamesQueryHandler : IRequestHandler<GetMyGamesQuery, Result<MyGamesResult>>
{
    private readonly IGameRepository _gameRepository;

    public GetMyGamesQueryHandler(IGameRepository gameRepository)
    {
        _gameRepository = gameRepository;
    }

    public async Task<Result<MyGamesResult>> Handle(GetMyGamesQuery request, CancellationToken cancellationToken)
    {
        // Validate
        if (string.IsNullOrWhiteSpace(request.UserExternalId))
        {
            return Result.Invalid(new ValidationError("UserExternalId is required"));
        }

        // Get user's games from repository
        var games = await _gameRepository.GetGamesByUserAsync(request.UserExternalId, cancellationToken);

        // Filter: only upcoming games (future)
        var upcomingGames = games
            .Where(g => g.DateTime > DateTime.UtcNow)
            .ToList();

        // Map to DTOs
        var gameDtos = upcomingGames.Select(g => new MyGameDto(
            g.Id,
            g.DateTime,
            g.Location,
            g.SkillLevel,
            g.MaxPlayers,
            g.CurrentPlayers,
            g.Status.ToString(),
            g.HostExternalId == request.UserExternalId // IsHost
        )).ToList();

        return Result.Success(new MyGamesResult(
            gameDtos,
            gameDtos.Count
        ));
    }
}
