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
    private NotificationContent GenerateContent(NotificationType type, Dictionary<string, string> context)
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

            NotificationType.GameCompleted => (
                "🎉 Partie complète !",
                $"Votre partie est complète ! Rendez-vous le {context.GetValueOrDefault("date", "bientôt")} à {context.GetValueOrDefault("location", "votre club")}"
            ),

            _ => throw new ArgumentException($"Unknown notification type: {type}", nameof(type))
        };

        return new NotificationContent(title, body, context);
    }

    /// <summary>
    /// Create base context with common game information (DRY helper)
    /// </summary>
    private static Dictionary<string, string> CreateBaseContext(string location, DateTime gameDateTime)
    {
        return new Dictionary<string, string>
        {
            ["location"] = location,
            ["date"] = gameDateTime.ToString("dd MMM yyyy HH:mm")
        };
    }

    /// <summary>
    /// Build content for game completed notification
    /// </summary>
    public NotificationContent BuildGameCompletedContent(
        string location,
        DateTime gameDateTime,
        int maxPlayers)
    {
        var context = CreateBaseContext(location, gameDateTime);
        context["maxPlayers"] = maxPlayers.ToString();

        return GenerateContent(NotificationType.GameCompleted, context);
    }

    /// <summary>
    /// Build content for game starting soon notification
    /// </summary>
    public NotificationContent BuildGameStartingSoonContent(
        string location,
        DateTime gameDateTime,
        List<string> participantNames)
    {
        var context = CreateBaseContext(location, gameDateTime);
        context["timeUntil"] = "2 heures";
        context["participants"] = string.Join(", ", participantNames);

        return GenerateContent(NotificationType.GameStartingSoon, context);
    }

    /// <summary>
    /// Build content for game canceled notification
    /// </summary>
    public NotificationContent BuildGameCanceledContent(string location, DateTime gameDateTime)
    {
        var context = CreateBaseContext(location, gameDateTime);
        return GenerateContent(NotificationType.GameCancelled, context);
    }

    /// <summary>
    /// Build content for player joined notification
    /// </summary>
    public NotificationContent BuildPlayerJoinedContent(string playerName, string location, DateTime gameDateTime)
    {
        var context = CreateBaseContext(location, gameDateTime);
        context["playerName"] = playerName;

        return GenerateContent(NotificationType.PlayerJoined, context);
    }

    /// <summary>
    /// Build content for guest joined notification
    /// </summary>
    public NotificationContent BuildGuestJoinedContent(string guestName, string location, DateTime gameDateTime)
    {
        var context = CreateBaseContext(location, gameDateTime);
        context["playerName"] = guestName;

        return GenerateContent(NotificationType.PlayerJoined, context);
    }

    /// <summary>
    /// Build content for player left notification
    /// </summary>
    public NotificationContent BuildPlayerLeftContent(string playerName, string location, DateTime gameDateTime)
    {
        var context = CreateBaseContext(location, gameDateTime);
        context["playerName"] = playerName;

        return GenerateContent(NotificationType.PlayerLeft, context);
    }
}
