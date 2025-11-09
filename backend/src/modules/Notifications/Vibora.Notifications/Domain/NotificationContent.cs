namespace Vibora.Notifications.Domain;

/// <summary>
/// Value object representing the content of a notification
/// Immutable - all properties are set via constructor
/// </summary>
public sealed class NotificationContent
{
    public string Title { get; private init; }
    public string Body { get; private init; }
    public Dictionary<string, string>? Data { get; private init; }

    // EF Core constructor
    private NotificationContent()
    {
        Title = string.Empty;
        Body = string.Empty;
    }

    public NotificationContent(string title, string body, Dictionary<string, string>? data = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty", nameof(title));

        if (string.IsNullOrWhiteSpace(body))
            throw new ArgumentException("Body cannot be empty", nameof(body));

        Title = title;
        Body = body;
        Data = data;
    }
}
