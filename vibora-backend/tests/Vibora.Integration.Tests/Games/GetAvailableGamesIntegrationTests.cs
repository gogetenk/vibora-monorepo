using System.Net;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Vibora.Integration.Tests.Infrastructure;

namespace Vibora.Integration.Tests.Games;

public class GetAvailableGamesIntegrationTests : IntegrationTestBaseImproved
{
    public GetAvailableGamesIntegrationTests(ViboraWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetAvailableGames_WithNoFilters_ShouldReturnAllFutureGames()
    {
        // Arrange - Seed test data
        await SeedTestDataAsync();

        // Act
        var response = await Client.GetAsync("/games");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.ReadAsAsync<GetAvailableGamesResponse>();
        result.Should().NotBeNull();
        result!.Games.Should().HaveCount(3); // 3 future games
        result.TotalCount.Should().Be(3);
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(20);
        result.TotalPages.Should().Be(1);

        // Verify games are ordered by date (nearest first)
        result.Games[0].Location.Should().Be("Club Paris 1");
        result.Games[1].Location.Should().Be("Club Paris 2");
        result.Games[2].Location.Should().Be("Club Paris 3");
    }

    [Fact]
    public async Task GetAvailableGames_WithLocationFilter_ShouldReturnFilteredGames()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act - Filter by location containing "Paris 2"
        var response = await Client.GetAsync("/games?location=Paris 2");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.ReadAsAsync<GetAvailableGamesResponse>();
        result.Should().NotBeNull();
        result!.Games.Should().HaveCount(1);
        result.Games[0].Location.Should().Be("Club Paris 2");
    }

