namespace Vibora.Games.Contracts.Events;

/// <summary>
/// Event published when a new game is created
/// Consumed by Communication module to create chat automatically
/// </summary>
public record GameCreatedEvent
{
    public Guid GameId { get; init; }
    public string HostExternalId { get; init; } = string.Empty;
    public DateTime DateTime { get; init; }
    public string Location { get; init; } = string.Empty;
    public string SkillLevel { get; init; } = string.Empty;
    public int MaxPlayers { get; init; }
    public DateTime CreatedAt { get; init; }
}
