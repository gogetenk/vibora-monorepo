using Vibora.Notifications.Domain;

namespace Vibora.Notifications.Infrastructure.Services;

/// <summary>
/// Service responsible for generating notification content based on type and context
/// Uses templates to create consistent, localized notifications
/// </summary>
public sealed class NotificationTemplateService
{
    /// <summary>
    /// Generate notification content for a specific notification type
    /// </summary>
    /// <param name="type">Type of notification</param>
    /// <param name="context">Context data for template (game name, player name, etc.)</param>
    /// <returns>NotificationContent with title and body</returns>
    public NotificationContent GenerateContent(NotificationType type, Dictionary<string, string> context)
    {
        var (title, body) = type switch
        {
            NotificationType.GameCreated => (
                "Nouvelle partie créée",
                $"{context.GetValueOrDefault("hostName", "Un joueur")} a créé une partie de padel le {context.GetValueOrDefault("date", "bientôt")} à {context.GetValueOrDefault("location", "un club")}"
            ),

            NotificationType.PlayerJoined => (
                "Nouveau joueur",
                $"{context.GetValueOrDefault("playerName", "Un joueur")} a rejoint votre partie de padel"
            ),

            NotificationType.PlayerLeft => (
                "Joueur parti",
                $"{context.GetValueOrDefault("playerName", "Un joueur")} a quitté votre partie de padel"
            ),

            NotificationType.GameCancelled => (
                "Partie annulée",
                $"La partie de padel prévue le {context.GetValueOrDefault("date", "bientôt")} a été annulée"
            ),

            NotificationType.GameStartingSoon => (
                "Partie bientôt",
                $"Votre partie de padel commence dans {context.GetValueOrDefault("timeUntil", "1 heure")} à {context.GetValueOrDefault("location", "votre club")}"
            ),

            NotificationType.NewChatMessage => (
                "Nouveau message",
                $"{context.GetValueOrDefault("senderName", "Un joueur")} : {context.GetValueOrDefault("messagePreview", "a envoyé un message")}"
            ),

            _ => throw new ArgumentException($"Unknown notification type: {type}", nameof(type))
        };

        return new NotificationContent(title, body, context);
    }

    /// <summary>
    /// Get default context for a notification type (used for testing/fallback)
    /// </summary>
    public static Dictionary<string, string> GetDefaultContext(NotificationType type)
    {
        return type switch
        {
            NotificationType.GameCreated => new()
            {
                ["hostName"] = "John Doe",
                ["date"] = "15 Oct 2025 19:00",
                ["location"] = "Club Padel Paris"
            },

            NotificationType.PlayerJoined => new()
            {
                ["playerName"] = "Jane Smith"
            },

            NotificationType.PlayerLeft => new()
            {
                ["playerName"] = "Jane Smith"
            },

            NotificationType.GameCancelled => new()
            {
                ["date"] = "15 Oct 2025 19:00"
            },

            NotificationType.GameStartingSoon => new()
            {
                ["timeUntil"] = "1 heure",
                ["location"] = "Club Padel Paris"
            },

            NotificationType.NewChatMessage => new()
            {
                ["senderName"] = "John Doe",
                ["messagePreview"] = "Salut! On se retrouve 15 min avant?"
            },

            _ => new Dictionary<string, string>()
        };
    }

    /// <summary>
    /// Build content for game canceled notification
    /// </summary>
    public NotificationContent BuildGameCanceledContent(string location, DateTime gameDateTime)
    {
        var context = new Dictionary<string, string>
        {
            ["date"] = gameDateTime.ToString("dd MMM yyyy HH:mm"),
            ["location"] = location
        };

        return GenerateContent(NotificationType.GameCancelled, context);
    }

    /// <summary>
    /// Build content for player joined notification
    /// </summary>
    public NotificationContent BuildPlayerJoinedContent(string playerName, string location, DateTime gameDateTime)
    {
        var context = new Dictionary<string, string>
        {
            ["playerName"] = playerName,
            ["date"] = gameDateTime.ToString("dd MMM yyyy HH:mm"),
            ["location"] = location
        };

        return GenerateContent(NotificationType.PlayerJoined, context);
    }

    /// <summary>
    /// Build content for guest joined notification
    /// </summary>
    public NotificationContent BuildGuestJoinedContent(string guestName, string location, DateTime gameDateTime)
    {
        var context = new Dictionary<string, string>
        {
            ["playerName"] = guestName,
            ["date"] = gameDateTime.ToString("dd MMM yyyy HH:mm"),
            ["location"] = location
        };

        return GenerateContent(NotificationType.PlayerJoined, context);
    }

    /// <summary>
    /// Build content for player left notification
    /// </summary>
    public NotificationContent BuildPlayerLeftContent(string playerName, string location, DateTime gameDateTime)
    {
        var context = new Dictionary<string, string>
        {
            ["playerName"] = playerName,
            ["date"] = gameDateTime.ToString("dd MMM yyyy HH:mm"),
            ["location"] = location
        };

        return GenerateContent(NotificationType.PlayerLeft, context);
    }
}
