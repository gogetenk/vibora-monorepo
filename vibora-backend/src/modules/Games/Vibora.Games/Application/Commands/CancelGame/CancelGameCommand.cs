using Ardalis.Result;
using MediatR;

namespace Vibora.Games.Application.Commands.CancelGame;

/// <summary>
/// Command to cancel a game (only host can cancel)
/// </summary>
internal sealed record CancelGameCommand(
    Guid GameId,
    string HostExternalId
) : IRequest<Result<CancelGameResult>>;

/// <summary>
/// Result of canceling a game
/// </summary>
internal sealed record CancelGameResult(
    Guid GameId,
    string Message
);
