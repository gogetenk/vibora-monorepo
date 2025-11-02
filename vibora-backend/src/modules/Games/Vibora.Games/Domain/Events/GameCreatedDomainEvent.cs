using Vibora.Shared.Domain;

namespace Vibora.Games.Domain.Events;

/// <summary>
/// Domain event raised when a new game is created
/// This is the internal domain event (not the integration event published via MassTransit)
/// </summary>
internal sealed class GameCreatedDomainEvent : IDomainEvent
{
    public Guid GameId { get; }
    public string HostExternalId { get; }
    public DateTime GameDateTime { get; }
    public string Location { get; }
    public string SkillLevel { get; }
    public int MaxPlayers { get; }
    public DateTime OccurredOn { get; }

    public GameCreatedDomainEvent(
        Guid gameId,
        string hostExternalId,
        DateTime gameDateTime,
        string location,
        string skillLevel,
        int maxPlayers)
    {
        GameId = gameId;
        HostExternalId = hostExternalId;
        GameDateTime = gameDateTime;
        Location = location;
        SkillLevel = skillLevel;
        MaxPlayers = maxPlayers;
        OccurredOn = DateTime.UtcNow;
    }
}
