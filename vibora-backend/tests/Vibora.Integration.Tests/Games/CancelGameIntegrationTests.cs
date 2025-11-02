using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System.Net;
using Vibora.Games.Domain;
using Vibora.Integration.Tests.Infrastructure;

namespace Vibora.Integration.Tests.Games;

public class CancelGameIntegrationTests : IntegrationTestBaseImproved
{
    public CancelGameIntegrationTests(ViboraWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task CancelGame_WithValidRequest_ShouldCancelSuccessfully()
    {
        // Arrange - Create a game with host
        var (host, game) = await Seeder.SeedGameWithHostAsync("auth0|host");

        AuthenticateAs(host.ExternalId);

        // Act - Cancel the game
        var response = await Client.PostAsync($"/games/{game.Id}/cancel?hostExternalId={host.ExternalId}", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.ReadAsAsync<CancelGameResponse>();
        result.Should().NotBeNull();
        result!.GameId.Should().Be(game.Id);
        result.Message.Should().Contain("canceled successfully");

        // Verify game is canceled in database
        var gameInDb = await Seeder.QueryGamesAsync(db =>
            db.Games.FirstOrDefaultAsync(g => g.Id == game.Id)
        );

        gameInDb.Should().NotBeNull();
        gameInDb!.Status.Should().Be(GameStatus.Canceled);
    }

    [Fact]
    public async Task CancelGame_WhenUserIsNotHost_ShouldReturnForbidden()
    {
        // Arrange - Create a game
        var (host, game) = await Seeder.SeedGameWithHostAsync("auth0|host");

        // Authenticate as non-host
        AuthenticateAs("auth0|not-the-host");

        // Act - Non-host tries to cancel
        var response = await Client.PostAsync($"/games/{game.Id}/cancel?hostExternalId=auth0|not-the-host", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CancelGame_WithNonExistentGame_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentGameId = Guid.NewGuid();

        // Authenticate as any host
        AuthenticateAs("auth0|host");

        // Act
        var response = await Client.PostAsync($"/games/{nonExistentGameId}/cancel?hostExternalId=auth0|host", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CancelGame_WhenGameAlreadyCanceled_ShouldReturnError()
    {
        // Arrange - Create already canceled game
        var host = await Seeder.SeedUserAsync(u => u.WithExternalId("auth0|host").Intermediate());
        var game = await Seeder.SeedGameAsync(g => g
            .WithHost(host.ExternalId)
            .Canceled());

        AuthenticateAs(host.ExternalId);

        // Act - Try to cancel again
        var response = await Client.PostAsync($"/games/{game.Id}/cancel?hostExternalId={host.ExternalId}", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var errorContent = await response.Content.ReadAsStringAsync();
        errorContent.Should().Contain("already canceled");
    }

    [Fact]
    public async Task CancelGame_WithParticipants_ShouldCancelForAll()
    {
        // Arrange - Create a game with participants
        var scenario = await Seeder.SeedCompleteGameAsync(
            participantCount: 2, // Host + 2 participants = 3 total
            configureGame: g => g.WithId(Guid.NewGuid()));

        AuthenticateAs(scenario.Host.ExternalId);

        // Act - Host cancels the game
        var response = await Client.PostAsync($"/games/{scenario.Game.Id}/cancel?hostExternalId={scenario.Host.ExternalId}", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify game is canceled with all participants still in database
        var gameInDb = await Seeder.QueryGamesAsync(db =>
            db.Games.Include(g => g.Participations)
                .FirstOrDefaultAsync(g => g.Id == scenario.Game.Id)
        );

        gameInDb.Should().NotBeNull();
        gameInDb!.Status.Should().Be(GameStatus.Canceled);
        gameInDb.Participations.Should().HaveCount(3); // Host + 2 players
    }

    // Response DTOs
    private record CancelGameResponse(
        Guid GameId,
        string Message
    );
}
