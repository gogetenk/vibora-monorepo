namespace Vibora.Users.Application.DTOs;

/// <summary>
/// Public profile DTO for displaying another user's information
/// Privacy rules: LastName is hidden - only first letter + "." is shown
/// Example: "Martin" becomes "M."
/// </summary>
public record UserPublicProfileDto(
    string FirstName,
    string? LastNameInitial, // "M." instead of "Martin"
    string SkillLevel,
    string? Bio,
    string? PhotoUrl,
    int GamesPlayedCount,
    DateTime MemberSince
);
