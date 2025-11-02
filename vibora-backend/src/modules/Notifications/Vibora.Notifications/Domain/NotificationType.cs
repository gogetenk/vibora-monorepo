namespace Vibora.Notifications.Domain;

/// <summary>
/// Represents the type/category of notification
/// Each type has a specific template and routing logic
/// </summary>
public enum NotificationType
{
    /// <summary>
    /// A new game has been created
    /// </summary>
    GameCreated = 1,

    /// <summary>
    /// A player has joined a game
    /// </summary>
    PlayerJoined = 2,

    /// <summary>
    /// A player has left a game
    /// </summary>
    PlayerLeft = 3,

    /// <summary>
    /// A game has been cancelled
    /// </summary>
    GameCancelled = 4,

    /// <summary>
    /// A game is starting soon (reminder)
    /// </summary>
    GameStartingSoon = 5,

    /// <summary>
    /// A new message has been posted in a game chat
    /// </summary>
    NewChatMessage = 6
}
