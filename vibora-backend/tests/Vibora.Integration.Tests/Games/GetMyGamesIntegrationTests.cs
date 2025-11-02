using System.Net;
using FluentAssertions;
using Vibora.Integration.Tests.Infrastructure;

namespace Vibora.Integration.Tests.Games;

public class GetMyGamesIntegrationTests : IntegrationTestBaseImproved
{
    public GetMyGamesIntegrationTests(ViboraWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetMyGames_WithUpcomingGames_ShouldReturnUserGames()
    {
        // Arrange - User with 2 upcoming games (1 as host, 1 as participant)
        var user = await Seeder.SeedUserAsync(u => u
            .WithExternalId("auth0|user-123")
            .WithName("Test User")
            .Intermediate());

        var otherHost = await Seeder.SeedUserAsync(u => u
            .WithExternalId("auth0|other-host")
            .WithName("Other Host")
            .Advanced());

        // Game 1: User is host
        await Seeder.SeedGameAsync(g => g
            .WithHost(user.ExternalId)
            .AtLocation("Club A")
            .At(DateTime.UtcNow.AddDays(1)));

        // Game 2: User is participant
        await Seeder.SeedGameAsync(g => g
            .WithHost(otherHost.ExternalId)
            .AtLocation("Club B")
            .At(DateTime.UtcNow.AddDays(2))
            .WithSkillLevel("Advanced")
            .WithParticipant(user.ExternalId, user.Name, user.SkillLevel.ToString()));

        AuthenticateAs(user.ExternalId);

        // Act
        var response = await Client.GetAsync($"/games/me?userExternalId={user.ExternalId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.ReadAsAsync<MyGamesResponse>();
        result.Should().NotBeNull();
        result!.Games.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);

        // Verify one is as host
        result.Games.Should().Contain(g => g.IsHost);
        result.Games.Should().Contain(g => !g.IsHost);
    }

    [Fact]
    public async Task GetMyGames_WithNoGames_ShouldReturnEmptyList()
    {
        // Arrange - User with no games
        var user = await Seeder.SeedUserAsync(u => u
            .WithExternalId("auth0|user-no-games")
            .WithName("User")
            .Intermediate());

        AuthenticateAs(user.ExternalId);

        // Act
        var response = await Client.GetAsync($"/games/me?userExternalId={user.ExternalId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.ReadAsAsync<MyGamesResponse>();
        result.Should().NotBeNull();
        result!.Games.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    // Response DTOs
    private record MyGamesResponse(
        List<MyGameDto> Games,
        int TotalCount
    );

    private record MyGameDto(
        Guid Id,
        DateTime DateTime,
        string Location,
        string SkillLevel,
        int MaxPlayers,
        int CurrentPlayers,
        string Status,
        bool IsHost,
        string? Notes
    );
}
