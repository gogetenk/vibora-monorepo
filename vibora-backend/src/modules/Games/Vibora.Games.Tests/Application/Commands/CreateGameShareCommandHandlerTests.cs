using Ardalis.Result;
using FluentAssertions;
using Moq;
using Vibora.Games.Application;
using Vibora.Games.Application.Commands.CreateGameShare;
using Vibora.Games.Domain;
using Vibora.Games.Domain.Events;

namespace Vibora.Games.Tests.Application.Commands;

public class CreateGameShareCommandHandlerTests
{
    private readonly Mock<IGameRepository> _gameRepositoryMock;
    private readonly Mock<IGameShareRepository> _gameShareRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly CreateGameShareCommandHandler _handler;

    public CreateGameShareCommandHandlerTests()
    {
        _gameRepositoryMock = new Mock<IGameRepository>();
        _gameShareRepositoryMock = new Mock<IGameShareRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _handler = new CreateGameShareCommandHandler(
            _gameRepositoryMock.Object,
            _gameShareRepositoryMock.Object,
            _unitOfWorkMock.Object
        );
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldCreateShareSuccessfully()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var sharedByUserExternalId = "auth0|user123";
        var game = CreateTestGame(gameId);

        _gameRepositoryMock.Setup(r => r.GetByIdAsync(gameId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(game));

        var command = new CreateGameShareCommand(gameId, sharedByUserExternalId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ShareToken.Should().NotBeNullOrEmpty();
        result.Value.ShareUrl.Should().Contain(result.Value.ShareToken);

        _gameShareRepositoryMock.Verify(
            r => r.AddAsync(It.IsAny<GameShare>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithEmptyGameId_ShouldReturnInvalid()
    {
        // Arrange
        var command = new CreateGameShareCommand(Guid.Empty, "auth0|user123");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.Invalid);
        result.ValidationErrors.Should().Contain(e => e.ErrorMessage.Contains("GameId cannot be empty"));

        _gameRepositoryMock.Verify(
            r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_WithEmptySharedByUserExternalId_ShouldReturnInvalid()
    {
        // Arrange
        var command = new CreateGameShareCommand(Guid.NewGuid(), "");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.Invalid);
        result.ValidationErrors.Should().Contain(e => e.ErrorMessage.Contains("SharedByUserExternalId is required"));
    }

    [Fact]
    public async Task Handle_WithNonExistentGame_ShouldReturnNotFound()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var command = new CreateGameShareCommand(gameId, "auth0|user123");

        _gameRepositoryMock.Setup(r => r.GetByIdAsync(gameId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Game>.NotFound($"Game with ID '{gameId}' not found"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.NotFound);
        result.Errors.Should().Contain(e => e.Contains("not found"));

        _gameShareRepositoryMock.Verify(
            r => r.AddAsync(It.IsAny<GameShare>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_ShouldRaiseDomainEvent()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var sharedByUserExternalId = "auth0|user123";
        var game = CreateTestGame(gameId);

        _gameRepositoryMock.Setup(r => r.GetByIdAsync(gameId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(game));

        GameShare? capturedGameShare = null;
        _gameShareRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<GameShare>(), It.IsAny<CancellationToken>()))
            .Callback<GameShare, CancellationToken>((share, ct) => capturedGameShare = share);

        var command = new CreateGameShareCommand(gameId, sharedByUserExternalId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedGameShare.Should().NotBeNull();
        capturedGameShare!.DomainEvents.Should().ContainSingle();

        var domainEvent = capturedGameShare.DomainEvents.Single() as GameSharedDomainEvent;
        domainEvent.Should().NotBeNull();
        domainEvent!.GameId.Should().Be(gameId);
        domainEvent.SharedByUserExternalId.Should().Be(sharedByUserExternalId);
    }

    [Fact]
    public async Task Handle_WithExpirationDate_ShouldCreateShareWithExpiration()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var sharedByUserExternalId = "auth0|user123";
        var expiresAt = DateTime.UtcNow.AddDays(7);
        var game = CreateTestGame(gameId);

        _gameRepositoryMock.Setup(r => r.GetByIdAsync(gameId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(game));

        GameShare? capturedGameShare = null;
        _gameShareRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<GameShare>(), It.IsAny<CancellationToken>()))
            .Callback<GameShare, CancellationToken>((share, ct) => capturedGameShare = share);

        var command = new CreateGameShareCommand(gameId, sharedByUserExternalId, expiresAt);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedGameShare.Should().NotBeNull();
        capturedGameShare!.ExpiresAt.Should().Be(expiresAt);
    }

    // Helper method
    private Game CreateTestGame(Guid gameId)
    {
        var gameResult = Game.Create(
            "auth0|host",
            "Host Name",
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
