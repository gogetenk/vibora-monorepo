namespace Vibora.Games.Contracts.Events;

/// <summary>
/// Event published when a guest (non-registered user) joins a game
/// Can be consumed by other modules to notify the host
/// </summary>
public record GuestJoinedEvent
{
    public Guid GameId { get; init; }
    public Guid GuestId { get; init; }
    public string GuestName { get; init; } = string.Empty;
    public string? GuestPhone { get; init; }
    public string? GuestEmail { get; init; }
    public string HostExternalId { get; init; } = string.Empty;
    public DateTime GameDateTime { get; init; }
    public string Location { get; init; } = string.Empty;
    public int CurrentPlayers { get; init; }
    public DateTime JoinedAt { get; init; }
}
