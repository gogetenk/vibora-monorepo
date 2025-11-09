using MediatR;
using Ardalis.Result;

namespace Vibora.Games.Application.Queries.SearchGames;

internal record SearchGamesQuery(
    string When, // Will be validated and parsed in handler
    string? Where = null,
    int? Level = null,
    double? Latitude = null,
    double? Longitude = null,
    int RadiusKm = 10
) : IRequest<Result<SearchGamesQueryResponse>>;

internal record SearchGamesQueryResponse(
    List<GameMatchDto> PerfectMatches,
    List<GameMatchDto> PartialMatches
);

internal record GameMatchDto(
    Guid Id,
    DateTime DateTime,
    string Location,
    int? SkillLevel,
    int CurrentPlayers,
    int MaxPlayers,
    string Status,
    int MatchScore,
    string HostDisplayName,
    double? DistanceKm = null
);
