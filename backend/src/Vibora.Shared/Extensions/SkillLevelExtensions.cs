namespace Vibora.Shared.Extensions;

/// <summary>
/// Extension methods for converting skill level integers to display strings
/// </summary>
public static class SkillLevelExtensions
{
    /// <summary>
    /// Converts a skill level (1-10) to a human-readable display string
    /// </summary>
    /// <param name="skillLevel">The skill level (1-10)</param>
    /// <returns>Display string: "Beginner", "Intermediate", "Advanced", or "Unknown"</returns>
    public static string ToDisplayString(this int skillLevel) => skillLevel switch
    {
        <= 3 => "Beginner",
        <= 6 => "Intermediate",
        <= 10 => "Advanced",
        _ => "Unknown"
    };
}
