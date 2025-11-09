using Ardalis.Result;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Vibora.Users.Application.Queries.GetCurrentUserProfile;
using Vibora.Users.Domain;
using Vibora.Users.Infrastructure.Data;
using Xunit;

namespace Vibora.Users.Tests.Application.Queries;

public class GetCurrentUserProfileQueryHandlerTests
{
    private readonly IUserRepository _userRepository;
    private readonly UsersDbContext _dbContext;
    private readonly GetCurrentUserProfileQueryHandler _handler;

    public GetCurrentUserProfileQueryHandlerTests()
    {
        _userRepository = Substitute.For<IUserRepository>();

        // Create in-memory database for testing SQL queries
        var options = new DbContextOptionsBuilder<UsersDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new UsersDbContext(options);

        _handler = new GetCurrentUserProfileQueryHandler(_userRepository, _dbContext);
    }

    [Fact]
    public async Task Handle_WithExistingUser_ShouldReturnFullProfile()
    {
        // Arrange
        var externalId = "auth0|test-user";
        var user = User.CreateFromExternalAuth(externalId, "John", SkillLevel.Advanced);
        user.UpdateProfile("John", "Doe", SkillLevel.Advanced, "Padel enthusiast");

        _userRepository.GetByExternalIdAsync(externalId, Arg.Any<CancellationToken>())
            .Returns(user);

        var query = new GetCurrentUserProfileQuery(externalId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ExternalId.Should().Be(externalId);
        result.Value.FirstName.Should().Be("John");
        result.Value.LastName.Should().Be("Doe");
        result.Value.SkillLevel.Should().Be("Advanced");
        result.Value.Bio.Should().Be("Padel enthusiast");
        result.Value.PhotoUrl.Should().BeNull();
        result.Value.GamesPlayedCount.Should().BeGreaterOrEqualTo(0); // Can be 0 if no games
        result.Value.MemberSince.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Handle_WithUserWithoutOptionalFields_ShouldReturnBasicProfile()
    {
        // Arrange
        var externalId = "auth0|minimal-user";
        var user = User.CreateFromExternalAuth(externalId, "Jane", SkillLevel.Beginner);

        _userRepository.GetByExternalIdAsync(externalId, Arg.Any<CancellationToken>())
            .Returns(user);

        var query = new GetCurrentUserProfileQuery(externalId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ExternalId.Should().Be(externalId);
        result.Value.FirstName.Should().Be("Jane");
        result.Value.LastName.Should().BeNull();
        result.Value.SkillLevel.Should().Be("Beginner");
        result.Value.Bio.Should().BeNull();
        result.Value.PhotoUrl.Should().BeNull();
        result.Value.GamesPlayedCount.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public async Task Handle_WithNonExistentUser_ShouldReturnNotFound()
    {
        // Arrange
        var externalId = "auth0|nonexistent";
        _userRepository.GetByExternalIdAsync(externalId, Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var query = new GetCurrentUserProfileQuery(externalId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Status.Should().Be(ResultStatus.NotFound);
        result.Errors.Should().Contain("User not found");
    }

    [Fact]
    public async Task Handle_WithUserWithAllFields_ShouldMapAllFieldsCorrectly()
    {
        // Arrange
        var externalId = "auth0|complete-user";
        var user = User.CreateFromExternalAuth(externalId, "Alice", SkillLevel.Intermediate);
        user.UpdateProfile(
            firstName: "Alice",
            lastName: "Smith",
            skillLevel: SkillLevel.Intermediate,
            bio: "Playing padel since 2020");

        _userRepository.GetByExternalIdAsync(externalId, Arg.Any<CancellationToken>())
            .Returns(user);

        var query = new GetCurrentUserProfileQuery(externalId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.ExternalId.Should().Be(externalId);
        result.Value.FirstName.Should().Be("Alice");
        result.Value.LastName.Should().Be("Smith");
        result.Value.SkillLevel.Should().Be("Intermediate");
        result.Value.Bio.Should().Be("Playing padel since 2020");
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

            var query = new GetCurrentUserProfileQuery(externalId);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.SkillLevel.Should().Be(expectedString,
                $"because skill level {skillLevel} should map to '{expectedString}'");
        }
    }
}
