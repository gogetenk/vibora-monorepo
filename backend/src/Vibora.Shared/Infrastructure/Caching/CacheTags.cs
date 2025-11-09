namespace Vibora.Shared.Infrastructure.Caching;

/// <summary>
/// Centralized cache tag constants for consistent invalidation across modules.
/// Used with ASP.NET Core Output Cache tag-based invalidation (IOutputCacheStore.EvictByTagAsync).
/// </summary>
public static class CacheTags
{
    // Games module tags
    public const string GamesAvailable = "games-available";
    public const string GamesSearch = "games-search";

    /// <summary>
    /// Get tag for a specific game by ID.
    /// Use this to invalidate cache for a single game's details.
    /// </summary>
    public static string GameById(Guid gameId) => $"game-{gameId}";

    /// <summary>
    /// Get tag for a specific user's games list.
    /// Use this to invalidate "my games" cache when user joins/leaves.
    /// </summary>
    public static string GamesByUser(string userExternalId) => $"games-user-{userExternalId}";

    /// <summary>
    /// Get tag for user's game count (used in cross-module calls).
    /// </summary>
    public static string GamesCountByUser(string userExternalId) => $"games-count-{userExternalId}";

    // Share tags
    public const string Shares = "shares";

    /// <summary>
    /// Get tag for a specific share by token.
    /// </summary>
    public static string ShareByToken(string token) => $"share-{token}";

    // Users module tags
    public const string Users = "users";

    /// <summary>
    /// Get tag for a specific user's profile (private, authenticated).
    /// </summary>
    public static string UserProfile(string userExternalId) => $"user-profile-{userExternalId}";

    /// <summary>
    /// Get tag for a specific user's public profile.
    /// </summary>
    public static string UserPublicProfile(string userExternalId) => $"user-public-{userExternalId}";
}
