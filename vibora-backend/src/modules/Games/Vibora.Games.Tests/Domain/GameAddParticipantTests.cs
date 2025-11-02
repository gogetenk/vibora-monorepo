using FluentAssertions;
using Vibora.Games.Domain;
using Xunit;

namespace Vibora.Games.Tests.Domain;

public class GameAddParticipantTests
{
    [Fact]
    public void AddParticipant_WithValidUser_ShouldSucceed()
    {
        // Arrange
        var game = GameTestHelpers.CreateValidGame();
        var userExternalId = "auth0|999";
        var userName = "Jane Doe";
        var userSkillLevel = "Intermediate";

        // Act
        var result = game.AddParticipant(userExternalId, userName, userSkillLevel);

        // Assert
        result.IsSuccess.Should().BeTrue();
        game.CurrentPlayers.Should().Be(2);
        game.Participations.Should().HaveCount(2);
        
        var participant = game.Participations.First(p => p.UserExternalId == userExternalId);
        participant.UserName.Should().Be(userName);
        participant.UserSkillLevel.Should().Be(userSkillLevel);
        participant.IsHost.Should().BeFalse();
    }

    [Fact]
    public void AddParticipant_WhenGameIsFull_ShouldReturnError()
    {
        // Arrange
        var game = GameTestHelpers.CreateValidGame(maxPlayers: 2); // Already has 1 (host)
        game.AddParticipant("auth0|222", "User2", "Beginner");

        // Act
        var result = game.AddParticipant("auth0|333", "User3", "Beginner");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Game is already full");
    }

    [Fact]
    public void AddParticipant_WhenUserAlreadyJoined_ShouldReturnError()
    {
        // Arrange
        var game = GameTestHelpers.CreateValidGame();
        var userExternalId = "auth0|999";
        game.AddParticipant(userExternalId, "Jane", "Intermediate");

        // Act
        var result = game.AddParticipant(userExternalId, "Jane", "Intermediate");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("User already joined this game");
    }

    [Fact]
    public void AddParticipant_WhenGameReachesMaxPlayers_ShouldSetStatusToFull()
    {
        // Arrange
        var game = GameTestHelpers.CreateValidGame(maxPlayers: 2); // Host already joined (1/2)

        // Act
        game.AddParticipant("auth0|222", "User2", "Beginner");

        // Assert
        game.Status.Should().Be(GameStatus.Full);
    }

    [Theory]
    [InlineData("5", "5", true)]  // Same level
    [InlineData("5", "4", true)]  // -1 level
    [InlineData("5", "6", true)]  // +1 level
    [InlineData("5", "3", false)] // -2 levels (not allowed)
    [InlineData("5", "7", false)] // +2 levels (not allowed)
    [InlineData("3", "2", true)]  // Edge case: level 3 accepts 2
    [InlineData("3", "1", false)] // Edge case: level 3 rejects 1
    public void AddParticipant_SkillLevelValidation_ShouldEnforceRules(
        string gameLevel, string playerLevel, bool shouldSucceed)
    {
        // Arrange
        var game = GameTestHelpers.CreateValidGame(skillLevel: gameLevel);

        // Act
        var result = game.AddParticipant("auth0|player", "Player", playerLevel);

        // Assert
        result.IsSuccess.Should().Be(shouldSucceed);
        if (!shouldSucceed)
        {
            result.Errors.Should().Contain(e => e.Contains("doesn't match"));
        }
    }

    [Fact]
    public void AddParticipant_WhenGameHasNoSkillLevel_ShouldAllowAnyPlayer()
    {
        // Arrange
        var game = GameTestHelpers.CreateValidGame(skillLevel: "");

        // Act
        var result = game.AddParticipant("auth0|player", "Player", "10");

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void AddParticipant_WhenGameSkillLevelIsNonNumeric_ShouldAllowAnyPlayer()
    {
        // Arrange
        var game = GameTestHelpers.CreateValidGame(skillLevel: "Advanced");

        // Act
        var result = game.AddParticipant("auth0|player", "Player", "5");

        // Assert
        result.IsSuccess.Should().BeTrue();
    }
}
