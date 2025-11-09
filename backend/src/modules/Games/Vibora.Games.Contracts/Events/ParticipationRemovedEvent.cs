namespace Vibora.Games.Contracts.Events;

/// <summary>
/// Event published when a participant leaves a game
/// Can be consumed by other modules to notify other players, update stats, etc.
/// </summary>
public record ParticipationRemovedEvent
{
    public Guid GameId { get; init; }
    public string UserExternalId { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
    public string HostExternalId { get; init; } = string.Empty;
    public DateTime GameDateTime { get; init; }
    public string Location { get; init; } = string.Empty;
    public int RemainingPlayers { get; init; }
    public string GameStatus { get; init; } = string.Empty;
    public DateTime RemovedAt { get; init; }
}
