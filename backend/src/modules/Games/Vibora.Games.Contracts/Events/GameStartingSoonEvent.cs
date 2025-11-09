namespace Vibora.Games.Contracts.Events;

/// <summary>
/// Event published when a game is starting soon (reminder - 2 hours before)
/// Can be consumed by other modules to notify all participants
/// Uses participant info types from GameCanceledEvent for consistency
/// </summary>
public record GameStartingSoonEvent
{
    public Guid GameId { get; init; }
    public DateTime GameDateTime { get; init; }
    public string Location { get; init; } = string.Empty;
    public string HostExternalId { get; init; } = string.Empty;
    public int CurrentPlayers { get; init; }
    public int MaxPlayers { get; init; }
    public int TimeUntilStartMinutes { get; init; }
    public List<ParticipantInfo> Participants { get; init; } = new();
    public List<GuestParticipantInfo> GuestParticipants { get; init; } = new();
    public DateTime PublishedAt { get; init; }
}
