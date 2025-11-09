using Ardalis.Result;
using MediatR;

namespace Vibora.Users.Application.Commands.CreateGuestUser;

internal sealed record CreateGuestUserCommand(
    string Name,
    int SkillLevel, // 1-10 scale
    string? PhoneNumber = null,
    string? Email = null
) : IRequest<Result<CreateGuestUserResult>>;

internal sealed record CreateGuestUserResult(
    string ExternalId, // guest:{guid}
    string Name,
    int SkillLevel,
    string Token,
    string? PhoneNumber = null,
    string? Email = null
);
