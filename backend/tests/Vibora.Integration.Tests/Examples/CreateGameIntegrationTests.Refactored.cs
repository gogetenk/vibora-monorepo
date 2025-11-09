using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Vibora.Integration.Tests.Infrastructure;

namespace Vibora.Integration.Tests.Examples;

/// <summary>
/// EXAMPLE: Refactored version of CreateGameIntegrationTests
/// Demonstrates best practices with builders, seeder, and clean API
/// 
/// IMPROVEMENTS:
/// - Uses GameBuilder and UserBuilder (no duplication)
/// - Uses TestDataSeeder for consistent seeding
/// - Uses HttpClientExtensions for clean deserialization
/// - Uses AuthenticateAs() helper (no manual token handling)
/// - Reduced from 116 lines to ~60 lines
/// </summary>
public class CreateGameIntegrationTestsRefactored : IntegrationTestBaseImproved
{
    public CreateGameIntegrationTestsRefactored(ViboraWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task CreateGame_WithValidData_ShouldReturnCreatedGame()
    {
        // Arrange - Seed user using builder (1 line instead of 10)
        var host = await Seeder.SeedUserAsync(u => u
            .WithExternalId("auth0|test-user-123")
            .WithName("Test User")
            .Intermediate());

        var request = new
        {
            hostExternalId = host.ExternalId,
            dateTime = DateTime.UtcNow.AddDays(2),
            location = "Club de Padel Paris",
            skillLevel = "Intermediate",
            maxPlayers = 4,
            notes = "Bring your own racket"
        };

        // Authenticate using helper (1 line instead of 3)
        AuthenticateAs(host.ExternalId);

        // Act
        var response = await Client.PostAsJsonAsync("/games", request);

        // Assert - Clean deserialization (1 line instead of 6)
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var gameResult = await response.ReadAsAsync<GameCreatedResponse>();

        gameResult.Should().NotBeNull();
        gameResult.Id.Should().NotBeEmpty();
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

        // Verify data persisted (using Seeder query helper)
        var gameInDb = await Seeder.QueryGamesAsync(db =>
            db.Games
                .Include(g => g.Participations)
                .FirstOrDefaultAsync(g => g.Id == gameResult.Id)
        );

        gameInDb.Should().NotBeNull();
        gameInDb!.HostExternalId.Should().Be(host.ExternalId);
        gameInDb.CurrentPlayers.Should().Be(1);
        gameInDb.Participations.Should().HaveCount(1);
    }

    [Fact]
    public async Task CreateGame_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new
        {
            hostExternalId = "auth0|test-user",
            dateTime = DateTime.UtcNow.AddDays(2),
            location = "Test Club",
            skillLevel = "Intermediate",
            maxPlayers = 4
        };

        // No authentication

        // Act
        var response = await Client.PostAsJsonAsync("/games", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateGame_WithNonExistentHost_ShouldReturnError()
    {
        // Arrange - No user seeded
        var request = new
        {
            hostExternalId = "auth0|non-existent",
            dateTime = DateTime.UtcNow.AddDays(2),
            location = "Test Club",
            skillLevel = "Intermediate",
            maxPlayers = 4
        };

        AuthenticateAs("auth0|non-existent");

        // Act
        var response = await Client.PostAsJsonAsync("/games", request);

        // Assert
        // API can return 400 (validation), 404 (not found), or 422 (business rule)
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest, 
            HttpStatusCode.NotFound, 
            HttpStatusCode.UnprocessableEntity);
        
        response.IsSuccessStatusCode.Should().BeFalse("Creating a game with non-existent host should fail");
    }

    // Response DTOs
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

    private record ParticipantResponse(
        string ExternalId,
        string Name,
        string SkillLevel
    );
}
