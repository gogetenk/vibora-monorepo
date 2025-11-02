using FluentAssertions;
using Vibora.Games.Domain;
using Vibora.Games.Domain.Events;
using Xunit;

namespace Vibora.Games.Tests.Domain;

public class GameAddGuestParticipantTests
{
    [Fact]
    public void AddGuestParticipant_WithValidPhoneNumber_ShouldSucceed()
    {
        // Arrange
        var game = GameTestHelpers.CreateValidGame();
        game.ClearDomainEvents();
        
        // Act
        var result = game.AddGuestParticipant("Guest John", "+33612345678", null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        game.GuestParticipants.Should().HaveCount(1);
        game.CurrentPlayers.Should().Be(2); // Host + 1 guest
        
        var guest = game.GuestParticipants.First();
        guest.Name.Should().Be("Guest John");
        guest.PhoneNumber.Should().Be("+33612345678");
        guest.Email.Should().BeNull();
        
        // Verify domain event was raised
        game.DomainEvents.Should().ContainSingle();
        var domainEvent = game.DomainEvents.Single() as GuestJoinedGameDomainEvent;
        domainEvent.Should().NotBeNull();
        domainEvent!.GameId.Should().Be(game.Id);
        domainEvent.GuestParticipantId.Should().Be(guest.Id);
        domainEvent.GuestName.Should().Be("Guest John");
        domainEvent.ContactIdentifier.Should().Be("+33612345678");
        domainEvent.CurrentPlayers.Should().Be(2);
    }

    [Fact]
    public void AddGuestParticipant_WithValidEmail_ShouldSucceed()
    {
        // Arrange
        var game = GameTestHelpers.CreateValidGame();
        game.ClearDomainEvents();
        
        // Act
        var result = game.AddGuestParticipant("Guest Jane", null, "jane@example.com");

        // Assert
        result.IsSuccess.Should().BeTrue();
        game.GuestParticipants.Should().HaveCount(1);
        game.CurrentPlayers.Should().Be(2); // Host + 1 guest
        
        var guest = game.GuestParticipants.First();
        guest.Name.Should().Be("Guest Jane");
        guest.PhoneNumber.Should().BeNull();
        guest.Email.Should().Be("jane@example.com");
    }

    [Fact]
    public void AddGuestParticipant_WhenGameIsFull_ShouldReturnError()
    {
        // Arrange
        var game = GameTestHelpers.CreateValidGame(maxPlayers: 2); // Only 2 players max
        game.AddParticipant("auth0|player1", "Player One", "Intermediate"); // Game is now full
        
        // Act
        var result = game.AddGuestParticipant("Guest John", "+33612345678", null);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Game is already full");
        game.GuestParticipants.Should().BeEmpty();
    }

    [Fact]
    public void AddGuestParticipant_WhenMaxGuestsReached_ShouldReturnError()
    {
        // Arrange
        var game = GameTestHelpers.CreateValidGame(maxPlayers: 10);
        game.AddGuestParticipant("Guest 1", "+33611111111", null);
        game.AddGuestParticipant("Guest 2", "+33622222222", null);
        
        // Act
        var result = game.AddGuestParticipant("Guest 3", "+33633333333", null);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Maximum 2 guests allowed per game");
        game.GuestParticipants.Should().HaveCount(2);
    }

    [Fact]
    public void AddGuestParticipant_WithDuplicatePhoneNumber_ShouldReturnError()
    {
        // Arrange
        var game = GameTestHelpers.CreateValidGame();
        var phoneNumber = "+33612345678";
        game.AddGuestParticipant("Guest John", phoneNumber, null);
        
        // Act
        var result = game.AddGuestParticipant("Guest Jane", phoneNumber, null);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("This phone number or email has already joined this game");
        game.GuestParticipants.Should().HaveCount(1);
    }

    [Fact]
    public void AddGuestParticipant_WithDuplicateEmail_ShouldReturnError()
    {
        // Arrange
        var game = GameTestHelpers.CreateValidGame();
        var email = "john@example.com";
        game.AddGuestParticipant("Guest John", null, email);
        
        // Act
        var result = game.AddGuestParticipant("Guest Jane", null, email);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("This phone number or email has already joined this game");
        game.GuestParticipants.Should().HaveCount(1);
    }

    [Fact]
    public void AddGuestParticipant_ShouldUpdateGameStatus_WhenFull()
    {
        // Arrange
        var game = GameTestHelpers.CreateValidGame(maxPlayers: 2);
        
        // Act
        var result = game.AddGuestParticipant("Guest John", "+33612345678", null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        game.CurrentPlayers.Should().Be(2);
        game.Status.Should().Be(GameStatus.Full);
    }
}
