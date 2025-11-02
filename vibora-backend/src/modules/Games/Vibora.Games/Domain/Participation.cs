namespace Vibora.Games.Domain;

/// <summary>
/// Participation entity - Represents a player's participation in a game
/// Stores user metadata (Name, SkillLevel) to avoid repeated queries to Users module
/// </summary>
public sealed class Participation
{
    public Guid Id { get; private set; }
    public Guid GameId { get; private set; }
    public string UserExternalId { get; private set; } = string.Empty; // User ExternalId from Auth0/Supabase
    
    // Cached user metadata (from Users module at join time)
    public string UserName { get; private set; } = string.Empty;
    public string UserSkillLevel { get; private set; } = string.Empty;
    
    public bool IsHost { get; private set; }
    public DateTime JoinedAt { get; private set; }

    // EF Core constructor
    private Participation() { }

    public static Participation Create(
        Guid gameId,
        string userExternalId,
        string userName,
        string userSkillLevel,
        bool isHost)
    {
        return new Participation
        {
            Id = Guid.NewGuid(),
            GameId = gameId,
            UserExternalId = userExternalId,
            UserName = userName,
            UserSkillLevel = userSkillLevel,
            IsHost = isHost,
            JoinedAt = DateTime.UtcNow
        };
    }
}
