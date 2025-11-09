using FluentAssertions;
using Vibora.Users.Domain;
using Vibora.Users.Domain.Events;
using Xunit;

namespace Vibora.Users.Tests.Domain;

public class UserTests
{
    [Fact]
    public void CreateFromExternalAuth_ShouldCreateValidUser()
    {
        // Arrange
        var externalId = "auth0|12345";
        var firstName = "John";
        var skillLevel = SkillLevel.Intermediate;

        // Act
        var user = User.CreateFromExternalAuth(externalId, firstName, skillLevel);

        // Assert
        user.Should().NotBeNull();
        user.ExternalId.Should().Be(externalId);
        user.FirstName.Should().Be(firstName);
        user.Name.Should().Be(firstName); // Legacy field
        user.SkillLevel.Should().Be(skillLevel);
        user.IsGuest.Should().BeFalse();
        user.Bio.Should().BeNull();
        user.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        user.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        user.LastSyncedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void CreateFromExternalAuth_WithDefaultSkillLevel_ShouldUseIntermediate()
    {
        // Arrange
        var externalId = "auth0|12345";
        var firstName = "John";

        // Act
        var user = User.CreateFromExternalAuth(externalId, firstName);

        // Assert
        user.SkillLevel.Should().Be(SkillLevel.Intermediate);
    }

    [Fact]
    public void CreateGuestUser_ShouldCreateValidGuestUser()
    {
        // Arrange
        var name = "Guest Player";
        var skillLevel = SkillLevel.Beginner;

        // Act
        var user = User.CreateGuestUser(name, skillLevel);

        // Assert
        user.Should().NotBeNull();
        user.ExternalId.Should().StartWith("guest:");
        user.FirstName.Should().Be(name);
        user.Name.Should().Be(name);
        user.SkillLevel.Should().Be(skillLevel);
        user.IsGuest.Should().BeTrue();
        user.Bio.Should().BeNull();
        user.LastSyncedAt.Should().BeNull();
    }

    [Fact]
    public void UpdateProfile_WithValidData_ShouldSucceed()
    {
        // Arrange
        var user = User.CreateFromExternalAuth("auth0|123", "John", SkillLevel.Beginner);
        var firstName = "John";
        var lastName = "Doe";
        var skillLevel = SkillLevel.Advanced;
        var bio = "I love playing padel!";

        // Act
        var result = user.UpdateProfile(firstName, lastName, skillLevel, bio);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.FirstName.Should().Be(firstName);
        user.LastName.Should().Be(lastName);
        user.SkillLevel.Should().Be(skillLevel);
        user.Bio.Should().Be(bio);
        user.Name.Should().Be("John Doe"); // Legacy field combines first + last
        user.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void UpdateProfile_WithoutLastName_ShouldSetNameToFirstNameOnly()
    {
        // Arrange
        var user = User.CreateFromExternalAuth("auth0|123", "John", SkillLevel.Beginner);

        // Act
        var result = user.UpdateProfile("Jane", null, SkillLevel.Intermediate, "Bio");

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.FirstName.Should().Be("Jane");
        user.LastName.Should().BeNull();
        user.Name.Should().Be("Jane"); // Legacy field is just first name
    }

    [Fact]
    public void UpdateProfile_WithoutBio_ShouldClearBio()
    {
        // Arrange
        var user = User.CreateFromExternalAuth("auth0|123", "John", SkillLevel.Beginner);
        user.UpdateProfile("John", "Doe", SkillLevel.Intermediate, "Initial bio");

        // Act
        var result = user.UpdateProfile("John", "Doe", SkillLevel.Advanced, null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.Bio.Should().BeNull();
    }

    [Fact]
    public void UpdateProfile_ShouldRaiseDomainEvent()
    {
        // Arrange
        var user = User.CreateFromExternalAuth("auth0|123", "John", SkillLevel.Beginner);

        // Act
        var result = user.UpdateProfile("John", "Doe", SkillLevel.Advanced, "Bio");

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.DomainEvents.Should().ContainSingle();
        user.DomainEvents.First().Should().BeOfType<UserProfileUpdatedDomainEvent>();
        var domainEvent = user.DomainEvents.First() as UserProfileUpdatedDomainEvent;
        domainEvent!.UserExternalId.Should().Be("auth0|123");
    }

    [Fact]
    public void UpdateProfile_WithEmptyFirstName_ShouldReturnError()
    {
        // Arrange
        var user = User.CreateFromExternalAuth("auth0|123", "John", SkillLevel.Beginner);

        // Act
        var result = user.UpdateProfile("", null, SkillLevel.Intermediate, null);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().ContainSingle(e => e.ErrorMessage == "FirstName is required");
    }

    [Fact]
    public void UpdateProfile_WithFirstNameTooShort_ShouldReturnError()
    {
        // Arrange
        var user = User.CreateFromExternalAuth("auth0|123", "John", SkillLevel.Beginner);

        // Act
        var result = user.UpdateProfile("J", null, SkillLevel.Intermediate, null);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().ContainSingle(e => e.ErrorMessage == "FirstName must be at least 2 characters");
    }

    [Fact]
    public void UpdateProfile_WithFirstNameTooLong_ShouldReturnError()
    {
        // Arrange
        var user = User.CreateFromExternalAuth("auth0|123", "John", SkillLevel.Beginner);
        var longName = new string('A', 51);

        // Act
        var result = user.UpdateProfile(longName, null, SkillLevel.Intermediate, null);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().ContainSingle(e => e.ErrorMessage == "FirstName must not exceed 50 characters");
    }

    [Fact]
    public void UpdateProfile_WithLastNameTooLong_ShouldReturnError()
    {
        // Arrange
        var user = User.CreateFromExternalAuth("auth0|123", "John", SkillLevel.Beginner);
        var longLastName = new string('B', 51);

        // Act
        var result = user.UpdateProfile("John", longLastName, SkillLevel.Intermediate, null);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().ContainSingle(e => e.ErrorMessage == "LastName must not exceed 50 characters");
    }

    [Fact]
    public void UpdateProfile_WithBioTooLong_ShouldReturnError()
    {
        // Arrange
        var user = User.CreateFromExternalAuth("auth0|123", "John", SkillLevel.Beginner);
        var longBio = new string('C', 151);

        // Act
        var result = user.UpdateProfile("John", null, SkillLevel.Intermediate, longBio);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().ContainSingle(e => e.ErrorMessage == "Bio must not exceed 150 characters");
    }

    [Fact]
    public void UpdateProfile_WithMultipleErrors_ShouldReturnAllErrors()
    {
        // Arrange
        var user = User.CreateFromExternalAuth("auth0|123", "John", SkillLevel.Beginner);
        var longLastName = new string('B', 51);
        var longBio = new string('C', 151);

        // Act
        var result = user.UpdateProfile("J", longLastName, SkillLevel.Intermediate, longBio);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().HaveCount(3);
        result.ValidationErrors.Should().Contain(e => e.ErrorMessage == "FirstName must be at least 2 characters");
        result.ValidationErrors.Should().Contain(e => e.ErrorMessage == "LastName must not exceed 50 characters");
        result.ValidationErrors.Should().Contain(e => e.ErrorMessage == "Bio must not exceed 150 characters");
    }

    [Fact]
    public void SyncFromExternalProvider_ShouldUpdateNameAndLastSyncedAt()
    {
        // Arrange
        var user = User.CreateFromExternalAuth("auth0|123", "OldName", SkillLevel.Beginner);
        var originalLastSynced = user.LastSyncedAt;
        Thread.Sleep(10); // Ensure time difference

        // Act
        user.SyncFromExternalProvider("NewName");

        // Assert
        user.Name.Should().Be("NewName");
        user.FirstName.Should().Be("NewName");
        user.LastSyncedAt.Should().BeAfter(originalLastSynced!.Value);
    }
}
