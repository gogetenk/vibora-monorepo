using System.Net;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Vibora.Games.Domain;
using Vibora.Integration.Tests.Infrastructure;

namespace Vibora.Integration.Tests.Games;

public class LeaveGameIntegrationTests : IntegrationTestBaseImproved
{
    public LeaveGameIntegrationTests(ViboraWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task LeaveGame_WithValidRequest_ShouldLeaveSuccessfully()
    {
        // Arrange - Create a game with host and a participant
        var scenario = await Seeder.SeedCompleteGameAsync(
            participantCount: 1, // Host + 1 participant
            configureGame: g => g.WithId(Guid.NewGuid()));

        var player = scenario.Participants.First();
        AuthenticateAs(player.ExternalId);

        // Act - Leave the game
        var response = await Client.DeleteAsync($"/games/{scenario.Game.Id}/players?userExternalId={player.ExternalId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.ReadAsAsync<LeaveGameResponse>();
        result.Should().NotBeNull();
        result!.GameId.Should().Be(scenario.Game.Id);
        result.Message.Should().Contain("Successfully left");

        // Verify player is removed from database
        var gameInDb = await Seeder.QueryGamesAsync(db =>
            db.Games.Include(g => g.Participations)
                .FirstOrDefaultAsync(g => g.Id == scenario.Game.Id)
        );

        gameInDb.Should().NotBeNull();
        gameInDb!.Participations.Should().NotContain(p => p.UserExternalId == player.ExternalId);
        gameInDb.CurrentPlayers.Should().Be(1); // Only host remaining
    }

    [Fact]
    public async Task LeaveGame_WhenHostTriesToLeave_ShouldReturnError()
    {
        // Arrange - Create a game
        var (host, game) = await Seeder.SeedGameWithHostAsync("auth0|host");

        AuthenticateAs(host.ExternalId);

        // Act - Host tries to leave
        var response = await Client.DeleteAsync($"/games/{game.Id}/players?userExternalId={host.ExternalId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var errorContent = await response.Content.ReadAsStringAsync();
        errorContent.Should().Contain("Host cannot leave");
    }

    [Fact]
    public async Task LeaveGame_WithNonExistentGame_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentGameId = Guid.NewGuid();

        AuthenticateAs("auth0|player");

        // Act
        var response = await Client.DeleteAsync($"/games/{nonExistentGameId}/players?userExternalId=auth0|player");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task LeaveGame_WhenUserNotInGame_ShouldReturnError()
    {
        // Arrange - Create a game without the player
        var (host, game) = await Seeder.SeedGameWithHostAsync("auth0|host");

        AuthenticateAs("auth0|not-a-participant");

        // Act - Try to leave a game you're not in
        var response = await Client.DeleteAsync($"/games/{game.Id}/players?userExternalId=auth0|not-a-participant");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var errorContent = await response.Content.ReadAsStringAsync();
        errorContent.Should().Contain("not a participant");
    }

    [Fact]
    public async Task LeaveGame_WhenGameIsCanceled_ShouldReturnError()
    {
        // Arrange - Create a game with participant and cancel it
        var scenario = await Seeder.SeedCompleteGameAsync(
            participantCount: 1,
            configureGame: g => g.WithId(Guid.NewGuid()).Canceled());

        var player = scenario.Participants.First();
        AuthenticateAs(player.ExternalId);

        // Act
        var response = await Client.DeleteAsync($"/games/{scenario.Game.Id}/players?userExternalId={player.ExternalId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var errorContent = await response.Content.ReadAsStringAsync();
        errorContent.Should().Contain("canceled");
    }

    [Fact]
    public async Task LeaveGame_WhenGameIsInPast_ShouldReturnError()
    {
        // Arrange - Create a game in the past with participant
        var host = await Seeder.SeedUserAsync(u => u
            .WithExternalId("auth0|host")
            .Intermediate());

        var player = await Seeder.SeedUserAsync(u => u
            .WithExternalId("auth0|player-123")
            .Intermediate());

        // Create game with participant, then update date to past using SQL
        var game = await Seeder.SeedGameAsync(g => g
            .WithHost(host.ExternalId)
            .WithParticipant(player.ExternalId, player.Name, player.SkillLevel.ToString()));

        await Seeder.QueryGamesAsync(async db =>
        {
            await db.Database.ExecuteSqlRawAsync(
                "UPDATE \"Games\" SET \"DateTime\" = @p0 WHERE \"Id\" = @p1",
                DateTime.UtcNow.AddDays(-1),
                game.Id
            );
            return true;
        });

        AuthenticateAs(player.ExternalId);

        // Act
        var response = await Client.DeleteAsync($"/games/{game.Id}/players?userExternalId={player.ExternalId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var errorContent = await response.Content.ReadAsStringAsync();
        (errorContent.Contains("already started") || errorContent.Contains("in the past")).Should().BeTrue();
    }

    [Fact]
    public async Task LeaveGame_WhenGameBecomesOpen_ShouldUpdateStatus()
    {
        // Arrange - Create a full game (4/4 players)
        var scenario = await Seeder.SeedCompleteGameAsync(
            participantCount: 3, // Host + 3 participants = 4 (full)
            configureGame: g => g.WithId(Guid.NewGuid()).WithMaxPlayers(4));

        // Verify game is full
        var gameBefore = await Seeder.QueryGamesAsync(db =>
            db.Games.Include(g => g.Participations)
                .FirstOrDefaultAsync(g => g.Id == scenario.Game.Id)
        );
        gameBefore!.Status.Should().Be(GameStatus.Full);

        var player = scenario.Participants.First();
        AuthenticateAs(player.ExternalId);

        // Act - One player leaves
        var response = await Client.DeleteAsync($"/games/{scenario.Game.Id}/players?userExternalId={player.ExternalId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var gameAfter = await Seeder.QueryGamesAsync(db =>
            db.Games.Include(g => g.Participations)
                .FirstOrDefaultAsync(g => g.Id == scenario.Game.Id)
        );

        gameAfter.Should().NotBeNull();
        gameAfter!.Status.Should().Be(GameStatus.Open); // Changed from Full to Open
        gameAfter.CurrentPlayers.Should().Be(3); // 4 -> 3 players
    }

    // Response DTOs
    private record LeaveGameResponse(
        Guid GameId,
        string Message
    );
}
