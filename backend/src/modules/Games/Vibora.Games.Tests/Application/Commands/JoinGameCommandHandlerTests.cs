using Ardalis.Result;
using FluentAssertions;
using Microsoft.AspNetCore.OutputCaching;
using Moq;
using Vibora.Games.Application;
using Vibora.Games.Application.Commands.JoinGame;
using Vibora.Games.Domain;

namespace Vibora.Games.Tests.Application.Commands;

public class JoinGameCommandHandlerTests
{
    private readonly Mock<IGameRepository> _gameRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IOutputCacheStore> _mockCacheStore;
    private readonly JoinGameCommandHandler _handler;

    public JoinGameCommandHandlerTests()
    {
        _gameRepositoryMock = new Mock<IGameRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mockCacheStore = new Mock<IOutputCacheStore>();
        _handler = new JoinGameCommandHandler(
            _gameRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _mockCacheStore.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldJoinGameSuccessfully()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var game = CreateTestGame(gameId);
        
        _gameRepositoryMock.Setup(r => r.GetByIdWithParticipationsAsync(gameId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(game));

        var command = new JoinGameCommand(
            gameId,
            "auth0|player-123",
            "John Doe",
            "Intermediate"
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.GameId.Should().Be(gameId);
        result.Value.Message.Should().Contain("Successfully joined");
        
        game.CurrentPlayers.Should().Be(2); // Host + new player
        game.Participations.Should().HaveCount(2);
        
        _gameRepositoryMock.Verify(r => r.AddParticipation(It.IsAny<Participation>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithEmptyGameId_ShouldReturnInvalid()
    {
        // Arrange
        var command = new JoinGameCommand(
            Guid.Empty,
            "auth0|player",
            "John",
            "Intermediate"
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.Invalid);
        result.ValidationErrors.Should().Contain(e => e.ErrorMessage == "GameId cannot be empty");
        
        _gameRepositoryMock.Verify(r => r.GetByIdWithParticipationsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithEmptyUserExternalId_ShouldReturnInvalid()
    {
        // Arrange
        var command = new JoinGameCommand(
            Guid.NewGuid(),
            "",
            "John",
            "Intermediate"
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.Invalid);
        result.ValidationErrors.Should().Contain(e => e.ErrorMessage == "UserExternalId is required");
    }

    [Fact]
    public async Task Handle_WithNonExistentGame_ShouldReturnNotFound()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        _gameRepositoryMock.Setup(r => r.GetByIdWithParticipationsAsync(gameId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Game>.NotFound($"Game with ID '{gameId}' not found"));

        var command = new JoinGameCommand(
            gameId,
            "auth0|player",
            "John",
            "Intermediate"
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
        
        _gameRepositoryMock.Setup(r => r.GetByIdWithParticipationsAsync(gameId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(game));

        var command = new JoinGameCommand(
            gameId,
            "auth0|player",
            "John",
            "Intermediate"
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
        
        _gameRepositoryMock.Setup(r => r.GetByIdWithParticipationsAsync(gameId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(game));

        var command = new JoinGameCommand(
            gameId,
            "auth0|player-new",
            "New Player",
            "Intermediate"
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.Error);
        result.Errors.Should().Contain("Game is already full");
    }

    [Fact]
    public async Task Handle_WhenUserAlreadyJoined_ShouldReturnError()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var game = CreateTestGame(gameId);
        var existingUserId = "auth0|player-existing";
        game.AddParticipant(existingUserId, "Existing Player", "Intermediate");
        
        _gameRepositoryMock.Setup(r => r.GetByIdWithParticipationsAsync(gameId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(game));

        var command = new JoinGameCommand(
            gameId,
            existingUserId, // Same user trying to join again
            "Existing Player",
            "Intermediate"
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.Error);
        result.Errors.Should().Contain("User already joined this game");
    }

    [Fact]
    public async Task Handle_WhenJoiningMakesGameFull_ShouldUpdateStatusToFull()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var game = CreateAlmostFullGame(gameId); // 3/4 players
        
        _gameRepositoryMock.Setup(r => r.GetByIdWithParticipationsAsync(gameId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(game));

        var command = new JoinGameCommand(
            gameId,
            "auth0|player-last",
            "Last Player",
            "Intermediate"
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        game.CurrentPlayers.Should().Be(4);
        game.Status.Should().Be(GameStatus.Full);
    }

    // Helper methods
    private Game CreateTestGame(Guid gameId)
    {
        var gameResult = Game.Create(
            "auth0|host",
            "Host",
            "Intermediate",
            DateTime.UtcNow.AddDays(1),
            "Test Club",
            "Intermediate",
            4
        );

        var game = gameResult.Value;
        typeof(Game).GetProperty("Id")!.SetValue(game, gameId);
        
        return game;
    }

    private Game CreateFullGame(Guid gameId)
    {
        var game = CreateTestGame(gameId);
        game.AddParticipant("auth0|player2", "Player 2", "Intermediate");
        game.AddParticipant("auth0|player3", "Player 3", "Intermediate");
        game.AddParticipant("auth0|player4", "Player 4", "Intermediate");
        
        return game;
    }

    private Game CreateAlmostFullGame(Guid gameId)
    {
        var game = CreateTestGame(gameId);
        game.AddParticipant("auth0|player2", "Player 2", "Intermediate");
        game.AddParticipant("auth0|player3", "Player 3", "Intermediate");
        
        return game;
    }
}
