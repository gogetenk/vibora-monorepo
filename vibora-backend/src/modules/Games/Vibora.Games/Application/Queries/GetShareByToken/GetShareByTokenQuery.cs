using Ardalis.Result;
using MediatR;

namespace Vibora.Games.Application.Queries.GetShareByToken;

/// <summary>
/// Query to get a game share by its token (for redirect + view count increment)
/// </summary>
internal sealed record GetShareByTokenQuery(
    string ShareToken
) : IRequest<Result<GetShareByTokenResult>>;

/// <summary>
/// Result containing share information and the game to redirect to
/// </summary>
internal sealed record GetShareByTokenResult(
    Guid GameId,
    Guid GameShareId,
    string ShareToken,
    int ViewCount,
    bool IsExpired,
    GameSummaryDto Game // Include game details for direct display
);

/// <summary>
/// Summary of game information for share page
/// </summary>
internal sealed record GameSummaryDto(
    Guid Id,
    DateTime DateTime,
    string Location,
    string SkillLevel,
    int MaxPlayers,
    int CurrentPlayers,
    string Status,
    string HostDisplayName
);
