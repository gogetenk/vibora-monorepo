using Vibora.Users.Domain;

namespace Vibora.Integration.Tests.Infrastructure.TestDataBuilders;

/// <summary>
/// Fluent builder for creating User test data
/// </summary>
public class UserBuilder
{
    private string _externalId = $"auth0|test-{Guid.NewGuid()}";
    private string _name = "Test User";
    private int _skillLevel = 5; // Default: Intermediate
    private string? _email;
    private string? _bio;

    /// <summary>
    /// Set external ID
    /// </summary>
    public UserBuilder WithExternalId(string externalId)
    {
        _externalId = externalId;
        return this;
    }

    /// <summary>
    /// Set name
    /// </summary>
    public UserBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    /// <summary>
    /// Set skill level (1-10)
    /// </summary>
    public UserBuilder WithSkillLevel(int skillLevel)
    {
        _skillLevel = skillLevel;
        return this;
    }

    /// <summary>
    /// Set email
    /// </summary>
    public UserBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }

    /// <summary>
    /// Set bio
    /// </summary>
    public UserBuilder WithBio(string bio)
    {
        _bio = bio;
        return this;
    }

    /// <summary>
    /// Create a beginner user (level 2)
    /// </summary>
    public UserBuilder Beginner()
    {
        _skillLevel = 2;
        return this;
    }

    /// <summary>
    /// Create an intermediate user (level 5)
    /// </summary>
    public UserBuilder Intermediate()
    {
        _skillLevel = 5;
        return this;
    }

    /// <summary>
    /// Create an advanced user (level 9)
    /// </summary>
    public UserBuilder Advanced()
    {
        _skillLevel = 9;
        return this;
    }

    /// <summary>
    /// Build the User entity
    /// </summary>
    public User Build()
    {
        // Parse Name into FirstName/LastName
        var nameParts = _name.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var firstName = nameParts.Length > 0 ? nameParts[0] : _name;
        var lastName = nameParts.Length > 1 ? nameParts[1] : null;

        // Create user with firstName only (not full name)
        var user = User.CreateFromExternalAuth(_externalId, firstName, _skillLevel, lastName, _email);

        // If we have bio, update the profile to set it
        if (_bio != null)
        {
            user.UpdateProfile(firstName, lastName, _skillLevel, _bio);
        }

        return user;
    }
}
