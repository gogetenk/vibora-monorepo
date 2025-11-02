using Ardalis.Result;
using FluentAssertions;
using Moq;
using Vibora.Games.Application;
using Vibora.Games.Application.Commands.CancelGame;
using Vibora.Games.Domain;
using Vibora.Games.Domain.Events;

namespace Vibora.Games.Tests.Application.Commands;

public class CancelGameCommandHandlerTests
{
    private readonly Mock<IGameRepository> _gameRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly CancelGameCommandHandler _handler;

    public CancelGameCommandHandlerTests()
    {
        _gameRepositoryMock = new Mock<IGameRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _handler = new CancelGameCommandHandler(_gameRepositoryMock.Object, _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldCancelGameSuccessfully()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var hostExternalId = "auth0|host";
        var game = CreateTestGame(gameId, hostExternalId);

        _gameRepositoryMock.Setup(r => r.GetByIdAsync(gameId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(game));

        var command = new CancelGameCommand(gameId, hostExternalId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.GameId.Should().Be(gameId);
        result.Value.Message.Should().Contain("canceled successfully");

        game.Status.Should().Be(GameStatus.Canceled);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithEmptyGameId_ShouldReturnInvalid()
    {
        // Arrange
        var command = new CancelGameCommand(Guid.Empty, "auth0|host");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.Invalid);
        result.ValidationErrors.Should().Contain(e => e.ErrorMessage == "GameId cannot be empty");

        _gameRepositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithEmptyHostExternalId_ShouldReturnInvalid()
    {
        // Arrange
        var command = new CancelGameCommand(Guid.NewGuid(), "");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.Invalid);
        result.ValidationErrors.Should().Contain(e => e.ErrorMessage == "HostExternalId is required");
    }

    [Fact]
    public async Task Handle_WithNonExistentGame_ShouldReturnNotFound()
    {
        // Arrange
        var command = new CancelGameCommand(Guid.NewGuid(), "auth0|host");

        _gameRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
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
    public async Task Handle_WhenUserIsNotHost_ShouldReturnForbidden()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var game = CreateTestGame(gameId, "auth0|host");

        _gameRepositoryMock.Setup(r => r.GetByIdAsync(gameId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(game));

        var command = new CancelGameCommand(gameId, "auth0|not-the-host");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.Forbidden);

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenGameAlreadyCanceled_ShouldReturnError()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var hostExternalId = "auth0|host";
        var game = CreateTestGame(gameId, hostExternalId);
        game.Cancel(); // Already canceled

        _gameRepositoryMock.Setup(r => r.GetByIdAsync(gameId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(game));

        var command = new CancelGameCommand(gameId, hostExternalId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.Error);
        result.Errors.Should().Contain(e => e.Contains("already canceled"));

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldRaiseDomainEvent()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var hostExternalId = "auth0|host";
        var game = CreateTestGame(gameId, hostExternalId);
        game.ClearDomainEvents(); // Clear creation event

        _gameRepositoryMock.Setup(r => r.GetByIdAsync(gameId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(game));

        var command = new CancelGameCommand(gameId, hostExternalId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verify domain event was raised
        game.DomainEvents.Should().ContainSingle();
        var domainEvent = game.DomainEvents.Single() as GameCanceledDomainEvent;
        domainEvent.Should().NotBeNull();
        domainEvent!.GameId.Should().Be(gameId);
        domainEvent.HostExternalId.Should().Be(hostExternalId);
    }

    // Helper methods
    private Game CreateTestGame(Guid gameId, string hostExternalId)
    {
        var gameResult = Game.Create(
            hostExternalId,
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
}
