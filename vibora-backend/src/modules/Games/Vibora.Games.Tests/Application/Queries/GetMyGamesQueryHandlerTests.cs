using Ardalis.Result;
using FluentAssertions;
using Moq;
using Vibora.Games.Application.Queries.GetMyGames;
using Vibora.Games.Domain;

namespace Vibora.Games.Tests.Application.Queries;

public class GetMyGamesQueryHandlerTests
{
    private readonly Mock<IGameRepository> _gameRepositoryMock;
    private readonly GetMyGamesQueryHandler _handler;

    public GetMyGamesQueryHandlerTests()
    {
        _gameRepositoryMock = new Mock<IGameRepository>();
        _handler = new GetMyGamesQueryHandler(_gameRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidUserExternalId_ShouldReturnUpcomingGames()
    {
        // Arrange
        var userExternalId = "auth0|user-123";
        var games = new List<Game>
        {
            CreateTestGame(DateTime.UtcNow.AddDays(1), userExternalId, true), // Host, future
            CreateTestGame(DateTime.UtcNow.AddDays(2), userExternalId, false), // Participant, future
            CreateTestGame(DateTime.UtcNow.AddDays(-1), userExternalId, false) // Past game (should be filtered)
        };

        _gameRepositoryMock.Setup(r => r.GetGamesByUserAsync(userExternalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(games);

        var query = new GetMyGamesQuery(userExternalId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Games.Should().HaveCount(2); // Only future games
        result.Value.TotalCount.Should().Be(2);
        
        var firstGame = result.Value.Games[0];
        firstGame.IsHost.Should().BeTrue();
        
        var secondGame = result.Value.Games[1];
        secondGame.IsHost.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithEmptyUserExternalId_ShouldReturnInvalid()
    {
        // Arrange
        var query = new GetMyGamesQuery("");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.Invalid);
        result.ValidationErrors.Should().Contain(e => e.ErrorMessage == "UserExternalId is required");
    }

    [Fact]
    public async Task Handle_WithNoGames_ShouldReturnEmptyList()
    {
        // Arrange
        var userExternalId = "auth0|user-123";
        _gameRepositoryMock.Setup(r => r.GetGamesByUserAsync(userExternalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Game>());

        var query = new GetMyGamesQuery(userExternalId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Games.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WithOnlyPastGames_ShouldReturnEmptyList()
    {
        // Arrange
        var userExternalId = "auth0|user-123";
        var pastGames = new List<Game>
        {
            CreateTestGame(DateTime.UtcNow.AddDays(-1), userExternalId, true),
            CreateTestGame(DateTime.UtcNow.AddHours(-2), userExternalId, false)
        };

        _gameRepositoryMock.Setup(r => r.GetGamesByUserAsync(userExternalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pastGames);

        var query = new GetMyGamesQuery(userExternalId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Games.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ShouldOrderGamesByDateTime()
    {
        // Arrange - Repository already returns ordered by DateTime DESC
        var userExternalId = "auth0|user-123";
        var games = new List<Game>
        {
            CreateTestGame(DateTime.UtcNow.AddDays(3), userExternalId, true),
            CreateTestGame(DateTime.UtcNow.AddDays(1), userExternalId, false),
            CreateTestGame(DateTime.UtcNow.AddDays(2), userExternalId, false)
        };

        _gameRepositoryMock.Setup(r => r.GetGamesByUserAsync(userExternalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(games);

        var query = new GetMyGamesQuery(userExternalId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Games.Should().HaveCount(3);
        // Verify they are returned in the order from repository
        result.Value.Games[0].DateTime.Should().Be(games[0].DateTime);
        result.Value.Games[1].DateTime.Should().Be(games[1].DateTime);
        result.Value.Games[2].DateTime.Should().Be(games[2].DateTime);
    }

    // Helper method
    private Game CreateTestGame(DateTime dateTime, string userExternalId, bool isHost)
    {
        var hostId = isHost ? userExternalId : "auth0|other-host";
        
        // For past dates in tests, bypass validation by using reflection to create the game
        // This is acceptable in unit tests to test business logic around past games
        var game = (Game)Activator.CreateInstance(typeof(Game), true)!;
        
        // Set properties via reflection
        typeof(Game).GetProperty("Id")!.SetValue(game, Guid.NewGuid());
        typeof(Game).GetProperty("HostExternalId")!.SetValue(game, hostId);
        typeof(Game).GetProperty("DateTime")!.SetValue(game, dateTime);
        typeof(Game).GetProperty("Location")!.SetValue(game, "Test Club");
        typeof(Game).GetProperty("SkillLevel")!.SetValue(game, "Intermediate");
        typeof(Game).GetProperty("MaxPlayers")!.SetValue(game, 4);
        typeof(Game).GetProperty("CurrentPlayers")!.SetValue(game, 1); // Host counts as 1
        typeof(Game).GetProperty("Status")!.SetValue(game, GameStatus.Open);
        typeof(Game).GetProperty("CreatedAt")!.SetValue(game, DateTime.UtcNow);
        
        // Create host participation
        var hostParticipation = (Participation)Activator.CreateInstance(typeof(Participation), true)!;
        typeof(Participation).GetProperty("Id")!.SetValue(hostParticipation, Guid.NewGuid());
        typeof(Participation).GetProperty("GameId")!.SetValue(hostParticipation, game.Id);
        typeof(Participation).GetProperty("UserExternalId")!.SetValue(hostParticipation, hostId);
        typeof(Participation).GetProperty("UserName")!.SetValue(hostParticipation, "Host Name");
        typeof(Participation).GetProperty("UserSkillLevel")!.SetValue(hostParticipation, "Intermediate");
        typeof(Participation).GetProperty("IsHost")!.SetValue(hostParticipation, true);
        typeof(Participation).GetProperty("JoinedAt")!.SetValue(hostParticipation, DateTime.UtcNow);
        
        // Add host to participations via reflection
        var participationsField = typeof(Game).GetField("_participations", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var participations = (List<Participation>)participationsField.GetValue(game)!;
        participations.Add(hostParticipation);

        // Add user as participant if not host
        if (!isHost)
        {
            game.AddParticipant(userExternalId, "User Name", "Intermediate");
        }

        return game;
    }
}
