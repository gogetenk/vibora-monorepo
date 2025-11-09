using Ardalis.Result;
using MediatR;

namespace Vibora.Games.Application.Queries.GetAvailableGames;

/// <summary>
/// Query to retrieve available games (Open or Full status, future dates)
/// Supports optional filtering by location, skill level, and date range
/// </summary>
internal sealed record GetAvailableGamesQuery(
    string? Location = null,
    string? SkillLevel = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    int PageNumber = 1,
    int PageSize = 20
) : IRequest<Result<GetAvailableGamesResult>>;

/// <summary>
/// Result containing paginated list of available games
/// </summary>
internal sealed record GetAvailableGamesResult(
    List<GameListItemDto> Items,
    int TotalCount,
    int PageNumber,
    int PageSize,
    int TotalPages
);

/// <summary>
/// DTO for a game in the list view
/// </summary>
internal sealed record GameListItemDto(
    Guid Id,
    DateTime DateTime,
    string Location,
    string SkillLevel,
    int MaxPlayers,
    int CurrentPlayers,
    string HostExternalId,
    string Status,
    DateTime CreatedAt
);
