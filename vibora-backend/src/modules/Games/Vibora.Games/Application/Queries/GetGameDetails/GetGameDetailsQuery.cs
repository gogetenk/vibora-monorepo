using Ardalis.Result;
using MediatR;

namespace Vibora.Games.Application.Queries.GetGameDetails;

/// <summary>
/// Query to retrieve detailed information about a specific game including participants
/// </summary>
internal sealed record GetGameDetailsQuery(Guid GameId) : IRequest<Result<GameDetailsResult>>;

/// <summary>
/// Detailed game information with all participants
/// </summary>
internal sealed record GameDetailsResult(
    Guid Id,
    DateTime DateTime,
    string Location,
    string SkillLevel,
    int MaxPlayers,
    int CurrentPlayers,
    string HostExternalId,
    string Status,
    DateTime CreatedAt,
    List<ParticipantInfoDto> Participants
);

/// <summary>
/// Unified participant information (registered user or guest)
/// </summary>
internal sealed record ParticipantInfoDto(
    string Type,              // "User" | "Guest"
    Guid? ParticipationId,    // Nullable (null for guests)
    string Identifier,        // UserExternalId for User, "Guest: {Name}" for Guest
    string DisplayName,       // UserName for User, Name for Guest
    string? SkillLevel,       // UserSkillLevel for User, null for Guest
    string? ContactInfo,      // null for User, PhoneNumber/Email for Guest
    bool IsHost,
    DateTime JoinedAt
);
