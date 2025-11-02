using Vibora.Users.Domain;

namespace Vibora.Users.Domain;

/// <summary>
/// Repository interface for User aggregate root
/// </summary>
internal interface IUserRepository
{
    /// <summary>
    /// Gets a user by their external ID (Auth0/Supabase ID)
    /// </summary>
    Task<User?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a non-guest user by their external ID
    /// </summary>
    Task<User?> GetNonGuestByExternalIdAsync(string externalId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all users (typically not needed, but available for admin operations)
    /// </summary>
    Task<List<User>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user exists by external ID
    /// </summary>
    Task<bool> ExistsAsync(string externalId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new user to the repository
    /// </summary>
    void Add(User user);

    /// <summary>
    /// Updates an existing user
    /// </summary>
    void Update(User user);

    /// <summary>
    /// Removes a user from the repository
    /// </summary>
    void Remove(User user);
    
    /// <summary>
    /// Find guest user by phone number
    /// Returns null if no guest with this phone number exists
    /// </summary>
    Task<User?> GetGuestByPhoneNumberAsync(
        string phoneNumber,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Find guest user by email
    /// Returns null if no guest with this email exists
    /// Email comparison is case-insensitive (normalized to lowercase)
    /// </summary>
    Task<User?> GetGuestByEmailAsync(
        string email,
        CancellationToken cancellationToken = default);
}
