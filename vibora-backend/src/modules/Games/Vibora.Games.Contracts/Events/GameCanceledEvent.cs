namespace Vibora.Games.Contracts.Events;

/// <summary>
/// Event published when a game is canceled by the host
/// Can be consumed by Communication module to notify participants
/// Contains full participant information to avoid cross-module queries
/// </summary>
public record GameCanceledEvent
{
    public Guid GameId { get; init; }
    public string HostExternalId { get; init; } = string.Empty;
    public DateTime GameDateTime { get; init; }
    public string Location { get; init; } = string.Empty;
    public int TotalParticipants { get; init; }
    public DateTime CanceledAt { get; init; }
    
    /// <summary>
    /// List of registered participants (excluding host)
    /// </summary>
    public List<ParticipantInfo> Participants { get; init; } = new();
    
    /// <summary>
    /// List of guest participants
    /// </summary>
    public List<GuestParticipantInfo> GuestParticipants { get; init; } = new();
}

/// <summary>
/// Information about a registered participant
/// </summary>
public record ParticipantInfo
{
    public string UserExternalId { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
    public string UserSkillLevel { get; init; } = string.Empty;
    public DateTime JoinedAt { get; init; }
}

/// <summary>
/// Information about a guest participant
/// </summary>
public record GuestParticipantInfo
{
    public Guid GuestId { get; init; }
    public string GuestName { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public string? Email { get; init; }
    public DateTime JoinedAt { get; init; }
}
