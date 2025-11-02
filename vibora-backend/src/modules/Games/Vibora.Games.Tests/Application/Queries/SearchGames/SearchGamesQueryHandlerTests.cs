using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Vibora.Games.Application.Queries.SearchGames;
using Vibora.Games.Domain;
using Vibora.Games.Infrastructure.Data;

namespace Vibora.Games.Tests.Application.Queries.SearchGames;

public class SearchGamesQueryHandlerTests
{
    private readonly Mock<GamesDbContext> _mockContext;
    private readonly Mock<DbSet<Game>> _mockGamesSet;
    private readonly SearchGamesQueryHandler _handler;

    public SearchGamesQueryHandlerTests()
    {
        var options = new DbContextOptionsBuilder<GamesDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        // Create a real context for testing (InMemoryDatabase is appropriate for integration-like unit tests)
        _mockContext = new Mock<GamesDbContext>(options);
        _mockGamesSet = new Mock<DbSet<Game>>();
        _handler = new SearchGamesQueryHandler(_mockContext.Object);
    }

    [Fact]
    public async Task Handle_WithGpsCoordinates_ShouldCalculateDistances()
    {
        // Arrange: Create in-memory context with real database
        var options = new DbContextOptionsBuilder<GamesDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new GamesDbContext(options);
        var handler = new SearchGamesQueryHandler(context);

        var userLat = 48.8566;
        var userLng = 2.3522;
        var targetDateTime = DateTime.UtcNow.AddHours(3);

        // Game 1: ~5km away (Casa Padel Paris 15)
        var game1 = CreateTestGame("Casa Padel Paris 15", targetDateTime, 48.8424, 2.2894, "5");
        context.Games.Add(game1);

        // Game 2: ~2km away (closer, should be first)
        var game2 = CreateTestGame("Padel Club Marais", targetDateTime, 48.8606, 2.3635, "5");
        context.Games.Add(game2);

        await context.SaveChangesAsync();

        var query = new SearchGamesQuery(
            When: targetDateTime,
            Level: 5,
            Latitude: userLat,
            Longitude: userLng,
            RadiusKm: 10
        );

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PerfectMatches.Should().HaveCount(2);

        // Games should have distance calculated
        result.Value.PerfectMatches.Should().OnlyContain(g => g.DistanceKm.HasValue);

        // First result should be closer game (~2km)
        result.Value.PerfectMatches[0].Id.Should().Be(game2.Id);
        result.Value.PerfectMatches[0].DistanceKm.Should().BeApproximately(2.0, 1.5);

        // Second result should be farther game (~5km)
        result.Value.PerfectMatches[1].Id.Should().Be(game1.Id);
        result.Value.PerfectMatches[1].DistanceKm.Should().BeApproximately(5.0, 1.5);
    }

    [Fact]
    public async Task Handle_WithRadiusFilter_ShouldExcludeGamesOutsideRadius()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<GamesDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new GamesDbContext(options);
        var handler = new SearchGamesQueryHandler(context);

        var userLat = 48.8566;
        var userLng = 2.3522;
        var targetDateTime = DateTime.UtcNow.AddHours(3);

        // Game inside radius (~2km)
        var nearGame = CreateTestGame("Near Club", targetDateTime, 48.8606, 2.3635, "5");
        context.Games.Add(nearGame);

        // Game outside radius (~20km away - Versailles)
        var farGame = CreateTestGame("Far Club", targetDateTime, 48.8049, 2.1204, "5");
        context.Games.Add(farGame);

        await context.SaveChangesAsync();

