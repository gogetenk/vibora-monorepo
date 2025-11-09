using Ardalis.Result;
using MediatR;

namespace Vibora.Users.Application.Queries.GetCurrentUserProfile;

/// <summary>
/// Query to retrieve the authenticated user's full profile including statistics
/// </summary>
internal sealed record GetCurrentUserProfileQuery(
    string UserExternalId
) : IRequest<Result<UserProfileDto>>;

/// <summary>
/// User profile information with statistics
/// </summary>
public sealed record UserProfileDto(
    string ExternalId,
    string FirstName,
    string? LastName,
    string SkillLevel,
    string? Bio,
    string? PhotoUrl,
    int GamesPlayedCount,
    DateTime MemberSince
);
