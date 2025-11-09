using FluentAssertions;
using Vibora.Games.Domain;
using Vibora.Games.Domain.Events;
using Xunit;

namespace Vibora.Games.Tests.Domain;

public class GameCreateTests
{
    [Fact]
    public void Create_WithValidParameters_ShouldReturnSuccessResult()
    {
        // Arrange
        var hostExternalId = "auth0|123456";
        var hostName = "John Doe";
        var hostSkillLevel = "Advanced";
        var dateTime = DateTime.UtcNow.AddDays(1);
        var location = "Club de Padel";
        var skillLevel = "Intermediate";
        var maxPlayers = 4;

        // Act
        var result = Game.Create(
            hostExternalId,
            hostName,
            hostSkillLevel,
            dateTime,
            location,
            skillLevel,
            maxPlayers);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.HostExternalId.Should().Be(hostExternalId);
        result.Value.DateTime.Should().Be(dateTime);
        result.Value.Location.Should().Be(location);
        result.Value.SkillLevel.Should().Be(skillLevel);
        result.Value.MaxPlayers.Should().Be(maxPlayers);
        result.Value.CurrentPlayers.Should().Be(1); // Host auto-joined
        result.Value.Status.Should().Be(GameStatus.Open);
    }

    [Fact]
    public void Create_ShouldAutoJoinHostAsFirstParticipant()
    {
        // Arrange
        var hostExternalId = "auth0|123456";
        var hostName = "John Doe";
        var hostSkillLevel = "Advanced";
        var dateTime = DateTime.UtcNow.AddDays(1);

        // Act
        var result = Game.Create(
            hostExternalId,
            hostName,
            hostSkillLevel,
            dateTime,
            "Club",
            "Intermediate",
            4);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var game = result.Value;
        
        game.Participations.Should().HaveCount(1);
        var hostParticipation = game.Participations.First();
        hostParticipation.UserExternalId.Should().Be(hostExternalId);
        hostParticipation.UserName.Should().Be(hostName);
        hostParticipation.UserSkillLevel.Should().Be(hostSkillLevel);
        hostParticipation.IsHost.Should().BeTrue();
    }

    [Fact]
    public void Create_ShouldRaiseDomainEvent()
    {
        // Arrange
        var hostExternalId = "auth0|123456";
        var dateTime = DateTime.UtcNow.AddDays(1);

        // Act
        var result = Game.Create(
            hostExternalId,
            "John Doe",
            "Advanced",
            dateTime,
            "Club",
            "Intermediate",
            4);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var game = result.Value;
        
        game.DomainEvents.Should().HaveCount(1);
        game.DomainEvents.First().Should().BeOfType<GameCreatedDomainEvent>();
    }

    [Fact]
    public void Create_WithPastDateTime_ShouldReturnValidationError()
    {
        // Arrange
        var pastDateTime = DateTime.UtcNow.AddHours(-1);

        // Act
        var result = Game.Create(
            "auth0|123",
            "John",
            "Advanced",
            pastDateTime,
            "Club",
            "Intermediate",
            4);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().Contain(e => 
            e.ErrorMessage.Contains("future", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithInvalidLocation_ShouldReturnValidationError(string? location)
    {
        // Arrange
        var dateTime = DateTime.UtcNow.AddDays(1);

        // Act
        var result = Game.Create(
            "auth0|123",
            "John",
            "Advanced",
            dateTime,
            location!,
            "Intermediate",
            4);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().Contain(e => 
            e.ErrorMessage.Contains("Location", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Create_WithLocationTooLong_ShouldReturnValidationError()
    {
        // Arrange
        var dateTime = DateTime.UtcNow.AddDays(1);
        var longLocation = new string('A', 501);

        // Act
        var result = Game.Create(
            "auth0|123",
            "John",
            "Advanced",
            dateTime,
            longLocation,
            "Intermediate",
            4);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().Contain(e => 
            e.ErrorMessage.Contains("500"));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(11)]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithInvalidMaxPlayers_ShouldReturnValidationError(int maxPlayers)
    {
        // Arrange
        var dateTime = DateTime.UtcNow.AddDays(1);

        // Act
        var result = Game.Create(
            "auth0|123",
            "John",
            "Advanced",
            dateTime,
            "Club",
            "Intermediate",
            maxPlayers);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().Contain(e => 
            e.ErrorMessage.Contains("between 2 and 10", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Create_WithMultipleValidationErrors_ShouldReturnAllErrors()
    {
        // Arrange
        var pastDateTime = DateTime.UtcNow.AddHours(-1);
        var emptyLocation = "";
        var invalidMaxPlayers = 20;

        // Act
        var result = Game.Create(
            "auth0|123",
            "John",
            "Advanced",
            pastDateTime,
            emptyLocation,
            "Intermediate",
            invalidMaxPlayers);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().HaveCountGreaterThanOrEqualTo(3);
        result.ValidationErrors.Should().Contain(e => e.ErrorMessage.Contains("future"));
        result.ValidationErrors.Should().Contain(e => e.ErrorMessage.Contains("Location"));
        result.ValidationErrors.Should().Contain(e => e.ErrorMessage.Contains("between 2 and 10"));
    }
}
