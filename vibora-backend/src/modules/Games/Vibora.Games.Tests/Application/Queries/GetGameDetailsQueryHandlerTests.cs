using Ardalis.Result;
using FluentAssertions;
using Moq;
using Vibora.Games.Application.Queries.GetGameDetails;
using Vibora.Games.Domain;

namespace Vibora.Games.Tests.Application.Queries;

public class GetGameDetailsQueryHandlerTests
{
    private readonly Mock<IGameRepository> _gameRepositoryMock;
    private readonly GetGameDetailsQueryHandler _handler;

    public GetGameDetailsQueryHandlerTests()
    {
        _gameRepositoryMock = new Mock<IGameRepository>();
        _handler = new GetGameDetailsQueryHandler(_gameRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidGameId_ShouldReturnGameDetails()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var game = CreateTestGame(gameId);
        
        _gameRepositoryMock.Setup(r => r.GetByIdWithParticipationsAsync(gameId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(game));

        var query = new GetGameDetailsQuery(gameId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(gameId);
        result.Value.Location.Should().Be("Test Club");
        result.Value.Participants.Should().HaveCount(2);
        
        // Verify host is first
        result.Value.Participants[0].IsHost.Should().BeTrue();
        result.Value.Participants[0].Type.Should().Be("User");
        result.Value.Participants[0].Identifier.Should().Be("auth0|host");
        result.Value.Participants[0].DisplayName.Should().Be("Host");
        
        // Verify regular participant is second
        result.Value.Participants[1].IsHost.Should().BeFalse();
        result.Value.Participants[1].Type.Should().Be("User");
        result.Value.Participants[1].Identifier.Should().Be("auth0|player");
        result.Value.Participants[1].DisplayName.Should().Be("Player");
    }

    [Fact]
    public async Task Handle_WithEmptyGameId_ShouldReturnInvalid()
    {
        // Arrange
        var query = new GetGameDetailsQuery(Guid.Empty);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.Invalid);
        result.ValidationErrors.Should().Contain(e => e.ErrorMessage == "GameId cannot be empty");
        
        // Repository should not be called
        _gameRepositoryMock.Verify(r => r.GetByIdWithParticipationsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithNonExistentGame_ShouldReturnNotFound()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        _gameRepositoryMock.Setup(r => r.GetByIdWithParticipationsAsync(gameId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Game>.NotFound($"Game with ID '{gameId}' not found"));

        var query = new GetGameDetailsQuery(gameId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.NotFound);
        result.Errors.Should().Contain($"Game with ID '{gameId}' not found");
    }

    [Fact]
    public async Task Handle_ShouldOrderParticipants_HostFirstThenByJoinDate()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var game = CreateGameWithMultipleParticipants(gameId);
        
        _gameRepositoryMock.Setup(r => r.GetByIdWithParticipationsAsync(gameId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(game));

        var query = new GetGameDetailsQuery(gameId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Participants.Should().HaveCount(3);
        
        // First should be host
        result.Value.Participants[0].IsHost.Should().BeTrue();
        result.Value.Participants[0].DisplayName.Should().Be("Host");
        
        // Others ordered by JoinedAt
        result.Value.Participants[1].DisplayName.Should().Be("Player 2");
        result.Value.Participants[2].DisplayName.Should().Be("Player 3");
    }

    [Fact]
    public async Task Handle_ShouldMapAllGameProperties()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var game = CreateTestGame(gameId);
        
        _gameRepositoryMock.Setup(r => r.GetByIdWithParticipationsAsync(gameId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(game));

        var query = new GetGameDetailsQuery(gameId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Value.Id.Should().Be(gameId);
        result.Value.Location.Should().Be("Test Club");
        result.Value.SkillLevel.Should().Be("Intermediate");
        result.Value.MaxPlayers.Should().Be(4);
        result.Value.CurrentPlayers.Should().Be(2);
        result.Value.HostExternalId.Should().Be("auth0|host");
        result.Value.Status.Should().Be("Open");
    }

    [Fact]
    public async Task Handle_WithMixedParticipants_ShouldReturnBothUsersAndGuests()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var game = CreateGameWithGuestParticipants(gameId);
        
        _gameRepositoryMock.Setup(r => r.GetByIdWithParticipationsAsync(gameId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(game));

        var query = new GetGameDetailsQuery(gameId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Participants.Should().HaveCount(4); // Host + 1 user + 2 guests
        
        // Verify types
        result.Value.Participants.Should().Contain(p => p.Type == "User");
        result.Value.Participants.Should().Contain(p => p.Type == "Guest");
        
        // Verify guests have correct properties
        var guestParticipants = result.Value.Participants.Where(p => p.Type == "Guest").ToList();
        guestParticipants.Should().HaveCount(2);
        guestParticipants.Should().AllSatisfy(g =>
        {
            g.ParticipationId.Should().BeNull();
            g.ContactInfo.Should().NotBeNullOrWhiteSpace();
            g.SkillLevel.Should().BeNull();
        });
    }

    [Fact]
    public async Task Handle_WithGuestsOnly_ShouldReturnGuestsAfterHost()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var game = CreateGameWithGuestsOnly(gameId);
        
        _gameRepositoryMock.Setup(r => r.GetByIdWithParticipationsAsync(gameId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(game));

        var query = new GetGameDetailsQuery(gameId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Participants.Should().HaveCount(3); // Host + 2 guests
        
        // Host should be first
        result.Value.Participants[0].IsHost.Should().BeTrue();
        result.Value.Participants[0].Type.Should().Be("User");
        
        // Guests should follow, sorted by JoinedAt
        result.Value.Participants[1].Type.Should().Be("Guest");
        result.Value.Participants[2].Type.Should().Be("Guest");
    }

    [Fact]
    public async Task Handle_GuestParticipants_ShouldHaveCorrectIdentifierFormat()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var game = CreateGameWithGuestParticipants(gameId);
        
        _gameRepositoryMock.Setup(r => r.GetByIdWithParticipationsAsync(gameId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(game));

        var query = new GetGameDetailsQuery(gameId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        var guestParticipants = result.Value.Participants.Where(p => p.Type == "Guest").ToList();
        
        // Identifier should be "Guest: {Name}"
        guestParticipants[0].Identifier.Should().StartWith("Guest: ");
        guestParticipants[0].Identifier.Should().Be("Guest: John Doe");
        
        // DisplayName should be just the name (no prefix)
        guestParticipants[0].DisplayName.Should().Be("John Doe");
        guestParticipants[0].DisplayName.Should().NotStartWith("Guest: ");
    }

    [Fact]
    public async Task Handle_ShouldOrderAllParticipants_HostFirstThenChronological()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var game = CreateGameWithMixedJoinTimes(gameId);
        
        _gameRepositoryMock.Setup(r => r.GetByIdWithParticipationsAsync(gameId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(game));

        var query = new GetGameDetailsQuery(gameId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        
        // First should always be host
        result.Value.Participants[0].IsHost.Should().BeTrue();
        
        // Verify chronological order for non-hosts (guest joined before participant)
        result.Value.Participants[1].Type.Should().Be("Guest");
        result.Value.Participants[2].Type.Should().Be("User");
        result.Value.Participants[2].IsHost.Should().BeFalse();
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
        
        // Add a participant (not host)
        game.AddParticipant("auth0|player", "Player", "Intermediate");
        
        // Set ID using reflection
        typeof(Game).GetProperty("Id")!.SetValue(game, gameId);
        
        return game;
    }

    private Game CreateGameWithMultipleParticipants(Guid gameId)
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
        
        // Add participants with different join times (simulated)
        game.AddParticipant("auth0|player2", "Player 2", "Intermediate");
        game.AddParticipant("auth0|player3", "Player 3", "Intermediate");
        
        typeof(Game).GetProperty("Id")!.SetValue(game, gameId);
        
        return game;
    }

    private Game CreateGameWithGuestParticipants(Guid gameId)
    {
        var game = CreateTestGame(gameId);
        
        // Add guest participants using domain method
        game.AddGuestParticipant("John Doe", "+33612345678", null);
        game.AddGuestParticipant("Jane Smith", null, "jane@example.com");
        
        return game;
    }

    private Game CreateGameWithGuestsOnly(Guid gameId)
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
        
        // Add only guest participants (no regular users)
        game.AddGuestParticipant("Guest One", "+33611111111", null);
        game.AddGuestParticipant("Guest Two", "+33622222222", null);
        
        typeof(Game).GetProperty("Id")!.SetValue(game, gameId);
        
        return game;
    }

    private Game CreateGameWithMixedJoinTimes(Guid gameId)
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
        
        // Add guest first (earlier join time)
        game.AddGuestParticipant("Early Guest", "+33611111111", null);
        
        // Simulate time passing with a small delay
        System.Threading.Thread.Sleep(10);
        
        // Add regular participant later
        game.AddParticipant("auth0|laterPlayer", "Later Player", "Intermediate");
        
        typeof(Game).GetProperty("Id")!.SetValue(game, gameId);
        
        return game;
    }
}
