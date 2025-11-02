using FluentAssertions;
using Vibora.Games.Domain;
using Vibora.Games.Domain.Events;
using Xunit;

namespace Vibora.Games.Tests.Domain;

public class GameCancelTests
{
    [Fact]
    public void Cancel_WhenGameIsOpen_ShouldSucceed()
    {
        // Arrange
        var game = GameTestHelpers.CreateValidGame();

        // Act
        var result = game.Cancel();

        // Assert
        result.IsSuccess.Should().BeTrue();
        game.Status.Should().Be(GameStatus.Canceled);
    }

    [Fact]
    public void Cancel_WhenGameAlreadyCanceled_ShouldReturnError()
    {
        // Arrange
        var game = GameTestHelpers.CreateValidGame();
        game.Cancel();

        // Act
        var result = game.Cancel();

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Game is already canceled");
    }

    [Fact]
    public void Cancel_ShouldRaiseGameCanceledDomainEvent()
    {
        // Arrange
        var game = GameTestHelpers.CreateValidGame();
        game.AddParticipant("auth0|player1", "Player 1", "Intermediate");
        game.AddParticipant("auth0|player2", "Player 2", "Intermediate");
        
        // Clear domain events from creation and participants
        game.ClearDomainEvents();

        // Act
        var result = game.Cancel();

        // Assert
        result.IsSuccess.Should().BeTrue();
        game.Status.Should().Be(GameStatus.Canceled);
        
        // Verify domain event was raised
        game.DomainEvents.Should().ContainSingle();
        var domainEvent = game.DomainEvents.Single() as GameCanceledDomainEvent;
        domainEvent.Should().NotBeNull();
        domainEvent!.GameId.Should().Be(game.Id);
        domainEvent.HostExternalId.Should().Be("auth0|host123");
        domainEvent.Location.Should().Be("Test Club");
        domainEvent.TotalParticipants.Should().Be(3); // Host + 2 players
        domainEvent.OccurredOn.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        
        // Verify participants are included in the event
        domainEvent.Participants.Should().HaveCount(3); // Host + 2 players
        domainEvent.GuestParticipants.Should().BeEmpty();
    }

    [Fact]
    public void Cancel_ShouldIncludeGuestsInDomainEvent()
    {
        // Arrange
        var game = GameTestHelpers.CreateValidGame();
        game.AddGuestParticipant("Guest Player", "+33612345678", null);
        
        // Clear domain events from creation and guest join
        game.ClearDomainEvents();

        // Act
        var result = game.Cancel();

        // Assert
        result.IsSuccess.Should().BeTrue();
        
        var domainEvent = game.DomainEvents.Single() as GameCanceledDomainEvent;
        domainEvent.Should().NotBeNull();
        domainEvent!.Participants.Should().HaveCount(1); // Only host
        domainEvent.GuestParticipants.Should().HaveCount(1);
        domainEvent.GuestParticipants.First().Name.Should().Be("Guest Player");
        domainEvent.GuestParticipants.First().PhoneNumber.Should().Be("+33612345678");
    }
}
