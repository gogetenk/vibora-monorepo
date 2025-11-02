namespace Vibora.Games.Contracts.Events;

/// <summary>
/// Event published when a game share link is created
/// Can be consumed by Analytics module to track sharing behavior
/// </summary>
public record GameSharedEvent
{
    public Guid GameId { get; init; }
    public Guid GameShareId { get; init; }
    public string SharedByUserExternalId { get; init; } = string.Empty;
    public string ShareToken { get; init; } = string.Empty;
    public DateTime SharedAt { get; init; }
}