    [Fact]
    public async Task GetAvailableGames_WithSkillLevelFilter_ShouldReturnFilteredGames()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act - Filter by skill level
        var response = await Client.GetAsync("/games?skillLevel=Advanced");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.ReadAsAsync<GetAvailableGamesResponse>();
        result.Should().NotBeNull();
        result!.Games.Should().HaveCount(1);
        result.Games[0].SkillLevel.Should().Be("Advanced");
    }

    [Fact]
    public async Task GetAvailableGames_WithDateRangeFilter_ShouldReturnFilteredGames()
    {
        // Arrange
        await SeedTestDataAsync();

        // Use a date range that will definitely include Club Paris 1 (at UtcNow + 1 day)
        // but exclude Club Paris 2 (at UtcNow + 3 days) and Club Paris 3 (at UtcNow + 5 days)
        var fromDate = DateTime.UtcNow.AddHours(12); // 12 hours from now
        var toDate = DateTime.UtcNow.AddDays(2);      // 2 days from now

        // Act - Filter by date range
        var response = await Client.GetAsync(
            $"/games?fromDate={fromDate:yyyy-MM-ddTHH:mm:ss}Z&toDate={toDate:yyyy-MM-ddTHH:mm:ss}Z");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.ReadAsAsync<GetAvailableGamesResponse>();
        result.Should().NotBeNull();
        result!.Games.Should().HaveCount(1);
        result.Games[0].Location.Should().Be("Club Paris 1");
    }

    [Fact]
    public async Task GetAvailableGames_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act - Request page 2 with page size 2
        var response = await Client.GetAsync("/games?pageNumber=2&pageSize=2");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.ReadAsAsync<GetAvailableGamesResponse>();
        result.Should().NotBeNull();
        result!.Games.Should().HaveCount(1); // Only 1 game on page 2
        result.TotalCount.Should().Be(3);
        result.PageNumber.Should().Be(2);
        result.PageSize.Should().Be(2);
        result.TotalPages.Should().Be(2);
    }

    [Fact]
    public async Task GetAvailableGames_WithInvalidPageNumber_ShouldReturnBadRequest()
    {
        // Act
        var response = await Client.GetAsync("/games?pageNumber=0");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetAvailableGames_WithInvalidPageSize_ShouldReturnBadRequest()
    {
        // Act
        var response = await Client.GetAsync("/games?pageSize=0");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetAvailableGames_WithInvalidDateRange_ShouldReturnBadRequest()
    {
        // Arrange
        var fromDate = DateTime.UtcNow.AddDays(5);
        var toDate = DateTime.UtcNow.AddDays(1);

        // Act - FromDate after ToDate (invalid)
        var response = await Client.GetAsync(
            $"/games?fromDate={fromDate:yyyy-MM-ddTHH:mm:ss}Z&toDate={toDate:yyyy-MM-ddTHH:mm:ss}Z");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetAvailableGames_ShouldNotReturnCanceledGames()
    {
        // Arrange - Create a canceled game using GameBuilder
        var host = await Seeder.SeedUserAsync(u => u
            .WithExternalId("auth0|host-canceled")
            .Intermediate());

        await Seeder.SeedGameAsync(g => g
            .WithHost(host.ExternalId)
            .AtLocation("Canceled Club")
            .At(DateTime.UtcNow.AddDays(1))
            .Canceled());

        // Act
        var response = await Client.GetAsync("/games");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.ReadAsAsync<GetAvailableGamesResponse>();
        result.Should().NotBeNull();
        result!.Games.Should().NotContain(g => g.Location == "Canceled Club");
    }

    [Fact]
    public async Task GetAvailableGames_ShouldNotReturnPastGames()
    {
        // Arrange - Create a past game using raw SQL to bypass domain validation
        var host = await Seeder.SeedUserAsync(u => u
            .WithExternalId("auth0|host-past")
            .Intermediate());

        await Seeder.QueryGamesAsync(async db =>
        {
            // Insert past game directly via SQL to bypass domain validation
            var gameId = Guid.NewGuid();
            var pastDate = DateTime.UtcNow.AddDays(-1); // Past date
            var createdAt = DateTime.UtcNow.AddDays(-2);

            await db.Database.ExecuteSqlRawAsync(
                @"INSERT INTO ""Games"" (""Id"", ""DateTime"", ""Location"", ""SkillLevel"", ""MaxPlayers"", ""CurrentPlayers"", ""HostExternalId"", ""Status"", ""CreatedAt"")
                  VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8})",
                gameId, pastDate, "Past Club", "Intermediate", 4, 1, host.ExternalId, 0, createdAt);

            return true;
        });

        // Act
        var response = await Client.GetAsync("/games");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.ReadAsAsync<GetAvailableGamesResponse>();
        result.Should().NotBeNull();
        result!.Games.Should().NotContain(g => g.Location == "Past Club");
    }

    // Helper methods
    private async Task SeedTestDataAsync()
    {
        // Seed users and games using Seeder
        var host1 = await Seeder.SeedUserAsync(u => u
            .WithExternalId("auth0|host-1")
            .WithName("Host 1")
            .Intermediate());

        var host2 = await Seeder.SeedUserAsync(u => u
            .WithExternalId("auth0|host-2")
            .WithName("Host 2")
            .Advanced());

        var host3 = await Seeder.SeedUserAsync(u => u
            .WithExternalId("auth0|host-3")
            .WithName("Host 3")
            .Beginner());

        // Create games with different dates
        await Seeder.SeedGameAsync(g => g
            .WithHost(host1.ExternalId)
            .AtLocation("Club Paris 1")
            .At(DateTime.UtcNow.AddDays(1))
            .WithSkillLevel("Intermediate"));

        await Seeder.SeedGameAsync(g => g
            .WithHost(host2.ExternalId)
            .AtLocation("Club Paris 2")
            .At(DateTime.UtcNow.AddDays(3))
            .WithSkillLevel("Advanced"));

        await Seeder.SeedGameAsync(g => g
            .WithHost(host3.ExternalId)
            .AtLocation("Club Paris 3")
            .At(DateTime.UtcNow.AddDays(5))
            .WithSkillLevel("Beginner"));
    }

    // Response DTOs
    private record GetAvailableGamesResponse(
        List<GameListItemResponse> Games,
        int TotalCount,
        int PageNumber,
        int PageSize,
        int TotalPages
    );

    private record GameListItemResponse(
        Guid Id,
        DateTime DateTime,
        string Location,
        string SkillLevel,
        int MaxPlayers,
        int CurrentPlayers,
        string HostExternalId,
        string Status,
        DateTime CreatedAt
    );
}
