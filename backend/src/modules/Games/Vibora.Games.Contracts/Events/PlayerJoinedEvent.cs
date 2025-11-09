namespace Vibora.Games.Contracts.Events;

/// <summary>
/// Event published when a registered user joins a game
/// Can be consumed by other modules to notify the host
/// </summary>
public record PlayerJoinedEvent
{
    public Guid GameId { get; init; }
    public Guid ParticipationId { get; init; }
    public string UserExternalId { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
    public string UserSkillLevel { get; init; } = string.Empty;
    public string HostExternalId { get; init; } = string.Empty;
    public DateTime GameDateTime { get; init; }
    public string Location { get; init; } = string.Empty;
    public int CurrentPlayers { get; init; }
    public int MaxPlayers { get; init; }
    public DateTime JoinedAt { get; init; }
}
