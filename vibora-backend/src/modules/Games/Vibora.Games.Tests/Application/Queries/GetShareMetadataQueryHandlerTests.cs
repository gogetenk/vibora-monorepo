using Ardalis.Result;
using FluentAssertions;
using Moq;
using Vibora.Games.Application.Queries.GetShareMetadata;
using Vibora.Games.Domain;

namespace Vibora.Games.Tests.Application.Queries;

public class GetShareMetadataQueryHandlerTests
{
    private readonly Mock<IGameShareRepository> _gameShareRepositoryMock;
    private readonly GetShareMetadataQueryHandler _handler;

    public GetShareMetadataQueryHandlerTests()
    {
        _gameShareRepositoryMock = new Mock<IGameShareRepository>();
        _handler = new GetShareMetadataQueryHandler(_gameShareRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidToken_ShouldReturnMetadata()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var game = CreateTestGame(gameId);
        var gameShare = GameShare.Create(gameId, "auth0|user123").Value;
        
        // Set the Game navigation property via reflection
        typeof(GameShare).GetProperty("Game")!.SetValue(gameShare, game);

        _gameShareRepositoryMock.Setup(r => r.GetByTokenAsync(gameShare.ShareToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(gameShare);

        var query = new GetShareMetadataQuery(gameShare.ShareToken);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Contain("Test Club");
        result.Value.Description.Should().Contain("Intermediate");
        result.Value.Location.Should().Be(game.Location);
        result.Value.SkillLevel.Should().Be(game.SkillLevel);
        result.Value.CurrentPlayers.Should().Be(game.CurrentPlayers);
        result.Value.MaxPlayers.Should().Be(game.MaxPlayers);
    }

    [Fact]
    public async Task Handle_WithEmptyToken_ShouldReturnInvalid()
    {
        // Arrange
        var query = new GetShareMetadataQuery("");

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

        var query = new GetShareMetadataQuery(token);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.NotFound);
        result.Errors.Should().Contain(e => e.Contains("not found"));
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
