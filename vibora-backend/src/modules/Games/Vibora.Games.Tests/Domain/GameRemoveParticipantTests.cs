using FluentAssertions;
using Vibora.Games.Domain;
using Vibora.Games.Domain.Events;
using Xunit;

namespace Vibora.Games.Tests.Domain;

public class GameRemoveParticipantTests
{
    [Fact]
    public void RemoveParticipant_WithValidUser_ShouldSucceed()
    {
        // Arrange
        var game = GameTestHelpers.CreateValidGame();
        var userExternalId = "auth0|999";
        game.AddParticipant(userExternalId, "Jane", "Intermediate");

        // Act
        var result = game.RemoveParticipant(userExternalId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        game.CurrentPlayers.Should().Be(1);
        game.Participations.Should().HaveCount(1);
        game.Participations.Should().NotContain(p => p.UserExternalId == userExternalId);
    }

    [Fact]
    public void RemoveParticipant_WhenUserNotParticipant_ShouldReturnError()
    {
        // Arrange
        var game = GameTestHelpers.CreateValidGame();

        // Act
        var result = game.RemoveParticipant("auth0|nonexistent");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("User is not a participant of this game");
    }

    [Fact]
    public void RemoveParticipant_WhenUserIsHost_ShouldSucceed()
    {
        // Arrange
        var game = GameTestHelpers.CreateValidGame();
        var hostExternalId = game.HostExternalId;
        var initialPlayerCount = game.CurrentPlayers;

        // Act
        var result = game.RemoveParticipant(hostExternalId);

        // Assert - Host can now leave like any other participant
        result.IsSuccess.Should().BeTrue();
        game.CurrentPlayers.Should().Be(initialPlayerCount - 1);
    }

    [Fact]
    public void RemoveParticipant_FromFullGame_ShouldSetStatusToOpen()
    {
        // Arrange
        var game = GameTestHelpers.CreateValidGame(maxPlayers: 2);
        var userExternalId = "auth0|999";
        game.AddParticipant(userExternalId, "Jane", "Intermediate");
        game.Status.Should().Be(GameStatus.Full);

        // Act
        game.RemoveParticipant(userExternalId);

        // Assert
        game.Status.Should().Be(GameStatus.Open);
    }

    [Fact]
    public void RemoveParticipant_ShouldRaiseParticipationRemovedDomainEvent()
    {
        // Arrange
        var game = GameTestHelpers.CreateValidGame();
        var userExternalId = "auth0|player-123";
        var userName = "Jane Doe";
        game.AddParticipant(userExternalId, userName, "Intermediate");
        
        // Clear domain events from AddParticipant (if any)
        game.ClearDomainEvents();

        // Act
        var result = game.RemoveParticipant(userExternalId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        
        // Verify domain event was raised
        game.DomainEvents.Should().ContainSingle();
        var domainEvent = game.DomainEvents.Single() as ParticipationRemovedDomainEvent;
        domainEvent.Should().NotBeNull();
        domainEvent!.GameId.Should().Be(game.Id);
        domainEvent.UserExternalId.Should().Be(userExternalId);
        domainEvent.UserName.Should().Be(userName);
        domainEvent.RemainingPlayers.Should().Be(1); // Only host remaining
        domainEvent.GameStatus.Should().Be(GameStatus.Open);
        domainEvent.OccurredOn.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }
}
