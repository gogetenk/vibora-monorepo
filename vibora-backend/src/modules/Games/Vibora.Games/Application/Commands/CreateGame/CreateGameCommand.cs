using Ardalis.Result;
using MediatR;

namespace Vibora.Games.Application.Commands.CreateGame;

internal sealed record CreateGameCommand(
    string HostExternalId, // User ExternalId from JWT
    DateTime DateTime,
    string Location,
    string SkillLevel,
    int MaxPlayers,
    double? Latitude = null,
    double? Longitude = null
) : IRequest<Result<CreateGameResult>>;

internal sealed record CreateGameResult(
    Guid Id,
    DateTime DateTime,
    string Location,
    string SkillLevel,
    int MaxPlayers,
    string HostExternalId,
    int CurrentPlayers,
    List<ParticipantDto> Participants,
    double? Latitude = null,
    double? Longitude = null
);

internal sealed record ParticipantDto(
    string ExternalId,
    string Name,
    string SkillLevel
);
