using Ardalis.Result;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Vibora.Users.Application.Queries.GetUserPublicProfile;
using Vibora.Users.Domain;
using Vibora.Users.Infrastructure.Data;
using Xunit;

namespace Vibora.Users.Tests.Application.Queries;

/// <summary>
/// Tests for GetUserPublicProfileQueryHandler
/// Verifies privacy rules: LastName is hidden (only first letter shown)
/// </summary>
public class GetUserPublicProfileQueryHandlerTests
{
    private readonly IUserRepository _userRepository;
    private readonly UsersDbContext _dbContext;
    private readonly GetUserPublicProfileQueryHandler _handler;

    public GetUserPublicProfileQueryHandlerTests()
    {
        _userRepository = Substitute.For<IUserRepository>();

        // Create in-memory database for testing SQL queries
        var options = new DbContextOptionsBuilder<UsersDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new UsersDbContext(options);

        _handler = new GetUserPublicProfileQueryHandler(_userRepository, _dbContext);
    }

    [Fact]
    public async Task Handle_WithExistingUser_ShouldReturnPublicProfileWithLastNameInitial()
    {
        // Arrange
        var externalId = "auth0|test-user";
        var user = User.CreateFromExternalAuth(externalId, "John", SkillLevel.Advanced);
        user.UpdateProfile("John", "Martin", SkillLevel.Advanced, "Padel enthusiast");

        _userRepository.GetByExternalIdAsync(externalId, Arg.Any<CancellationToken>())
            .Returns(user);

        var query = new GetUserPublicProfileQuery(externalId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.FirstName.Should().Be("John");
        result.Value.LastNameInitial.Should().Be("M."); // Privacy rule: only first letter
        result.Value.SkillLevel.Should().Be("Advanced");
        result.Value.Bio.Should().Be("Padel enthusiast");
        result.Value.PhotoUrl.Should().BeNull();
        result.Value.GamesPlayedCount.Should().BeGreaterOrEqualTo(0);
        result.Value.MemberSince.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Handle_WithUserWithoutLastName_ShouldReturnNullLastNameInitial()
    {
        // Arrange
        var externalId = "auth0|no-lastname";
        var user = User.CreateFromExternalAuth(externalId, "Jane", SkillLevel.Beginner);

        _userRepository.GetByExternalIdAsync(externalId, Arg.Any<CancellationToken>())
            .Returns(user);

        var query = new GetUserPublicProfileQuery(externalId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.FirstName.Should().Be("Jane");
        result.Value.LastNameInitial.Should().BeNull(); // No LastName = null initial
        result.Value.SkillLevel.Should().Be("Beginner");
        result.Value.Bio.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithNonExistentUser_ShouldReturnNotFound()
    {
        // Arrange
        var externalId = "auth0|nonexistent";
        _userRepository.GetByExternalIdAsync(externalId, Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var query = new GetUserPublicProfileQuery(externalId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Status.Should().Be(ResultStatus.NotFound);
        result.Errors.Should().Contain("User not found");
    }

    [Fact]
    public async Task Handle_WithUserWithEmptyLastName_ShouldReturnNullLastNameInitial()
    {
        // Arrange
        var externalId = "auth0|empty-lastname";
        var user = User.CreateFromExternalAuth(externalId, "Bob", SkillLevel.Intermediate);
        user.UpdateProfile("Bob", "   ", SkillLevel.Intermediate, null); // Whitespace-only

        _userRepository.GetByExternalIdAsync(externalId, Arg.Any<CancellationToken>())
            .Returns(user);

        var query = new GetUserPublicProfileQuery(externalId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.LastNameInitial.Should().BeNull(); // Whitespace should be treated as null
    }

    [Fact]
    public async Task Handle_WithDifferentLastNameInitials_ShouldReturnCorrectInitial()
    {
        // Arrange
        var testCases = new[]
        {
            ("Smith", "S."),
            ("Anderson", "A."),
            ("Zulu", "Z."),
            ("O'Brien", "O.") // Test with special character
        };

        foreach (var (lastName, expectedInitial) in testCases)
        {
            var externalId = $"auth0|user-{lastName}";
            var user = User.CreateFromExternalAuth(externalId, "Test", SkillLevel.Intermediate);
            user.UpdateProfile("Test", lastName, SkillLevel.Intermediate, null);

            _userRepository.GetByExternalIdAsync(externalId, Arg.Any<CancellationToken>())
                .Returns(user);

            var query = new GetUserPublicProfileQuery(externalId);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.LastNameInitial.Should().Be(expectedInitial,
                $"because LastName '{lastName}' should show initial '{expectedInitial}'");
        }
    }

    [Fact]
    public async Task Handle_WithDifferentSkillLevels_ShouldReturnCorrectSkillLevel()
    {
        // Arrange - Test each skill level
        var testCases = new[]
        {
            (SkillLevel.Beginner, "Beginner"),
            (SkillLevel.Intermediate, "Intermediate"),
            (SkillLevel.Advanced, "Advanced")
        };

        foreach (var (skillLevel, expectedString) in testCases)
        {
            var externalId = $"auth0|user-{skillLevel}";
            var user = User.CreateFromExternalAuth(externalId, "Test", skillLevel);

            _userRepository.GetByExternalIdAsync(externalId, Arg.Any<CancellationToken>())
                .Returns(user);

            var query = new GetUserPublicProfileQuery(externalId);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.SkillLevel.Should().Be(expectedString,
                $"because skill level {skillLevel} should map to '{expectedString}'");
        }
    }

    [Fact]
    public async Task Handle_WithAllProfileFields_ShouldHideFullLastName()
    {
        // Arrange
        var externalId = "auth0|complete-profile";
        var user = User.CreateFromExternalAuth(externalId, "Alice", SkillLevel.Advanced);
        user.UpdateProfile(
            firstName: "Alice",
            lastName: "Wonderland", // Should be hidden
            skillLevel: SkillLevel.Advanced,
            bio: "Professional padel player");

        _userRepository.GetByExternalIdAsync(externalId, Arg.Any<CancellationToken>())
            .Returns(user);

        var query = new GetUserPublicProfileQuery(externalId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.FirstName.Should().Be("Alice");
        result.Value.LastNameInitial.Should().Be("W."); // Privacy: NOT "Wonderland"
        result.Value.LastNameInitial.Should().NotContain("onderland");
        result.Value.Bio.Should().Be("Professional padel player");
    }

    [Fact]
    public async Task Handle_ShouldNotExposeFullLastName()
    {
        // Arrange - Verify privacy rule: NEVER expose full LastName
        var externalId = "auth0|privacy-test";
        var user = User.CreateFromExternalAuth(externalId, "John", SkillLevel.Intermediate);
        user.UpdateProfile("John", "PrivateLastName", SkillLevel.Intermediate, null);

        _userRepository.GetByExternalIdAsync(externalId, Arg.Any<CancellationToken>())
            .Returns(user);

        var query = new GetUserPublicProfileQuery(externalId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert - Verify LastName is NOT exposed in ANY field
        result.IsSuccess.Should().BeTrue();
        var dto = result.Value;

        dto.LastNameInitial.Should().Be("P.");
        dto.LastNameInitial.Should().NotContain("rivate");
        dto.LastNameInitial.Should().NotContain("LastName");

        // Verify other fields don't contain the LastName either
        dto.FirstName.Should().NotContain("PrivateLastName");
        dto.Bio.Should().NotContain("PrivateLastName");
    }
}
