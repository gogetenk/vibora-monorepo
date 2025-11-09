using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Vibora.Games.Application.Queries.SearchGames;
using Vibora.Integration.Tests.Infrastructure;
using Xunit;

namespace Vibora.Integration.Tests.Games;

[Collection("Integration")]
public class SearchGamesWithGpsIntegrationTests : IntegrationTestBaseImproved
{
    public SearchGamesWithGpsIntegrationTests(ViboraWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GET_games_search_WithGpsCoordinates_Returns200WithDistances()
    {
        // Arrange: Create test game with GPS coordinates (Casa Padel Paris 15)
        var host = await Seeder.SeedUserAsync(u => u
            .WithExternalId("auth0|gps-host-1")
            .WithName("GPS Host")
            .Intermediate());

        var gameDateTime = DateTime.UtcNow.AddHours(3);
        var createRequest = new
        {
            dateTime = gameDateTime,
            location = "Casa Padel Paris 15",
            skillLevel = 5,
            maxPlayers = 4,
            latitude = 48.8424,
            longitude = 2.2894
        };

        AuthenticateAs(host.ExternalId);
        var createResponse = await Client.PostAsJsonAsync("/games", createRequest);
        createResponse.EnsureSuccessStatusCode();

        // Act: Search with GPS coordinates (Paris center)
        var searchUrl = $"/games/search?when={gameDateTime:O}&latitude=48.8566&longitude=2.3522&radiusKm=10&level=5";
        var searchResponse = await Client.GetAsync(searchUrl);

        // Assert
        searchResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await searchResponse.Content.ReadFromJsonAsync<SearchGamesQueryResponse>();

        result.Should().NotBeNull();
        result!.PerfectMatches.Should().HaveCount(1);
        result.PerfectMatches[0].DistanceKm.Should().NotBeNull();
        result.PerfectMatches[0].DistanceKm.Should().BeGreaterThan(0);
        result.PerfectMatches[0].DistanceKm.Should().BeLessThan(10); // Within radius
        result.PerfectMatches[0].DistanceKm.Should().BeApproximately(5.0, 2.0); // ~5km from Paris center
    }

    [Fact]
    public async Task GET_games_search_WithMultipleGames_ShouldSortByDistance()
    {
        // Arrange: Create multiple games at different distances
        var host = await Seeder.SeedUserAsync(u => u
            .WithExternalId("auth0|gps-host-2")
            .WithName("GPS Host 2")
            .Intermediate());

        AuthenticateAs(host.ExternalId);

        var gameDateTime = DateTime.UtcNow.AddHours(3);

        // Far game (~5km from Paris center)
        var farGameRequest = new
        {
            dateTime = gameDateTime,
            location = "Casa Padel Paris 15",
            skillLevel = 5,
            maxPlayers = 4,
            latitude = 48.8424,
            longitude = 2.2894
        };
        var farResponse = await Client.PostAsJsonAsync("/games", farGameRequest);
        var farGame = await farResponse.Content.ReadFromJsonAsync<CreateGameResult>();

        // Close game (~2km from Paris center)
        var closeGameRequest = new
        {
            dateTime = gameDateTime,
            location = "Padel Club Marais",
            skillLevel = 5,
            maxPlayers = 4,
            latitude = 48.8606,
            longitude = 2.3635
        };
        var closeResponse = await Client.PostAsJsonAsync("/games", closeGameRequest);
        var closeGame = await closeResponse.Content.ReadFromJsonAsync<CreateGameResult>();

        // Act: Search from Paris center
        var searchUrl = $"/games/search?when={gameDateTime:O}&latitude=48.8566&longitude=2.3522&radiusKm=10&level=5";
        var searchResponse = await Client.GetAsync(searchUrl);

        // Assert
        searchResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await searchResponse.Content.ReadFromJsonAsync<SearchGamesQueryResponse>();

        result.Should().NotBeNull();
        result!.PerfectMatches.Should().HaveCount(2);

        // First result should be closer game
        result.PerfectMatches[0].Id.Should().Be(closeGame!.Id);
        result.PerfectMatches[0].DistanceKm.Should().BeLessThan(result.PerfectMatches[1].DistanceKm.GetValueOrDefault());

        // Second result should be farther game
        result.PerfectMatches[1].Id.Should().Be(farGame!.Id);
    }

    [Fact]
    public async Task GET_games_search_WithRadiusFilter_ShouldExcludeFarGames()
    {
        // Arrange: Create two games - one near, one far
        var host = await Seeder.SeedUserAsync(u => u
            .WithExternalId("auth0|gps-host-3")
            .WithName("GPS Host 3")
            .Intermediate());

        AuthenticateAs(host.ExternalId);

        var gameDateTime = DateTime.UtcNow.AddHours(3);

        // Near game (~2km)
        var nearGameRequest = new
        {
            dateTime = gameDateTime,
            location = "Near Club",
            skillLevel = 5,
            maxPlayers = 4,
            latitude = 48.8606,
            longitude = 2.3635
        };
        var nearResponse = await Client.PostAsJsonAsync("/games", nearGameRequest);
        var nearGame = await nearResponse.Content.ReadFromJsonAsync<CreateGameResult>();

        // Far game (~20km - Versailles)
        var farGameRequest = new
        {
            dateTime = gameDateTime,
            location = "Far Club Versailles",
            skillLevel = 5,
            maxPlayers = 4,
            latitude = 48.8049,
            longitude = 2.1204
        };
        var farResponse = await Client.PostAsJsonAsync("/games", farGameRequest);
        var farGame = await farResponse.Content.ReadFromJsonAsync<CreateGameResult>();

        // Act: Search with small radius (5km)
        var searchUrl = $"/games/search?when={gameDateTime:O}&latitude=48.8566&longitude=2.3522&radiusKm=5&level=5";
        var searchResponse = await Client.GetAsync(searchUrl);

        // Assert
        searchResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await searchResponse.Content.ReadFromJsonAsync<SearchGamesQueryResponse>();

        result.Should().NotBeNull();
        var allMatches = result!.PerfectMatches.Concat(result.PartialMatches).ToList();

        // Only near game should be included
        allMatches.Should().Contain(g => g.Id == nearGame!.Id);
        allMatches.Should().NotContain(g => g.Id == farGame!.Id);
    }

    [Fact]
    public async Task GET_games_search_WithInvalidGpsCoordinates_Returns400()
    {
        // Arrange
        var when = DateTime.UtcNow.AddHours(3);

        // Act: Invalid latitude (>90)
        var response1 = await Client.GetAsync(
            $"/games/search?when={when:O}&latitude=100&longitude=2.3522&level=5"
        );

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error1 = await response1.Content.ReadAsStringAsync();
        error1.Should().Contain("Latitude");

        // Act: Invalid longitude (<-180)
        var response2 = await Client.GetAsync(
            $"/games/search?when={when:O}&latitude=48.8566&longitude=-200&level=5"
        );

        // Assert
        response2.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error2 = await response2.Content.ReadAsStringAsync();
        error2.Should().Contain("Longitude");
    }

    [Fact]
    public async Task GET_games_search_WithOnlyLatitude_Returns400()
    {
        // Arrange
        var when = DateTime.UtcNow.AddHours(3);

        // Act: Missing longitude
        var response = await Client.GetAsync(
            $"/games/search?when={when:O}&latitude=48.8566&level=5"
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadAsStringAsync();
        error.Should().Contain("latitude and longitude");
    }

    [Fact]
    public async Task GET_games_search_WithOnlyLongitude_Returns400()
    {
        // Arrange
        var when = DateTime.UtcNow.AddHours(3);

        // Act: Missing latitude
        var response = await Client.GetAsync(
            $"/games/search?when={when:O}&longitude=2.3522&level=5"
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadAsStringAsync();
        error.Should().Contain("latitude and longitude");
    }

    [Fact]
    public async Task GET_games_search_WithoutGps_ShouldFallbackToTextSearch()
    {
        // Arrange: Create game without GPS coordinates
        var host = await Seeder.SeedUserAsync(u => u
            .WithExternalId("auth0|no-gps-host")
            .WithName("No GPS Host")
            .Intermediate());

        AuthenticateAs(host.ExternalId);

        var gameDateTime = DateTime.UtcNow.AddHours(3);
        var createRequest = new
        {
            dateTime = gameDateTime,
            location = "Casa Padel Paris",
            skillLevel = 5,
            maxPlayers = 4
            // No latitude/longitude
        };
        var createResponse = await Client.PostAsJsonAsync("/games", createRequest);
        createResponse.EnsureSuccessStatusCode();

        // Act: Search without GPS coordinates (text-based search)
        var searchUrl = $"/games/search?when={gameDateTime:O}&where=Casa Padel&level=5";
        var searchResponse = await Client.GetAsync(searchUrl);

        // Assert
        searchResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await searchResponse.Content.ReadFromJsonAsync<SearchGamesQueryResponse>();

        result.Should().NotBeNull();
        var allMatches = result!.PerfectMatches.Concat(result.PartialMatches).ToList();
        allMatches.Should().NotBeEmpty();
        allMatches.Should().Contain(g => g.Location.Contains("Casa Padel"));

        // Games without GPS coordinates should have null distance
        var matchedGame = allMatches.First(g => g.Location.Contains("Casa Padel"));
        matchedGame.DistanceKm.Should().BeNull();
    }

    [Fact]
    public async Task POST_games_WithGpsCoordinates_ReturnsCoordinatesInResponse()
    {
        // Arrange
        var host = await Seeder.SeedUserAsync(u => u
            .WithExternalId("auth0|gps-create-host")
            .WithName("GPS Create Host")
            .Intermediate());

        var request = new
        {
            dateTime = DateTime.UtcNow.AddHours(3),
            location = "Test Club with GPS",
            skillLevel = 5,
            maxPlayers = 4,
            latitude = 48.8566,
            longitude = 2.3522
        };

        AuthenticateAs(host.ExternalId);

        // Act
        var response = await Client.PostAsJsonAsync("/games", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<CreateGameResult>();

        result.Should().NotBeNull();
        result!.Latitude.Should().Be(48.8566);
        result.Longitude.Should().Be(2.3522);
    }

    [Fact]
    public async Task POST_games_WithoutGpsCoordinates_ReturnsNullCoordinates()
    {
        // Arrange
        var host = await Seeder.SeedUserAsync(u => u
            .WithExternalId("auth0|no-gps-create-host")
            .WithName("No GPS Create Host")
            .Intermediate());

        var request = new
        {
            dateTime = DateTime.UtcNow.AddHours(3),
            location = "Test Club without GPS",
            skillLevel = 5,
            maxPlayers = 4
            // No GPS coordinates
        };

        AuthenticateAs(host.ExternalId);

        // Act
        var response = await Client.PostAsJsonAsync("/games", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<CreateGameResult>();

        result.Should().NotBeNull();
        result!.Latitude.Should().BeNull();
        result.Longitude.Should().BeNull();
    }

    [Fact]
    public async Task GET_games_search_PerfectMatchWithGps_ShouldHaveScore4()
    {
        // Arrange: Create perfect match game (±2h, <2km, ±1 level)
        var host = await Seeder.SeedUserAsync(u => u
            .WithExternalId("auth0|perfect-match-host")
            .WithName("Perfect Match Host")
            .Intermediate());

        AuthenticateAs(host.ExternalId);

        var targetDateTime = DateTime.UtcNow.AddHours(3);
        var createRequest = new
        {
            dateTime = targetDateTime.AddMinutes(30), // Within ±2h
            location = "Perfect Club",
            skillLevel = 5, // Exact level
            maxPlayers = 4,
            latitude = 48.8606, // ~2km from Paris center
            longitude = 2.3635
        };
        await Client.PostAsJsonAsync("/games", createRequest);

        // Act: Search with GPS
        var searchUrl = $"/games/search?when={targetDateTime:O}&latitude=48.8566&longitude=2.3522&radiusKm=10&level=5";
        var searchResponse = await Client.GetAsync(searchUrl);

        // Assert
        searchResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await searchResponse.Content.ReadFromJsonAsync<SearchGamesQueryResponse>();

        result.Should().NotBeNull();
        result!.PerfectMatches.Should().HaveCount(1);
        result.PerfectMatches[0].MatchScore.Should().Be(4); // Perfect match = 4 points
        result.PerfectMatches[0].DistanceKm.Should().BeLessThan(2.5);
    }

    // Response DTOs matching API contract
    private record CreateGameResult(
        Guid Id,
        DateTime DateTime,
        string Location,
        int SkillLevel,
        int MaxPlayers,
        string HostExternalId,
        int CurrentPlayers,
        double? Latitude,
        double? Longitude
    );
}
