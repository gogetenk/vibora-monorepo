namespace Vibora.Users.Domain;

/// <summary>
/// Skill level for user profiles (1-10 scale)
/// 1-3: Beginner
/// 4-7: Intermediate  
/// 8-10: Advanced
/// </summary>
public static class SkillLevelConstants
{
    public const int Min = 1;
    public const int Max = 10;
    public const int Default = 5; // Intermediate

    public static bool IsValid(int level) => level >= Min && level <= Max;

    public static string GetLabel(int level)
    {
        return level switch
        {
            <= 3 => "Beginner",
            <= 7 => "Intermediate",
            _ => "Advanced"
        };
    }
    
    public static int Parse(string skillLevelString)
    {
        return skillLevelString?.ToLower() switch
        {
            "beginner" => 2,
            "intermediate" => 5,
            "advanced" => 9,
            _ => Default
        };
    }
}
