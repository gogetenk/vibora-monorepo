using Ardalis.Result;
using MediatR;

namespace Vibora.Games.Application.Commands.CreateGameShare;

/// <summary>
/// Command to create a shareable link for a game
/// </summary>
internal sealed record CreateGameShareCommand(
    Guid GameId,
    string SharedByUserExternalId,
    DateTime? ExpiresAt = null
) : IRequest<Result<CreateGameShareResult>>;

/// <summary>
/// Result of creating a game share
/// </summary>
internal sealed record CreateGameShareResult(
    Guid GameShareId,
    string ShareToken,
    string ShareUrl
);
