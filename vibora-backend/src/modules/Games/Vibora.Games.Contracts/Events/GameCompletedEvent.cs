namespace Vibora.Games.Contracts.Events;

/// <summary>
/// Event published when a game reaches max players (game completed)
/// Can be consumed by other modules to notify the host
/// </summary>
public record GameCompletedEvent
{
    public Guid GameId { get; init; }
    public string HostExternalId { get; init; } = string.Empty;
    public DateTime GameDateTime { get; init; }
    public string Location { get; init; } = string.Empty;
    public int MaxPlayers { get; init; }
    public DateTime CompletedAt { get; init; }
}
