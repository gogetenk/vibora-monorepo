using Ardalis.Result;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Vibora.Games.Application;
using Vibora.Games.Application.Commands.JoinGameAsGuest;
using Vibora.Games.Domain;
using Vibora.Users.Contracts.Services;

namespace Vibora.Games.Tests.Application.Commands;

public class JoinGameAsGuestCommandHandlerTests
{
    private readonly Mock<IGameRepository> _gameRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IUsersServiceClient> _usersServiceClientMock;
    private readonly Mock<ILogger<JoinGameAsGuestCommandHandler>> _loggerMock;
    private readonly JoinGameAsGuestCommandHandler _handler;

    public JoinGameAsGuestCommandHandlerTests()
    {
        _gameRepositoryMock = new Mock<IGameRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _usersServiceClientMock = new Mock<IUsersServiceClient>();
        _loggerMock = new Mock<ILogger<JoinGameAsGuestCommandHandler>>();

        // Setup default behavior for IUsersServiceClient
        _usersServiceClientMock
            .Setup(c => c.CreateOrUpdateGuestUserAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string name, string? phone, string? email, int skill, CancellationToken ct) =>
                $"guest:{Guid.NewGuid()}");

        _handler = new JoinGameAsGuestCommandHandler(
            _gameRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _usersServiceClientMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommandAndPhoneNumber_ShouldJoinGameSuccessfully()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var game = CreateTestGame(gameId);
        
        _gameRepositoryMock.Setup(r => r.GetByIdAsync(gameId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(game));

        var command = new JoinGameAsGuestCommand(
            gameId,
            "Guest Player",
            "+33612345678",
            null
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.GameId.Should().Be(gameId);
        result.Value.GuestName.Should().Be("Guest Player");
        result.Value.CurrentPlayers.Should().Be(2); // Host + guest
        result.Value.Message.Should().Contain("Successfully joined");
        
        game.CurrentPlayers.Should().Be(2);
        game.GuestParticipants.Should().HaveCount(1);
        
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidCommandAndEmail_ShouldJoinGameSuccessfully()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var game = CreateTestGame(gameId);
        
        _gameRepositoryMock.Setup(r => r.GetByIdAsync(gameId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(game));

        var command = new JoinGameAsGuestCommand(
            gameId,
            "Guest Player",
            null,
            "guest@example.com"
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.GuestName.Should().Be("Guest Player");
        
        game.GuestParticipants.Should().HaveCount(1);
        game.GuestParticipants.First().Email.Should().Be("guest@example.com");
    }

    [Fact]
    public async Task Handle_WithBothPhoneAndEmail_ShouldJoinGameSuccessfully()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var game = CreateTestGame(gameId);
        
        _gameRepositoryMock.Setup(r => r.GetByIdAsync(gameId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(game));

        var command = new JoinGameAsGuestCommand(
            gameId,
            "Guest Player",
            "+33612345678",
            "guest@example.com"
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        game.GuestParticipants.Should().HaveCount(1);
        game.GuestParticipants.First().PhoneNumber.Should().Be("+33612345678");
        game.GuestParticipants.First().Email.Should().Be("guest@example.com");
    }

    [Fact]
    public async Task Handle_WithEmptyGameId_ShouldReturnInvalid()
    {
        // Arrange
        var command = new JoinGameAsGuestCommand(
            Guid.Empty,
            "Guest",
            "+33612345678",
            null
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.Invalid);
        result.ValidationErrors.Should().Contain(e => e.ErrorMessage == "GameId cannot be empty");
        
        _gameRepositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithEmptyName_ShouldReturnInvalid()
    {
        // Arrange
        var command = new JoinGameAsGuestCommand(
            Guid.NewGuid(),
            "",
            "+33612345678",
            null
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.Invalid);
        result.ValidationErrors.Should().Contain(e => e.ErrorMessage == "Guest name is required");
    }

    [Fact]
    public async Task Handle_WithNoContactInfo_ShouldReturnInvalid()
    {
        // Arrange
        var command = new JoinGameAsGuestCommand(
            Guid.NewGuid(),
            "Guest",
            null,
            null
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.Invalid);
        result.ValidationErrors.Should().Contain(e => e.ErrorMessage == "Either phone number or email is required");
    }

    [Fact]
    public async Task Handle_WithNonExistentGame_ShouldReturnNotFound()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        _gameRepositoryMock.Setup(r => r.GetByIdAsync(gameId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Game>.NotFound($"Game with ID '{gameId}' not found"));

        var command = new JoinGameAsGuestCommand(
            gameId,
            "Guest",
            "+33612345678",
            null
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.NotFound);
        result.Errors.Should().Contain($"Game with ID '{gameId}' not found");
    }

    [Fact]
    public async Task Handle_WhenGameIsCanceled_ShouldReturnError()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var game = CreateTestGame(gameId);
        game.Cancel(); // Cancel the game
        
        _gameRepositoryMock.Setup(r => r.GetByIdAsync(gameId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(game));

        var command = new JoinGameAsGuestCommand(
            gameId,
            "Guest",
            "+33612345678",
            null
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.Error);
        result.Errors.Should().Contain("Cannot join a canceled game");
    }

    [Fact]
    public async Task Handle_WhenGameIsFull_ShouldReturnError()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var game = CreateFullGame(gameId);
        
        _gameRepositoryMock.Setup(r => r.GetByIdAsync(gameId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(game));

        var command = new JoinGameAsGuestCommand(
            gameId,
            "Guest",
            "+33612345678",
            null
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.Error);
        result.Errors.Should().Contain("Game is already full");
    }

    [Fact]
    public async Task Handle_WhenMaxGuestsReached_ShouldReturnError()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var game = CreateGameWithMaxGuests(gameId);
        
        _gameRepositoryMock.Setup(r => r.GetByIdAsync(gameId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(game));

        var command = new JoinGameAsGuestCommand(
            gameId,
            "Guest 3",
            "+33600000003",
            null
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.Error);
        result.Errors.Should().Contain("Maximum 2 guests allowed per game");
    }

    [Fact]
    public async Task Handle_WhenGuestAlreadyJoinedWithSamePhone_ShouldReturnError()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var game = CreateTestGame(gameId);
        var phoneNumber = "+33612345678";
        game.AddGuestParticipant("Existing Guest", phoneNumber, null);
        
        _gameRepositoryMock.Setup(r => r.GetByIdAsync(gameId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(game));

        var command = new JoinGameAsGuestCommand(
            gameId,
            "New Guest",
            phoneNumber, // Same phone
            null
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.Error);
        result.Errors.Should().Contain("This phone number or email has already joined this game");
    }

    [Fact]
    public async Task Handle_WhenGuestAlreadyJoinedWithSameEmail_ShouldReturnError()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var game = CreateTestGame(gameId);
        var email = "guest@example.com";
        game.AddGuestParticipant("Existing Guest", null, email);
        
        _gameRepositoryMock.Setup(r => r.GetByIdAsync(gameId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(game));

        var command = new JoinGameAsGuestCommand(
            gameId,
            "New Guest",
            null,
            email // Same email
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.Error);
        result.Errors.Should().Contain("This phone number or email has already joined this game");
    }

    [Fact]
    public async Task Handle_WhenJoiningMakesGameFull_ShouldUpdateStatusToFull()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var game = CreateAlmostFullGame(gameId); // 3/4 players
        
        _gameRepositoryMock.Setup(r => r.GetByIdAsync(gameId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(game));

        var command = new JoinGameAsGuestCommand(
            gameId,
            "Last Guest",
            "+33612345678",
            null
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        game.CurrentPlayers.Should().Be(4);
        game.Status.Should().Be(GameStatus.Full);
    }

    // Note: Test for "game already started" is not included because Game.Create() 
    // prevents creating games in the past (validation at creation time).
    // In production, this scenario would be caught earlier in the flow.

    // Helper methods
    private Game CreateTestGame(Guid gameId)
    {
        var gameResult = Game.Create(
            "auth0|host",
            "Host",
            "5",
            DateTime.UtcNow.AddDays(1),
            "Test Club",
            "5",
            4
        );

        var game = gameResult.Value;
        typeof(Game).GetProperty("Id")!.SetValue(game, gameId);

        return game;
    }

    private Game CreateFullGame(Guid gameId)
    {
        var game = CreateTestGame(gameId);
        game.AddParticipant("auth0|player2", "Player 2", "5");
        game.AddParticipant("auth0|player3", "Player 3", "5");
        game.AddParticipant("auth0|player4", "Player 4", "5");
        
        return game;
    }

    private Game CreateAlmostFullGame(Guid gameId)
    {
        var game = CreateTestGame(gameId);
        game.AddParticipant("auth0|player2", "Player 2", "5");
        game.AddParticipant("auth0|player3", "Player 3", "5");
        
        return game;
    }

    private Game CreateGameWithMaxGuests(Guid gameId)
    {
        var game = CreateTestGame(gameId);
        game.AddGuestParticipant("Guest 1", "+33600000001", null);
        game.AddGuestParticipant("Guest 2", "+33600000002", null);
        
        return game;
    }
}
