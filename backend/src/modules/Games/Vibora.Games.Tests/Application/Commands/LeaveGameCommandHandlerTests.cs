using Ardalis.Result;
using FluentAssertions;
using Moq;
using Vibora.Games.Application;
using Vibora.Games.Application.Commands.LeaveGame;
using Vibora.Games.Domain;
using Vibora.Games.Domain.Events;

namespace Vibora.Games.Tests.Application.Commands;

public class LeaveGameCommandHandlerTests
{
    private readonly Mock<IGameRepository> _gameRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly LeaveGameCommandHandler _handler;

    public LeaveGameCommandHandlerTests()
    {
        _gameRepositoryMock = new Mock<IGameRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _handler = new LeaveGameCommandHandler(_gameRepositoryMock.Object, _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldLeaveGameSuccessfully()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var game = CreateGameWithParticipant(gameId, "auth0|player-123");
        
        _gameRepositoryMock.Setup(r => r.GetByIdWithParticipationsAsync(gameId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(game));

        var command = new LeaveGameCommand(gameId, "auth0|player-123");

        var initialPlayerCount = game.CurrentPlayers;

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.GameId.Should().Be(gameId);
        result.Value.Message.Should().Contain("Successfully left");
        
        game.CurrentPlayers.Should().Be(initialPlayerCount - 1);
        game.Participations.Should().NotContain(p => p.UserExternalId == "auth0|player-123");
        
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithEmptyGameId_ShouldReturnInvalid()
    {
        // Arrange
        var command = new LeaveGameCommand(Guid.Empty, "auth0|player");

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
        var command = new LeaveGameCommand(Guid.NewGuid(), "");

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
        var command = new LeaveGameCommand(Guid.NewGuid(), "auth0|player");
        
        _gameRepositoryMock.Setup(r => r.GetByIdWithParticipationsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Game>.NotFound("Game not found"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.NotFound);
        result.Errors.Should().Contain(e => e.Contains("not found"));
        
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUserNotInGame_ShouldReturnError()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var game = CreateTestGame(gameId); // Only host, no other participants
        
        _gameRepositoryMock.Setup(r => r.GetByIdWithParticipationsAsync(gameId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(game));

        var command = new LeaveGameCommand(gameId, "auth0|not-a-participant");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.Error);
        result.Errors.Should().Contain(e => e.Contains("not a participant"));
        
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenHostLeaves_ShouldSucceed()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var game = CreateTestGame(gameId);
        
        _gameRepositoryMock.Setup(r => r.GetByIdWithParticipationsAsync(gameId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(game));
        
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new LeaveGameCommand(gameId, "auth0|host"); // Host can now leave

        var initialPlayerCount = game.CurrentPlayers;

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        game.CurrentPlayers.Should().Be(initialPlayerCount - 1);
        
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenGameIsCanceled_ShouldReturnError()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var game = CreateGameWithParticipant(gameId, "auth0|player");
        game.Cancel(); // Cancel the game
        
        _gameRepositoryMock.Setup(r => r.GetByIdWithParticipationsAsync(gameId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(game));

        var command = new LeaveGameCommand(gameId, "auth0|player");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.Error);
        result.Errors.Should().Contain(e => e.Contains("canceled"));
        
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenGameIsInThePast_ShouldReturnError()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        
        // Create game with past date using reflection (bypass validation for unit test)
        var game = (Game)Activator.CreateInstance(typeof(Game), true)!;
        typeof(Game).GetProperty("Id")!.SetValue(game, gameId);
        typeof(Game).GetProperty("HostExternalId")!.SetValue(game, "auth0|host");
        typeof(Game).GetProperty("DateTime")!.SetValue(game, DateTime.UtcNow.AddDays(-1)); // Past date
        typeof(Game).GetProperty("Location")!.SetValue(game, "Test Club");
        typeof(Game).GetProperty("SkillLevel")!.SetValue(game, "Intermediate");
        typeof(Game).GetProperty("MaxPlayers")!.SetValue(game, 4);
        typeof(Game).GetProperty("CurrentPlayers")!.SetValue(game, 2);
        typeof(Game).GetProperty("Status")!.SetValue(game, GameStatus.Open);
        typeof(Game).GetProperty("CreatedAt")!.SetValue(game, DateTime.UtcNow);
        
        // Create host participation
        var hostParticipation = (Participation)Activator.CreateInstance(typeof(Participation), true)!;
        typeof(Participation).GetProperty("Id")!.SetValue(hostParticipation, Guid.NewGuid());
        typeof(Participation).GetProperty("GameId")!.SetValue(hostParticipation, gameId);
        typeof(Participation).GetProperty("UserExternalId")!.SetValue(hostParticipation, "auth0|host");
        typeof(Participation).GetProperty("UserName")!.SetValue(hostParticipation, "Host");
        typeof(Participation).GetProperty("UserSkillLevel")!.SetValue(hostParticipation, "Intermediate");
        typeof(Participation).GetProperty("IsHost")!.SetValue(hostParticipation, true);
        typeof(Participation).GetProperty("JoinedAt")!.SetValue(hostParticipation, DateTime.UtcNow);
        
        // Create player participation
        var playerParticipation = (Participation)Activator.CreateInstance(typeof(Participation), true)!;
        typeof(Participation).GetProperty("Id")!.SetValue(playerParticipation, Guid.NewGuid());
        typeof(Participation).GetProperty("GameId")!.SetValue(playerParticipation, gameId);
        typeof(Participation).GetProperty("UserExternalId")!.SetValue(playerParticipation, "auth0|player");
        typeof(Participation).GetProperty("UserName")!.SetValue(playerParticipation, "Player");
        typeof(Participation).GetProperty("UserSkillLevel")!.SetValue(playerParticipation, "Intermediate");
        typeof(Participation).GetProperty("IsHost")!.SetValue(playerParticipation, false);
        typeof(Participation).GetProperty("JoinedAt")!.SetValue(playerParticipation, DateTime.UtcNow);
        
        // Add participations
        var participationsField = typeof(Game).GetField("_participations", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var participations = (List<Participation>)participationsField.GetValue(game)!;
        participations.Add(hostParticipation);
        participations.Add(playerParticipation);
        
        _gameRepositoryMock.Setup(r => r.GetByIdWithParticipationsAsync(gameId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(game));

        var command = new LeaveGameCommand(gameId, "auth0|player");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.Error);
        result.Errors.Should().Contain(e => e.Contains("already started") || e.Contains("in the past"));
        
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenLeavingMakesGameOpen_ShouldUpdateStatusToOpen()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var game = CreateFullGame(gameId); // 4/4 players (Full)
        
        _gameRepositoryMock.Setup(r => r.GetByIdWithParticipationsAsync(gameId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(game));

        var command = new LeaveGameCommand(gameId, "auth0|player2");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        game.CurrentPlayers.Should().Be(3);
        game.Status.Should().Be(GameStatus.Open); // Changed from Full to Open
    }

    [Fact]
    public async Task Handle_WhenPlayerLeaves_ShouldRaiseDomainEvent()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var game = CreateGameWithParticipant(gameId, "auth0|player-123");
        
        // Clear domain events from game creation to focus on RemoveParticipant event
        game.ClearDomainEvents();
        
        _gameRepositoryMock.Setup(r => r.GetByIdWithParticipationsAsync(gameId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(game));

        var command = new LeaveGameCommand(gameId, "auth0|player-123");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        
        // Verify domain event was raised
        game.DomainEvents.Should().ContainSingle();
        var domainEvent = game.DomainEvents.Single() as ParticipationRemovedDomainEvent;
        domainEvent.Should().NotBeNull();
        domainEvent!.GameId.Should().Be(gameId);
        domainEvent.UserExternalId.Should().Be("auth0|player-123");
        domainEvent.UserName.Should().Be("Participant");
        domainEvent.RemainingPlayers.Should().Be(1); // Only host left
        domainEvent.GameStatus.Should().Be(GameStatus.Open);
        
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
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

    private Game CreateGameWithParticipant(Guid gameId, string participantExternalId)
    {
        var game = CreateTestGame(gameId);
        game.AddParticipant(participantExternalId, "Participant", "Intermediate");
        
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
}
