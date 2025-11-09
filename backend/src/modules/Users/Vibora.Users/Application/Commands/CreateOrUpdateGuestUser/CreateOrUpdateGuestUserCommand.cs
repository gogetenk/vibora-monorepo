using Ardalis.Result;
using MediatR;

namespace Vibora.Users.Application.Commands.CreateOrUpdateGuestUser;

/// <summary>
/// Command to create a new guest user or update an existing guest with matching contact info
/// This command centralizes the orchestration logic that was previously in UsersServiceInProcessClient
///
/// Architecture Decision: This keeps the Application Layer responsible for business orchestration,
/// while service clients remain dumb proxies (just ISender + DTO mapping)
/// </summary>
internal sealed record CreateOrUpdateGuestUserCommand(
    string Name,
    string? PhoneNumber,
    string? Email,
    int SkillLevel = 5 // Default: 5 (Intermediate on 1-10 scale)
) : IRequest<Result<CreateOrUpdateGuestUserResult>>;

/// <summary>
/// Result of create/update guest user operation
/// Contains the ExternalId needed for cross-module reference
/// </summary>
internal sealed record CreateOrUpdateGuestUserResult(
    string ExternalId // "guest:{guid}" format
);
