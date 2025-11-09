using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Vibora.Games.Domain;
using Vibora.Integration.Tests.Infrastructure;

namespace Vibora.Integration.Tests.Games;

public class JoinGameAsGuestIntegrationTests : IntegrationTestBaseImproved
{
    public JoinGameAsGuestIntegrationTests(ViboraWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task JoinGameAsGuest_WithPhoneNumber_ShouldJoinSuccessfully()
    {
        // Arrange
        var (host, game) = await Seeder.SeedGameWithHostAsync("auth0|host");

        var request = new
        {
            Name = "Guest Player",
            PhoneNumber = "+33612345678",
            Email = (string?)null
        };

        // Act - No authentication required for guest
        var response = await Client.PostAsJsonAsync($"/games/{game.Id}/players/guest", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.ReadAsAsync<JoinGameAsGuestResponse>();
        result.Should().NotBeNull();
        result!.GameId.Should().Be(game.Id);
        result.GuestName.Should().Be("Guest Player");
        result.CurrentPlayers.Should().Be(2); // Host + guest
        result.Message.Should().Contain("Successfully joined");

        // Verify in database
        var gameInDb = await Seeder.QueryGamesAsync(db =>
            db.Games
                .Include(g => g.GuestParticipants)
                .FirstOrDefaultAsync(g => g.Id == game.Id));

        gameInDb.Should().NotBeNull();
        gameInDb!.GuestParticipants.Should().HaveCount(1);
        gameInDb.GuestParticipants.First().Name.Should().Be("Guest Player");
        gameInDb.GuestParticipants.First().PhoneNumber.Should().Be("+33612345678");
    }

    [Fact]
    public async Task JoinGameAsGuest_WithEmail_ShouldJoinSuccessfully()
    {
        // Arrange
        var (host, game) = await Seeder.SeedGameWithHostAsync();

        var request = new
        {
            Name = "Guest Player",
            PhoneNumber = (string?)null,
            Email = "guest@example.com"
        };

        // Act
        var response = await Client.PostAsJsonAsync($"/games/{game.Id}/players/guest", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.ReadAsAsync<JoinGameAsGuestResponse>();
        result.Should().NotBeNull();
        result!.GuestName.Should().Be("Guest Player");

        // Verify in database
        var gameInDb = await Seeder.QueryGamesAsync(db =>
            db.Games
                .Include(g => g.GuestParticipants)
                .FirstOrDefaultAsync(g => g.Id == game.Id));

        gameInDb!.GuestParticipants.First().Email.Should().Be("guest@example.com");
    }

    [Fact]
    public async Task JoinGameAsGuest_WithBothPhoneAndEmail_ShouldJoinSuccessfully()
    {
        // Arrange
        var (host, game) = await Seeder.SeedGameWithHostAsync();

        var request = new
        {
            Name = "Guest Player",
            PhoneNumber = "+33612345678",
            Email = "guest@example.com"
        };

        // Act
        var response = await Client.PostAsJsonAsync($"/games/{game.Id}/players/guest", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify both contact methods are stored
        var gameInDb = await Seeder.QueryGamesAsync(db =>
            db.Games
                .Include(g => g.GuestParticipants)
                .FirstOrDefaultAsync(g => g.Id == game.Id));

        var guest = gameInDb!.GuestParticipants.First();
        guest.PhoneNumber.Should().Be("+33612345678");
        guest.Email.Should().Be("guest@example.com");
    }

    [Fact]
    public async Task JoinGameAsGuest_WithNoContactInfo_ShouldReturnBadRequest()
    {
        // Arrange
        var (host, game) = await Seeder.SeedGameWithHostAsync();

        var request = new
        {
            Name = "Guest Player",
            PhoneNumber = (string?)null,
            Email = (string?)null
        };

        // Act
        var response = await Client.PostAsJsonAsync($"/games/{game.Id}/players/guest", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task JoinGameAsGuest_WithNonExistentGame_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentGameId = Guid.NewGuid();

        var request = new
        {
            Name = "Guest Player",
            PhoneNumber = "+33612345678",
            Email = (string?)null
        };

        // Act
        var response = await Client.PostAsJsonAsync($"/games/{nonExistentGameId}/players/guest", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task JoinGameAsGuest_WhenGameIsFull_ShouldReturnBadRequest()
    {
        // Arrange - Create a full game
        var scenario = await Seeder.SeedCompleteGameAsync(
            participantCount: 3, // Host + 3 = 4 (full)
            configureGame: g => g.WithId(Guid.NewGuid()).WithMaxPlayers(4));

        var request = new
        {
            Name = "Guest Player",
            PhoneNumber = "+33612345678",
            Email = (string?)null
        };

        // Act
        var response = await Client.PostAsJsonAsync($"/games/{scenario.Game.Id}/players/guest", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Game is already full");
    }

    [Fact]
    public async Task JoinGameAsGuest_WhenGameIsCanceled_ShouldReturnBadRequest()
    {
        // Arrange - Create a canceled game
        var host = await Seeder.SeedUserAsync(u => u.WithExternalId("auth0|host"));
        var game = await Seeder.SeedGameAsync(g => g
            .WithHost(host.ExternalId)
            .Canceled());

        var request = new
        {
            Name = "Guest Player",
            PhoneNumber = "+33612345678",
            Email = (string?)null
        };

        // Act
        var response = await Client.PostAsJsonAsync($"/games/{game.Id}/players/guest", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Cannot join a canceled game");
    }

    [Fact]
    public async Task JoinGameAsGuest_WhenMaxGuestsReached_ShouldReturnBadRequest()
    {
        // Arrange - Create a game with 2 guests already
        var host = await Seeder.SeedUserAsync(u => u.WithExternalId("auth0|host"));
        var game = await Seeder.SeedGameAsync(g => g
            .WithHost(host.ExternalId)
            .WithGuest("Guest 1", "+33600000001", null)
            .WithGuest("Guest 2", "+33600000002", null));

        var request = new
        {
            Name = "Third Guest",
            PhoneNumber = "+33600000003",
            Email = (string?)null
        };

        // Act
        var response = await Client.PostAsJsonAsync($"/games/{game.Id}/players/guest", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Maximum 2 guests allowed per game");
    }

    [Fact]
    public async Task JoinGameAsGuest_WhenSamePhoneAlreadyJoined_ShouldReturnBadRequest()
    {
        // Arrange - Create a game with a guest
        var phoneNumber = "+33612345678";
        var host = await Seeder.SeedUserAsync(u => u.WithExternalId("auth0|host"));
        var game = await Seeder.SeedGameAsync(g => g
            .WithHost(host.ExternalId)
            .WithGuest("Existing Guest", phoneNumber, null));

        var request = new
        {
            Name = "New Guest",
            PhoneNumber = phoneNumber, // Same phone
            Email = (string?)null
        };

        // Act
        var response = await Client.PostAsJsonAsync($"/games/{game.Id}/players/guest", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("This phone number or email has already joined this game");
    }

    [Fact]
    public async Task JoinGameAsGuest_WhenSameEmailAlreadyJoined_ShouldReturnBadRequest()
    {
        // Arrange - Create a game with a guest
        var email = "guest@example.com";
        var host = await Seeder.SeedUserAsync(u => u.WithExternalId("auth0|host"));
        var game = await Seeder.SeedGameAsync(g => g
            .WithHost(host.ExternalId)
            .WithGuest("Existing Guest", null, email));

        var request = new
        {
            Name = "New Guest",
            PhoneNumber = (string?)null,
            Email = email // Same email
        };

        // Act
        var response = await Client.PostAsJsonAsync($"/games/{game.Id}/players/guest", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("This phone number or email has already joined this game");
    }

    [Fact]
    public async Task JoinGameAsGuest_ShouldUpdateGameStatus_WhenBecomingFull()
    {
        // Arrange - Create a game with 3/4 players
        var scenario = await Seeder.SeedCompleteGameAsync(
            participantCount: 2, // Host + 2 participants = 3/4
            configureGame: g => g.WithId(Guid.NewGuid()).WithMaxPlayers(4));

        var request = new
        {
            Name = "Last Guest",
            PhoneNumber = "+33612345678",
            Email = (string?)null
        };

        // Act
        var response = await Client.PostAsJsonAsync($"/games/{scenario.Game.Id}/players/guest", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify game status changed to Full
        var gameInDb = await Seeder.QueryGamesAsync(db =>
            db.Games.FirstOrDefaultAsync(g => g.Id == scenario.Game.Id));

        gameInDb.Should().NotBeNull();
        gameInDb!.CurrentPlayers.Should().Be(4);
        gameInDb.Status.Should().Be(GameStatus.Full);
    }

    // Response DTOs
    private record JoinGameAsGuestResponse(
        Guid GameId,
        Guid GuestParticipantId,
        string GuestName,
        int CurrentPlayers,
        string GameStatus,
        string Message
    );
}
