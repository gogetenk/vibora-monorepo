using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Vibora.Integration.Tests.Infrastructure;
using Xunit;

namespace Vibora.Integration.Tests.Games;

public class SearchGamesIntegrationTests : IntegrationTestBaseImproved
{
    public SearchGamesIntegrationTests(ViboraWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task SearchGames_WithPerfectMatch_ReturnsPerfectMatches()
    {
        // Arrange
        var host = await Seeder.SeedUserAsync(u => u
            .WithExternalId("auth0|host-123")
            .WithName("Host User")
            .Intermediate());

        var targetDateTime = DateTime.UtcNow.AddDays(1);

        // Create 2 perfect matches (±2h, same location, ±1 level)
        var game1 = await CreateGameAsync(host.ExternalId, targetDateTime, "Casa Padel Paris", 5);
        var game2 = await CreateGameAsync(host.ExternalId, targetDateTime.AddHours(1.5), "Casa Padel Paris", 6);

        // Create 1 partial match (different location)
        var game3 = await CreateGameAsync(host.ExternalId, targetDateTime, "Urban Padel", 5);

        // Act
        var response = await Client.GetAsync($"/games/search?when={targetDateTime:O}&where=Casa Padel Paris&level=5");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<SearchGamesResponse>();

        result.Should().NotBeNull();
        result!.PerfectMatches.Should().HaveCount(2);
        result.PerfectMatches.Should().Contain(g => g.Id == game1);
        result.PerfectMatches.Should().Contain(g => g.Id == game2);
        result.PerfectMatches.Should().AllSatisfy(g => g.MatchScore.Should().Be(3));

        result.PartialMatches.Should().HaveCount(1);
        result.PartialMatches.Should().Contain(g => g.Id == game3);
        result.PartialMatches[0].MatchScore.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task SearchGames_WithOnlyPartialMatches_ReturnsPartialMatches()
    {
        // Arrange
        var host = await Seeder.SeedUserAsync(u => u
            .WithExternalId("auth0|host-123")
            .WithName("Host User")
            .Intermediate());

        var targetDateTime = DateTime.UtcNow.AddDays(1);

        // Create 3 partial matches (different times/locations but same level)
        var game1 = await CreateGameAsync(host.ExternalId, targetDateTime.AddHours(3), "Casa Padel Paris", 5);
        var game2 = await CreateGameAsync(host.ExternalId, targetDateTime, "Urban Padel", 5);
        var game3 = await CreateGameAsync(host.ExternalId, targetDateTime.AddHours(5), "Casa Padel Paris", 7);

        // Act
        var response = await Client.GetAsync($"/games/search?when={targetDateTime:O}&where=Casa Padel Paris&level=5");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<SearchGamesResponse>();

        result.Should().NotBeNull();
        result!.PerfectMatches.Should().BeEmpty();
        result.PartialMatches.Should().HaveCount(3);
        result.PartialMatches.Should().AllSatisfy(g => g.MatchScore.Should().BeInRange(1, 2));
    }

    [Fact]
    public async Task SearchGames_WithNoMatches_ReturnsEmptyLists()
    {
        // Arrange
        var host = await Seeder.SeedUserAsync(u => u
            .WithExternalId("auth0|host-123")
            .WithName("Host User")
            .Advanced());

        var targetDateTime = DateTime.UtcNow.AddDays(1);

        // Create game with completely different criteria (time > 4h, different location, level diff > 2)
        await CreateGameAsync(host.ExternalId, targetDateTime.AddDays(2), "Far Away Club", 10);

        // Act
        var response = await Client.GetAsync($"/games/search?when={targetDateTime:O}&where=Casa Padel Paris&level=5");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<SearchGamesResponse>();

        result.Should().NotBeNull();
        result!.PerfectMatches.Should().BeEmpty();
        result!.PartialMatches.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchGames_OrdersPerfectMatchesByClosestTime()
    {
        // Arrange
        var host = await Seeder.SeedUserAsync(u => u
            .WithExternalId("auth0|host-123")
            .WithName("Host User")
            .Intermediate());

        var targetDateTime = DateTime.UtcNow.AddDays(1);

        var game1 = await CreateGameAsync(host.ExternalId, targetDateTime.AddHours(-1), "Casa Padel Paris", 5);   // 1h before
        var game2 = await CreateGameAsync(host.ExternalId, targetDateTime.AddMinutes(30), "Casa Padel Paris", 5); // 30min after
        var game3 = await CreateGameAsync(host.ExternalId, targetDateTime.AddHours(2), "Casa Padel Paris", 5);    // 2h after

        // Act
        var response = await Client.GetAsync($"/games/search?when={targetDateTime:O}&where=Casa Padel Paris&level=5");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<SearchGamesResponse>();

        result.Should().NotBeNull();
        result!.PerfectMatches.Should().HaveCount(3);
        result.PerfectMatches[0].Id.Should().Be(game2);  // Closest (30min)
        result.PerfectMatches[1].Id.Should().Be(game1);  // 2nd closest (1h)
        result.PerfectMatches[2].Id.Should().Be(game3);  // Furthest (2h)
    }

    [Fact]
    public async Task SearchGames_LimitsPerfectMatchesTo3()
    {
        // Arrange
        var host = await Seeder.SeedUserAsync(u => u
            .WithExternalId("auth0|host-123")
            .WithName("Host User")
            .Intermediate());

        var targetDateTime = DateTime.UtcNow.AddDays(1);

        // Create 5 perfect matches
        for (int i = 0; i < 5; i++)
        {
            await CreateGameAsync(host.ExternalId, targetDateTime.AddMinutes(i * 10), "Casa Padel Paris", 5);
        }

        // Act
        var response = await Client.GetAsync($"/games/search?when={targetDateTime:O}&where=Casa Padel Paris&level=5");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<SearchGamesResponse>();

        result.Should().NotBeNull();
        result!.PerfectMatches.Should().HaveCount(3);  // Limited to 3
    }

    [Fact]
    public async Task SearchGames_WithInvalidParameters_ReturnsBadRequest()
    {
        // Act - Invalid 'when' format
        var response1 = await Client.GetAsync("/games/search?when=invalid&where=Casa Padel Paris&level=5");
        response1.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Act - Missing 'where'
        var response2 = await Client.GetAsync($"/games/search?when={DateTime.UtcNow:O}&level=5");
        response2.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Act - Invalid 'level' (out of range)
        var response3 = await Client.GetAsync($"/games/search?when={DateTime.UtcNow:O}&where=Casa Padel Paris&level=15");
        response3.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // Helper method to create a game
    private async Task<Guid> CreateGameAsync(string hostExternalId, DateTime dateTime, string location, int level)
    {
        var createRequest = new
        {
            hostExternalId = hostExternalId,
            dateTime = dateTime,
            location = location,
            skillLevel = level.ToString(),
            maxPlayers = 4
        };

        AuthenticateAs(hostExternalId);
        var response = await Client.PostAsJsonAsync("/games", createRequest);
        response.EnsureSuccessStatusCode();

        var game = await response.Content.ReadFromJsonAsync<GameCreatedResponse>();
        return game!.Id;
    }

    // Response DTOs
    private record SearchGamesResponse(
        List<GameMatchDto> PerfectMatches,
        List<GameMatchDto> PartialMatches
    );

    private record GameMatchDto(
        Guid Id,
        DateTime DateTime,
        string Location,
        int? SkillLevel,
        int CurrentPlayers,
        int MaxPlayers,
        string Status,
        int MatchScore,
        string HostDisplayName
    );

    private record GameCreatedResponse(
        Guid Id,
        DateTime DateTime,
        string Location,
        string SkillLevel,
        int MaxPlayers,
        string HostExternalId,
        int CurrentPlayers
    );
}
