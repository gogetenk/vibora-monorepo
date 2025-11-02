using Ardalis.Result;
using MediatR;

namespace Vibora.Games.Application.Queries.GetMyGames;

/// <summary>
/// Query to get user's upcoming games (created or joined)
/// </summary>
internal sealed record GetMyGamesQuery(
    string UserExternalId
) : IRequest<Result<MyGamesResult>>;

/// <summary>
/// Result containing user's upcoming games
/// </summary>
internal sealed record MyGamesResult(
    List<MyGameDto> Games,
    int TotalCount
);

internal sealed record MyGameDto(
    Guid Id,
    DateTime DateTime,
    string Location,
    string SkillLevel,
    int MaxPlayers,
    int CurrentPlayers,
    string Status,
    bool IsHost
);
