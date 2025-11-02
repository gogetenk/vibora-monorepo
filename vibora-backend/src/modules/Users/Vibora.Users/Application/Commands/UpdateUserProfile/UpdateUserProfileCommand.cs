using Ardalis.Result;
using MediatR;

namespace Vibora.Users.Application.Commands.UpdateUserProfile;

internal sealed record UpdateUserProfileCommand(
    string ExternalId, // From JWT
    string FirstName,
    string? LastName,
    int SkillLevel, // 1-10 scale
    string? Bio
) : IRequest<Result<UpdateUserProfileResult>>;

internal sealed record UpdateUserProfileResult(
    string ExternalId,
    string FirstName,
    string? LastName,
    int SkillLevel,
    string? Bio
);
