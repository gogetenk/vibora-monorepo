namespace Vibora.Users.Contracts.Events;

/// <summary>
/// Event published when a new user (registered or guest) is created
/// </summary>
public record UserCreatedEvent
{
    public Guid UserId { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool IsGuest { get; init; }
    public DateTime CreatedAt { get; init; }
}
