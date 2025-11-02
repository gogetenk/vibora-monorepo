using Ardalis.Result;
using MediatR;

namespace Vibora.Games.Application.Commands.JoinGame;

/// <summary>
/// Command to join an existing game as a participant
/// </summary>
internal sealed record JoinGameCommand(
    Guid GameId,
    string UserExternalId,
    string UserName,
    string UserSkillLevel
) : IRequest<Result<JoinGameResult>>;

/// <summary>
/// Result of joining a game
/// </summary>
internal sealed record JoinGameResult(
    Guid GameId,
    Guid ParticipationId,
    string Message
);
