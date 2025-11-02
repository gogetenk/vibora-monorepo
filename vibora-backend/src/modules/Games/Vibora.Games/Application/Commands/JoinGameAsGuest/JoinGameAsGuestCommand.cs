using Ardalis.Result;
using MediatR;

namespace Vibora.Games.Application.Commands.JoinGameAsGuest;

/// <summary>
/// Command to join a game as a guest participant (without creating an account)
/// </summary>
internal sealed record JoinGameAsGuestCommand(
    Guid GameId,
    string Name,
    string? PhoneNumber,
    string? Email) : IRequest<Result<JoinGameAsGuestResult>>;

/// <summary>
/// Result returned after a guest successfully joins a game
/// </summary>
internal sealed record JoinGameAsGuestResult(
    Guid GameId,
    Guid GuestParticipantId,
    string GuestName,
    int CurrentPlayers,
    string GameStatus,
    string Message);
