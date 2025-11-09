using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Vibora.Integration.Tests.Infrastructure;

namespace Vibora.Integration.Tests.Games;

public class CreateGameIntegrationTests : IntegrationTestBaseImproved
{
    public CreateGameIntegrationTests(ViboraWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task CreateGame_WithValidData_ShouldReturnCreatedGame()
    {
        // Arrange - Seed a test user in the database
        var host = await Seeder.SeedUserAsync(u => u
            .WithExternalId("auth0|test-user-123")
            .WithName("Test User")
            .Intermediate());

        var request = new
        {
            hostExternalId = host.ExternalId,
            dateTime = DateTime.UtcNow.AddDays(2),
            location = "Club de Padel Paris",
            skillLevel = 5, // Intermediate = 5 (int, not string "Intermediate")
            maxPlayers = 4
        };

        AuthenticateAs(host.ExternalId);

        // Act - Call the API endpoint
        var response = await Client.PostAsJsonAsync("/games", request);

        // Assert - Check response status
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        // Assert - Check response body
        var gameResult = await response.ReadAsAsync<GameCreatedResponse>();

        gameResult.Should().NotBeNull();
        gameResult!.Id.Should().NotBeEmpty();
        gameResult.HostExternalId.Should().Be(host.ExternalId);
        gameResult.Location.Should().Be("Club de Padel Paris");
        gameResult.SkillLevel.Should().Be("Intermediate");
        gameResult.MaxPlayers.Should().Be(4);
        gameResult.CurrentPlayers.Should().Be(1); // Host auto-joined
        gameResult.Participants.Should().HaveCount(1);

        var hostParticipant = gameResult.Participants.First();
        hostParticipant.ExternalId.Should().Be(host.ExternalId);
        hostParticipant.Name.Should().Be("Test User");
        hostParticipant.SkillLevel.Should().Be("Intermediate");

        // Assert - Verify data is persisted in database
        var gameInDb = await Seeder.QueryGamesAsync(db =>
            db.Games
                .Include(g => g.Participations)
                .FirstOrDefaultAsync(g => g.Id == gameResult.Id)
        );

        gameInDb.Should().NotBeNull();
        gameInDb!.HostExternalId.Should().Be(host.ExternalId);
        gameInDb.Location.Should().Be("Club de Padel Paris");
        gameInDb.CurrentPlayers.Should().Be(1);
        gameInDb.Participations.Should().HaveCount(1);
    }

    // Response DTOs matching the API contract
    private record GameCreatedResponse(
        Guid Id,
        DateTime DateTime,
        string Location,
        string SkillLevel,
        int MaxPlayers,
        string HostExternalId,
        int CurrentPlayers,
        List<ParticipantResponse> Participants
    );

    [Fact]
    public async Task POST_games_WithGpsCoordinates_ShouldCreateGameSuccessfully()
    {
        // Arrange - Seed a test user
        var host = await Seeder.SeedUserAsync(u => u
            .WithExternalId("auth0|gps-test-user")
            .WithName("GPS Test User")
            .Advanced());

        var request = new
        {
            hostExternalId = host.ExternalId,
            dateTime = DateTime.UtcNow.AddHours(5),
            location = "Casa Padel with GPS",
            skillLevel = "Advanced",
            maxPlayers = 4,
            latitude = 48.8424,
            longitude = 2.2894,
            notes = "Game with GPS coordinates"
        };

        AuthenticateAs(host.ExternalId);

        // Act - Create game with GPS coordinates
        var response = await Client.PostAsJsonAsync("/games", request);

        // Assert - Check response status
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        // Assert - Check response body
        var game = await response.ReadAsAsync<GameCreatedWithGpsResponse>();
        game.Should().NotBeNull();
        game!.Id.Should().NotBeEmpty();
        game.Latitude.Should().Be(48.8424);
        game.Longitude.Should().Be(2.2894);
        game.Location.Should().Be("Casa Padel with GPS");

        // Assert - Verify data is persisted in database
        var gameInDb = await Seeder.QueryGamesAsync(db =>
            db.Games.FirstOrDefaultAsync(g => g.Id == game.Id)
        );

        gameInDb.Should().NotBeNull();
        gameInDb!.Latitude.Should().Be(48.8424);
        gameInDb.Longitude.Should().Be(2.2894);
    }

    private record ParticipantResponse(
        string ExternalId,
        string Name,
        string SkillLevel
    );

    private record GameCreatedWithGpsResponse(
        Guid Id,
        DateTime DateTime,
        string Location,
        string SkillLevel,
        int MaxPlayers,
        string HostExternalId,
        int CurrentPlayers,
        double? Latitude,
        double? Longitude,
        List<ParticipantResponse> Participants
    );
}
