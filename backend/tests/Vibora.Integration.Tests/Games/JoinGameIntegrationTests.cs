using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Vibora.Integration.Tests.Infrastructure;

namespace Vibora.Integration.Tests.Games;

public class JoinGameIntegrationTests : IntegrationTestBaseImproved
{
    public JoinGameIntegrationTests(ViboraWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task JoinGame_WithValidRequest_ShouldJoinSuccessfully()
    {
        // Arrange - Create a game and a user
        var (host, game) = await Seeder.SeedGameWithHostAsync("auth0|host");
        var player = await Seeder.SeedUserAsync(u => u
            .WithExternalId("auth0|player-123")
            .WithName("John Doe")
            .Intermediate());

        var request = new
        {
            UserName = player.Name,
            UserSkillLevel = player.SkillLevel.ToString(),
            UserExternalId = player.ExternalId // For tests only
        };

        AuthenticateAs(player.ExternalId);

        // Act
        var response = await Client.PostAsJsonAsync($"/games/{game.Id}/players", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.ReadAsAsync<JoinGameResponse>();
        result.Should().NotBeNull();
        result!.GameId.Should().Be(game.Id);
        result.Message.Should().Contain("Successfully joined");
    }

    [Fact]
    public async Task JoinGame_WithNonExistentGame_ShouldReturnNotFound()
    {
        // Arrange
        var player = await Seeder.SeedUserAsync(u => u
            .WithExternalId("auth0|player")
            .Intermediate());

        var nonExistentGameId = Guid.NewGuid();

        var request = new
        {
            UserName = player.Name,
            UserSkillLevel = player.SkillLevel.ToString(),
            UserExternalId = player.ExternalId
        };

        AuthenticateAs(player.ExternalId);

        // Act
        var response = await Client.PostAsJsonAsync($"/games/{nonExistentGameId}/players", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // Response DTOs
    private record JoinGameResponse(
        Guid GameId,
        Guid ParticipationId,
        string Message
    );
}