        var query = new SearchGamesQuery(
            When: targetDateTime,
            Level: 5,
            Latitude: userLat,
            Longitude: userLng,
            RadiusKm: 5 // Only 5km radius
        );

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var allMatches = result.Value.PerfectMatches.Concat(result.Value.PartialMatches).ToList();
        allMatches.Should().Contain(g => g.Id == nearGame.Id, "near game should be included");
        allMatches.Should().NotContain(g => g.Id == farGame.Id, "far game should be excluded by radius");
    }

    [Fact]
    public async Task Handle_PerfectMatch_WithGpsCoordinates_ShouldHaveScore4()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<GamesDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new GamesDbContext(options);
        var handler = new SearchGamesQueryHandler(context);

        var userLat = 48.8566;
        var userLng = 2.3522;
        var targetDateTime = DateTime.UtcNow.AddHours(1);

        var game = CreateTestGame("Test Club", targetDateTime, 48.8606, 2.3635, "5");
        context.Games.Add(game);
        await context.SaveChangesAsync();

        var query = new SearchGamesQuery(
            When: targetDateTime,
            Level: 5,
            Latitude: userLat,
            Longitude: userLng,
            RadiusKm: 10
        );

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PerfectMatches.Should().HaveCount(1);
        result.Value.PerfectMatches[0].MatchScore.Should().Be(4); // Perfect = score 4
        result.Value.PerfectMatches[0].DistanceKm.Should().NotBeNull();
        result.Value.PerfectMatches[0].DistanceKm.Should().BeLessThan(3);
    }

    [Fact]
    public async Task Handle_PartialMatch_WhenLevelMismatch()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<GamesDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new GamesDbContext(options);
        var handler = new SearchGamesQueryHandler(context);

        var userLat = 48.8566;
        var userLng = 2.3522;
        var targetDateTime = DateTime.UtcNow.AddHours(1);

        // Game with level difference > 1 (should be partial, not perfect)
        var game = CreateTestGame("Test Club", targetDateTime, 48.8606, 2.3635, "8");
        context.Games.Add(game);
        await context.SaveChangesAsync();

        var query = new SearchGamesQuery(
            When: targetDateTime,
            Level: 5,
            Latitude: userLat,
            Longitude: userLng,
            RadiusKm: 10
        );

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PerfectMatches.Should().BeEmpty(); // Not perfect due to level mismatch
        result.Value.PartialMatches.Should().HaveCount(1);
        result.Value.PartialMatches[0].MatchScore.Should().BeGreaterThan(0);
        result.Value.PartialMatches[0].DistanceKm.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_PartialMatch_WhenTimeMismatch()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<GamesDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new GamesDbContext(options);
        var handler = new SearchGamesQueryHandler(context);

        var userLat = 48.8566;
        var userLng = 2.3522;
        var targetDateTime = DateTime.UtcNow.AddHours(1);

        // Game with time difference > 2h but <= 4h (partial match)
        var game = CreateTestGame("Test Club", targetDateTime.AddHours(3), 48.8606, 2.3635, "5");
        context.Games.Add(game);
        await context.SaveChangesAsync();

        var query = new SearchGamesQuery(
            When: targetDateTime,
            Level: 5,
            Latitude: userLat,
            Longitude: userLng,
            RadiusKm: 10
        );

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PerfectMatches.Should().BeEmpty(); // Not perfect due to time mismatch
        result.Value.PartialMatches.Should().HaveCount(1);
        result.Value.PartialMatches[0].MatchScore.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Handle_WithoutGpsCoordinates_ShouldNotCalculateDistance()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<GamesDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new GamesDbContext(options);
        var handler = new SearchGamesQueryHandler(context);

        var targetDateTime = DateTime.UtcNow.AddHours(3);

        // Game without GPS coordinates
        var game = CreateTestGame("Casa Padel Paris", targetDateTime, null, null, "5");
        context.Games.Add(game);
        await context.SaveChangesAsync();

        var query = new SearchGamesQuery(
            When: targetDateTime,
            Where: "Casa Padel",
            Level: 5
        );

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var allMatches = result.Value.PerfectMatches.Concat(result.Value.PartialMatches).ToList();
        allMatches.Should().NotBeEmpty();
        allMatches.Should().Contain(g => g.Location.Contains("Casa Padel"));
        allMatches.First(g => g.Location.Contains("Casa Padel")).DistanceKm.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldCalculateHaversineDistanceCorrectly()
    {
        // Arrange: Known distance between two Paris locations
        // Paris center: 48.8566, 2.3522
        // Eiffel Tower: 48.8584, 2.2945
        // Expected distance: ~4.5 km

        var options = new DbContextOptionsBuilder<GamesDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new GamesDbContext(options);
        var handler = new SearchGamesQueryHandler(context);

        var userLat = 48.8566;
        var userLng = 2.3522;
        var targetDateTime = DateTime.UtcNow.AddHours(1);

        var game = CreateTestGame("Near Eiffel Tower", targetDateTime, 48.8584, 2.2945, "5");
        context.Games.Add(game);
        await context.SaveChangesAsync();

        var query = new SearchGamesQuery(
            When: targetDateTime,
            Level: 5,
            Latitude: userLat,
            Longitude: userLng,
            RadiusKm: 10
        );

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var matchedGame = result.Value.PerfectMatches.Concat(result.Value.PartialMatches)
            .FirstOrDefault(g => g.Id == game.Id);

        matchedGame.Should().NotBeNull();
        matchedGame!.DistanceKm.Should().NotBeNull();
        matchedGame.DistanceKm.Should().BeApproximately(4.5, 1.0); // ~4.5km with 1km tolerance
    }

    [Fact]
    public async Task Handle_WithGpsCoordinates_ShouldPrioritizeCloserGames()
    {
        // Arrange: Create 3 games at different distances but all perfect matches
        var options = new DbContextOptionsBuilder<GamesDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new GamesDbContext(options);
        var handler = new SearchGamesQueryHandler(context);

        var userLat = 48.8566;
        var userLng = 2.3522;
        var targetDateTime = DateTime.UtcNow.AddHours(1);

        var farGame = CreateTestGame("Far Club", targetDateTime, 48.8424, 2.2894, "5"); // ~5km
        var closeGame = CreateTestGame("Close Club", targetDateTime, 48.8606, 2.3635, "5"); // ~2km
        var mediumGame = CreateTestGame("Medium Club", targetDateTime, 48.8550, 2.3300, "5"); // ~3km

        context.Games.AddRange(farGame, closeGame, mediumGame);
        await context.SaveChangesAsync();

        var query = new SearchGamesQuery(
            When: targetDateTime,
            Level: 5,
            Latitude: userLat,
            Longitude: userLng,
            RadiusKm: 10
        );

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PerfectMatches.Should().HaveCount(3);

        // Should be sorted by distance (closest first)
        result.Value.PerfectMatches[0].Id.Should().Be(closeGame.Id);
        result.Value.PerfectMatches[1].Id.Should().Be(mediumGame.Id);
        result.Value.PerfectMatches[2].Id.Should().Be(farGame.Id);
    }

    // Helper method to create test game
    private Game CreateTestGame(
        string location,
        DateTime dateTime,
        double? latitude,
        double? longitude,
        string skillLevel)
    {
        var gameResult = Game.Create(
            hostExternalId: "test-host",
            hostName: "Test Host",
            hostSkillLevel: skillLevel,
            dateTime: dateTime,
            location: location,
            skillLevel: skillLevel,
            maxPlayers: 4,
            latitude: latitude,
            longitude: longitude
        );

        if (!gameResult.IsSuccess)
        {
            throw new InvalidOperationException($"Failed to create test game: {string.Join(", ", gameResult.ValidationErrors.Select(e => e.ErrorMessage))}");
        }

        return gameResult.Value;
    }
}
