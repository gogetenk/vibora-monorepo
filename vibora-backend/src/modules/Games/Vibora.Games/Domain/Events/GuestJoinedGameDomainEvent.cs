using Vibora.Shared.Domain;

namespace Vibora.Games.Domain.Events;

/// <summary>
/// Domain event raised when a guest participant joins a game
/// </summary>
public sealed record GuestJoinedGameDomainEvent : IDomainEvent
{
    public Guid GameId { get; }
    public Guid GuestParticipantId { get; }
    public string GuestName { get; }
    public string ContactIdentifier { get; }
    public int CurrentPlayers { get; }
    public GameStatus GameStatus { get; }
    public DateTime OccurredOn { get; }

    public GuestJoinedGameDomainEvent(
        Guid gameId,
        Guid guestParticipantId,
        string guestName,
        string contactIdentifier,
        int currentPlayers,
        GameStatus gameStatus)
    {
        GameId = gameId;
        GuestParticipantId = guestParticipantId;
        GuestName = guestName;
        ContactIdentifier = contactIdentifier;
        CurrentPlayers = currentPlayers;
        GameStatus = gameStatus;
        OccurredOn = DateTime.UtcNow;
    }
}
