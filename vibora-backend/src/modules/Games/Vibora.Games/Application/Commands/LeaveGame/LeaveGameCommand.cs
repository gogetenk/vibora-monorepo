using Ardalis.Result;
using MediatR;

namespace Vibora.Games.Application.Commands.LeaveGame;

/// <summary>
/// Command to leave a game (remove participation)
/// </summary>
internal sealed record LeaveGameCommand(
    Guid GameId,
    string UserExternalId
) : IRequest<Result<LeaveGameResult>>;

/// <summary>
/// Result of leaving a game
/// </summary>
internal sealed record LeaveGameResult(
    Guid GameId,
    string Message
);
