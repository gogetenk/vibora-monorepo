using Ardalis.Result;
using FluentAssertions;
using Moq;
using Vibora.Games.Application;
using Vibora.Games.Application.Queries.GetShareByToken;
using Vibora.Games.Domain;

namespace Vibora.Games.Tests.Application.Queries;

public class GetShareByTokenQueryHandlerTests
{
    private readonly Mock<IGameShareRepository> _gameShareRepositoryMock;
    private readonly Mock<IGameRepository> _gameRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly GetShareByTokenQueryHandler _handler;

    public GetShareByTokenQueryHandlerTests()
    {
        _gameShareRepositoryMock = new Mock<IGameShareRepository>();
        _gameRepositoryMock = new Mock<IGameRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _handler = new GetShareByTokenQueryHandler(
            _gameShareRepositoryMock.Object,
            _gameRepositoryMock.Object,
            _unitOfWorkMock.Object
        );
    }

    [Fact]
    public async Task Handle_WithValidToken_ShouldReturnShareAndIncrementViewCount()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var gameShare = GameShare.Create(gameId, "auth0|user123").Value;
        var initialViewCount = gameShare.ViewCount;

        var game = Game.Create(
            "auth0|user123",
            "Host Name",
            "5",
            DateTime.UtcNow.AddDays(1),
            "Test Location",
            "5",
            4).Value;

        _gameShareRepositoryMock.Setup(r => r.GetByTokenAsync(gameShare.ShareToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(gameShare);

        _gameRepositoryMock.Setup(r => r.GetByIdWithParticipationsAsync(gameId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Game>.Success(game));

        var query = new GetShareByTokenQuery(gameShare.ShareToken);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.GameId.Should().Be(gameShare.GameId);
        result.Value.ShareToken.Should().Be(gameShare.ShareToken);
        result.Value.ViewCount.Should().Be(initialViewCount + 1);
        result.Value.IsExpired.Should().BeFalse();

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithEmptyToken_ShouldReturnInvalid()
    {
        // Arrange
        var query = new GetShareByTokenQuery("");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.Invalid);
        result.ValidationErrors.Should().Contain(e => e.ErrorMessage.Contains("ShareToken is required"));
    }

    [Fact]
    public async Task Handle_WithNonExistentToken_ShouldReturnNotFound()
    {
        // Arrange
        var token = "invalid123";
        _gameShareRepositoryMock.Setup(r => r.GetByTokenAsync(token, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GameShare?)null);

        var query = new GetShareByTokenQuery(token);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.NotFound);
        result.Errors.Should().Contain(e => e.Contains("not found"));
    }

    [Fact]
    public async Task Handle_WithExpiredShare_ShouldReturnShareButNotIncrementViewCount()
    {
        // Arrange - Create a valid share, then manually set expiration to past (for testing)
        var gameId = Guid.NewGuid();
        var gameShare = GameShare.Create(gameId, "auth0|user123", DateTime.UtcNow.AddDays(1)).Value;
        var expiresAtField = typeof(GameShare).GetField("<ExpiresAt>k__BackingField", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        expiresAtField!.SetValue(gameShare, DateTime.UtcNow.AddDays(-1));

        var initialViewCount = gameShare.ViewCount;

        var game = Game.Create(
            "auth0|user123",
            "Host Name",
            "5",
            DateTime.UtcNow.AddDays(1),
            "Test Location",
            "5",
            4).Value;

        _gameShareRepositoryMock.Setup(r => r.GetByTokenAsync(gameShare.ShareToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(gameShare);

        _gameRepositoryMock.Setup(r => r.GetByIdWithParticipationsAsync(gameId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Game>.Success(game));

        var query = new GetShareByTokenQuery(gameShare.ShareToken);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsExpired.Should().BeTrue();
        result.Value.ViewCount.Should().Be(initialViewCount); // Not incremented

        // SaveChanges should not be called for expired shares
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
