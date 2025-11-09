using Ardalis.Result;

namespace Vibora.Games.Domain;

/// <summary>
/// Represents a guest participant who joins a game without creating an account
/// </summary>
public sealed class GuestParticipant
{
    public Guid Id { get; private set; }
    public Guid GameId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? PhoneNumber { get; private set; }
    public string? Email { get; private set; }
    public string? GuestExternalId { get; private set; } // Link to GuestUser (guest:{guid})
    public DateTime JoinedAt { get; private set; }

    // Navigation property
    public Game Game { get; private set; } = null!;

    // EF Core constructor
    private GuestParticipant() { }

    public static Result<GuestParticipant> Create(
        Guid gameId,
        string name,
        string? phoneNumber,
        string? email,
        string? guestExternalId = null)
    {
        var errors = new List<ValidationError>();

        if (gameId == Guid.Empty)
        {
            errors.Add(new ValidationError("GameId cannot be empty"));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            errors.Add(new ValidationError("Guest name is required"));
        }
        else if (name.Length > 100)
        {
            errors.Add(new ValidationError("Guest name must not exceed 100 characters"));
        }

        // At least one contact method is required
        if (string.IsNullOrWhiteSpace(phoneNumber) && string.IsNullOrWhiteSpace(email))
        {
            errors.Add(new ValidationError("Either phone number or email is required"));
        }

        // Validate phone number format if provided
        if (!string.IsNullOrWhiteSpace(phoneNumber) && phoneNumber.Length > 20)
        {
            errors.Add(new ValidationError("Phone number must not exceed 20 characters"));
        }

        // Validate email format if provided
        if (!string.IsNullOrWhiteSpace(email))
        {
            if (email.Length > 255)
            {
                errors.Add(new ValidationError("Email must not exceed 255 characters"));
            }
            else if (!email.Contains('@'))
            {
                errors.Add(new ValidationError("Email must be a valid email address"));
            }
        }

        if (errors.Any())
        {
            return Result<GuestParticipant>.Invalid(errors);
        }

        var guestParticipant = new GuestParticipant
        {
            Id = Guid.NewGuid(),
            GameId = gameId,
            Name = name.Trim(),
            PhoneNumber = phoneNumber?.Trim(),
            Email = email?.Trim()?.ToLowerInvariant(),
            GuestExternalId = guestExternalId,
            JoinedAt = DateTime.UtcNow
        };

        return Result.Success(guestParticipant);
    }

    /// <summary>
    /// Check if this guest matches the provided contact information
    /// </summary>
    public bool MatchesContact(string? phoneNumber, string? email)
    {
        if (!string.IsNullOrWhiteSpace(phoneNumber) && 
            !string.IsNullOrWhiteSpace(PhoneNumber) &&
            PhoneNumber.Equals(phoneNumber, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(email) && 
            !string.IsNullOrWhiteSpace(Email) &&
            Email.Equals(email, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Get the primary contact identifier for this guest
    /// </summary>
    public string GetContactIdentifier()
    {
        return PhoneNumber ?? Email ?? "Unknown";
    }
}
