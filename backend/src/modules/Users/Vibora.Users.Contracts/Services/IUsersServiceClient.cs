using Ardalis.Result;

namespace Vibora.Users.Contracts.Services;

/// <summary>
/// Client to query Users module for user information
/// PUBLIC interface for cross-module communication (HTTP or In-Process)
/// Shared by all modules that need to query Users data
/// </summary>
public interface IUsersServiceClient
{
    /// <summary>
    /// Gets user metadata by external ID
    /// Returns Result with NotFound status if user doesn't exist
    /// </summary>
    Task<Result<UserMetadataDto>> GetUserMetadataAsync(
        string externalId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new guest user or update existing guest with matching contact
    /// Returns the guest user's ExternalId for reference
    /// Used for Phase 3B automatic guest reconciliation
    /// </summary>
    /// <param name="name">Guest display name</param>
    /// <param name="phoneNumber">Phone number (E.164 format recommended)</param>
    /// <param name="email">Email address</param>
    /// <param name="skillLevel">Skill level on 1-10 scale (defaults to 5 = Intermediate)</param>
    /// <returns>Guest user's ExternalId (format: "guest:guid")</returns>
    /// <remarks>
    /// Idempotency: If guest with same phone/email exists, returns their ExternalId without duplicating.
    /// Phone is checked first, then email if phone not found.
    /// </remarks>
    Task<string> CreateOrUpdateGuestUserAsync(
        string name,
        string? phoneNumber,
        string? email,
        int skillLevel = 5, // Default: 5 (Intermediate on 1-10 scale)
        CancellationToken cancellationToken = default);
}

/// <summary>
/// DTO for user metadata
/// Used for cross-module communication
/// </summary>
public record UserMetadataDto(
    string ExternalId,
    string Name,
    int SkillLevel // 1-10 scale
);
