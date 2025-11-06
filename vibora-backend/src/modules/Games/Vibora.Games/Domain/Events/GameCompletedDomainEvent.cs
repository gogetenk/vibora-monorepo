using Vibora.Shared.Domain;

namespace Vibora.Games.Domain.Events;

/// <summary>
/// Domain event raised when a game reaches max players
/// This is the internal domain event (not the integration event published via MassTransit)
/// </summary>
internal sealed class GameCompletedDomainEvent : IDomainEvent
{
    public Guid GameId { get; }
    public string HostExternalId { get; }
    public DateTime GameDateTime { get; }
    public string Location { get; }
    public int MaxPlayers { get; }
    public DateTime OccurredOn { get; }

    public GameCompletedDomainEvent(
        Guid gameId,
        string hostExternalId,
        DateTime gameDateTime,
        string location,
        int maxPlayers)
    {
        GameId = gameId;
        HostExternalId = hostExternalId;
        GameDateTime = gameDateTime;
        Location = location;
        MaxPlayers = maxPlayers;
        OccurredOn = DateTime.UtcNow;
    }
}
