using Ardalis.Result;
using Vibora.Shared.Domain;
using Vibora.Users.Domain.Events;

namespace Vibora.Users.Domain;

/// <summary>
/// User aggregate root - Stores metadata for users authenticated via Auth0/Supabase
/// ExternalId (from Auth0/Supabase) is used as the primary key throughout the system
/// </summary>
public sealed class User : AggregateRoot
{
    /// <summary>
    /// External ID from Auth0/Supabase - Used as PRIMARY KEY everywhere
    /// Format: "auth0|123456" or Supabase UUID
    /// </summary>
    public string ExternalId { get; private set; } = string.Empty;

    // Legacy property for backward compatibility - will be replaced by FirstName/LastName
    public string Name { get; private set; } = string.Empty;

    // New profile properties
    public string FirstName { get; private set; } = string.Empty;
    public string? LastName { get; private set; }
    public int SkillLevel { get; private set; } = 5; // Default: Intermediate (1-10 scale)
    public string? Bio { get; private set; }
    public string? PhotoUrl { get; private set; }

    public bool IsGuest { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? LastSyncedAt { get; private set; }

    /// <summary>
    /// Phone number for guest matching (E.164 format recommended)
    /// Only populated for guest users initially
    /// Can be populated for regular users from auth provider
    /// </summary>
    public string? PhoneNumber { get; private set; }

    /// <summary>
    /// Email address for guest matching (normalized lowercase)
    /// Only populated for guest users initially
    /// Can be populated for regular users from auth provider
    /// </summary>
    public string? Email { get; private set; }

    // EF Core constructor
    private User() { }

    /// <summary>
    /// Create user metadata from external auth provider (Auth0/Supabase)
    /// </summary>
    public static User CreateFromExternalAuth(
        string externalId,
        string firstName,
        int skillLevel = 5, // Default: 5 (Intermediate on 1-10 scale)
        string? lastName = null,
        string? email = null)
    {
        // Build full name for legacy compatibility
        var fullName = !string.IsNullOrWhiteSpace(lastName) 
            ? $"{firstName} {lastName}"
            : firstName;

        return new User
        {
            ExternalId = externalId, // PK from Auth0/Supabase
            Name = fullName,         // Legacy field: full name
            FirstName = firstName,   // First name only
            LastName = lastName,     // Last name (nullable)
            Email = email,           // Email from auth provider
            SkillLevel = skillLevel,
            IsGuest = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            LastSyncedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Create guest user (temporary, no external auth)
    /// ExternalId = "guest:{guid}" for uniqueness
    /// </summary>
    public static User CreateGuestUser(
        string name,
        int skillLevel,
        string? phoneNumber = null,
        string? email = null)
    {
        return new User
        {
            ExternalId = $"guest:{Guid.NewGuid()}", // Unique guest ID
            Name = name,
            FirstName = name,
            SkillLevel = skillLevel,
            IsGuest = true,
            PhoneNumber = phoneNumber?.Trim(),
            Email = email?.Trim()?.ToLowerInvariant(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            LastSyncedAt = null
        };
    }

    /// <summary>
    /// Validate profile update parameters
    /// Collects all validation errors before returning
    /// </summary>
    private static Result ValidateProfileUpdate(
        string firstName,
        string? lastName,
        string? bio)
    {
        var errors = new List<ValidationError>();

        // FirstName validation
        if (string.IsNullOrWhiteSpace(firstName))
        {
            errors.Add(new ValidationError("FirstName is required"));
        }
        else if (firstName.Length < 2)
        {
            errors.Add(new ValidationError("FirstName must be at least 2 characters"));
        }
        else if (firstName.Length > 50)
        {
            errors.Add(new ValidationError("FirstName must not exceed 50 characters"));
        }

        // LastName validation (optional)
        if (!string.IsNullOrWhiteSpace(lastName) && lastName.Length > 50)
        {
            errors.Add(new ValidationError("LastName must not exceed 50 characters"));
        }

        // Bio validation (optional)
        if (!string.IsNullOrWhiteSpace(bio) && bio.Length > 150)
        {
            errors.Add(new ValidationError("Bio must not exceed 150 characters"));
        }

        return errors.Any()
            ? Result.Invalid(errors)
            : Result.Success();
    }

    /// <summary>
    /// Update user profile with validation
    /// Returns Result to follow Railway-Oriented Programming
    /// </summary>
    public Result UpdateProfile(
        string firstName,
        string? lastName,
        int skillLevel, // 1-10 scale
        string? bio)
    {
        // Validate all inputs first (collect ALL errors)
        var validationResult = ValidateProfileUpdate(firstName, lastName, bio);
        if (!validationResult.IsSuccess)
        {
            return validationResult;
        }

        // Update fields
        FirstName = firstName;
        LastName = lastName;
        SkillLevel = skillLevel;
        Bio = bio;
        UpdatedAt = DateTime.UtcNow;

        // Update legacy Name field for backward compatibility
        Name = string.IsNullOrWhiteSpace(lastName)
            ? firstName
            : $"{firstName} {lastName}";

        // Raise domain event (will be published by Unit of Work after transaction)
        AddDomainEvent(new UserProfileUpdatedDomainEvent(Guid.Empty, ExternalId));

        return Result.Success();
    }

    /// <summary>
    /// Update user profile photo URL
    /// </summary>
    public void UpdateProfilePhoto(string? photoUrl)
    {
        PhotoUrl = photoUrl;
        UpdatedAt = DateTime.UtcNow;

        // Raise domain event for photo update
        AddDomainEvent(new UserProfileUpdatedDomainEvent(Guid.Empty, ExternalId));
    }

    /// <summary>
    /// Sync user data from external auth provider (Auth0/Supabase)
    /// </summary>
    public void SyncFromExternalProvider(string name)
    {
        Name = name;
        FirstName = name;
        LastSyncedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Set contact information for guest users
    /// Used to enable automatic reconciliation when they sign up
    /// </summary>
    /// <param name="phoneNumber">Phone number (E.164 format recommended)</param>
    /// <param name="email">Email address (will be normalized to lowercase)</param>
    /// <exception cref="InvalidOperationException">If called on non-guest user</exception>
    public void SetContactInfo(string? phoneNumber, string? email)
    {
        if (!IsGuest)
        {
            throw new InvalidOperationException(
                "SetContactInfo can only be called on guest users");
        }

        // Validation - at least one contact method required
        if (string.IsNullOrWhiteSpace(phoneNumber) && string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException(
                "At least one contact method (phone or email) must be provided");
        }

        // Normalize and set
        PhoneNumber = phoneNumber?.Trim();
        Email = email?.Trim()?.ToLowerInvariant();
        UpdatedAt = DateTime.UtcNow;
    }
}
