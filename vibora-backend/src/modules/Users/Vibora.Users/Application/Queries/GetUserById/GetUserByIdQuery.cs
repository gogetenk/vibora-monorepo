using Ardalis.Result;
using MediatR;

namespace Vibora.Users.Application.Queries.GetUserById;

internal sealed record GetUserByIdQuery(
    string ExternalId
) : IRequest<Result<GetUserByIdResult>>;

internal sealed record GetUserByIdResult(
    string ExternalId,
    string Name,
    string SkillLevel,
    string? Bio
);
