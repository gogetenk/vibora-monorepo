using Vibora.Shared.Domain;

namespace Vibora.Games.Domain.Events;

/// <summary>
/// Domain event raised when a participant leaves a game
/// This is the internal domain event (not the integration event published via MassTransit)
/// </summary>
internal sealed class ParticipationRemovedDomainEvent : IDomainEvent
{
    public Guid GameId { get; }
    public string UserExternalId { get; }
    public string UserName { get; }
    public int RemainingPlayers { get; }
    public GameStatus GameStatus { get; }
    public DateTime OccurredOn { get; }

    public ParticipationRemovedDomainEvent(
        Guid gameId,
        string userExternalId,
        string userName,
        int remainingPlayers,
        GameStatus gameStatus)
    {
        GameId = gameId;
        UserExternalId = userExternalId;
        UserName = userName;
        RemainingPlayers = remainingPlayers;
        GameStatus = gameStatus;
        OccurredOn = DateTime.UtcNow;
    }
}
