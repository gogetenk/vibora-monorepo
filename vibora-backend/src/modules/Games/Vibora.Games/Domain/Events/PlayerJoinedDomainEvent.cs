using Vibora.Shared.Domain;

namespace Vibora.Games.Domain.Events;

/// <summary>
/// Domain event raised when a player joins a game
/// This is the internal domain event (not the integration event published via MassTransit)
/// </summary>
internal sealed class PlayerJoinedDomainEvent : IDomainEvent
{
    public Guid GameId { get; }
    public Guid ParticipationId { get; }
    public string UserExternalId { get; }
    public string UserName { get; }
    public string UserSkillLevel { get; }
    public string HostExternalId { get; }
    public DateTime GameDateTime { get; }
    public string Location { get; }
    public int CurrentPlayers { get; }
    public int MaxPlayers { get; }
    public DateTime OccurredOn { get; }

    public PlayerJoinedDomainEvent(
        Guid gameId,
        Guid participationId,
        string userExternalId,
        string userName,
        string userSkillLevel,
        string hostExternalId,
        DateTime gameDateTime,
        string location,
        int currentPlayers,
        int maxPlayers)
    {
        GameId = gameId;
        ParticipationId = participationId;
        UserExternalId = userExternalId;
        UserName = userName;
        UserSkillLevel = userSkillLevel;
        HostExternalId = hostExternalId;
        GameDateTime = gameDateTime;
        Location = location;
        CurrentPlayers = currentPlayers;
        MaxPlayers = maxPlayers;
        OccurredOn = DateTime.UtcNow;
    }
}
