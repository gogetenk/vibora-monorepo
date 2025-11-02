using Ardalis.Result;
using MediatR;
using Vibora.Users.Application.DTOs;

namespace Vibora.Users.Application.Queries.GetUserPublicProfile;

/// <summary>
/// Query to retrieve another user's public profile
/// Privacy rules: LastName is hidden (only first letter shown)
/// </summary>
internal sealed record GetUserPublicProfileQuery(
    string TargetUserExternalId
) : IRequest<Result<UserPublicProfileDto>>;
