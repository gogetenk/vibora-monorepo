using Ardalis.Result;
using MediatR;

namespace Vibora.Users.Application.Queries.GetCurrentUser;

internal sealed record GetCurrentUserQuery(
    string ExternalId // From JWT sub claim (Auth0/Supabase user ID)
) : IRequest<Result<GetCurrentUserResult>>;

internal sealed record GetCurrentUserResult(
    string ExternalId, // Used as ID everywhere in the system
    string Name,
    string SkillLevel,
    string? Bio
);
