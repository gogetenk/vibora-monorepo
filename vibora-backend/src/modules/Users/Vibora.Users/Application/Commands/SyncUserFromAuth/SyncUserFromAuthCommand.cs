using Ardalis.Result;
using MediatR;

namespace Vibora.Users.Application.Commands.SyncUserFromAuth;

/// <summary>
/// Synchronize user metadata from external auth provider (Auth0/Supabase)
/// Called via webhook when user is created/updated in external system
/// Phase 3B: Includes optional phone/email for automatic guest reconciliation
/// </summary>
internal sealed record SyncUserFromAuthCommand(
    string ExternalId,
    string Name,
    int SkillLevel,
    string? FirstName = null,   // NEW: First name only
    string? LastName = null,    // NEW: Last name
    string? PhoneNumber = null,
    string? Email = null
) : IRequest<Result<SyncUserFromAuthResult>>;

internal sealed record SyncUserFromAuthResult(
    string ExternalId,
    string Name,
    int SkillLevel,
    bool IsNewUser
);
