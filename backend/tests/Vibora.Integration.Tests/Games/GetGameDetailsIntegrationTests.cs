using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Vibora.Integration.Tests.Infrastructure;

namespace Vibora.Integration.Tests.Games;

public class GetGameDetailsIntegrationTests : IntegrationTestBaseImproved
{
    public GetGameDetailsIntegrationTests(ViboraWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetGameDetails_WithValidId_ShouldReturnGameWithParticipants()
    {
        // Arrange - Create a game with multiple participants
        var scenario = await Seeder.SeedCompleteGameAsync(
            participantCount: 1, // Host + 1 participant = 2 total
            configureGame: g => g
                .WithId(Guid.NewGuid())
                .AtLocation("Club Test Paris"));

        // Act
        var response = await Client.GetAsync($"/games/{scenario.Game.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.ReadAsAsync<GameDetailsResponse>();
        result.Should().NotBeNull();
        result!.Id.Should().Be(scenario.Game.Id);
        result.Location.Should().Be("Club Test Paris");
        result.SkillLevel.Should().Be("Intermediate");
        result.MaxPlayers.Should().Be(4);
        result.CurrentPlayers.Should().Be(2);
        result.Status.Should().Be("Open");

        // Verify participants
        result.Participants.Should().HaveCount(2);

        // Host should be first
        var host = result.Participants.FirstOrDefault(p => p.IsHost);
        host.Should().NotBeNull();
        host!.Type.Should().Be("User");
        host.Identifier.Should().Be(scenario.Host.ExternalId);
        host.DisplayName.Should().Be(scenario.Host.Name);
        host.SkillLevel.Should().Be(scenario.Host.SkillLevel.ToString());

        // Regular participant
        var participant = result.Participants.FirstOrDefault(p => !p.IsHost);
        participant.Should().NotBeNull();
    }

    [Fact]
    public async Task GetGameDetails_WithNonExistentId_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/games/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetGameDetails_WithEmptyGuid_ShouldReturnBadRequest()
    {
        // Arrange
        var emptyGuid = Guid.Empty;

        // Act
        var response = await Client.GetAsync($"/games/{emptyGuid}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetGameDetails_FullGame_ShouldReturnCorrectStatus()
    {
        // Arrange - Create a full game (4/4 players)
        var scenario = await Seeder.SeedCompleteGameAsync(
            participantCount: 3, // Host + 3 participants = 4 total (full)
            configureGame: g => g
                .WithId(Guid.NewGuid())
                .WithMaxPlayers(4));

        // Act
        var response = await Client.GetAsync($"/games/{scenario.Game.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.ReadAsAsync<GameDetailsResponse>();
        result.Should().NotBeNull();
        result!.Status.Should().Be("Full");
        result.CurrentPlayers.Should().Be(4);
        result.MaxPlayers.Should().Be(4);
        result.Participants.Should().HaveCount(4);
    }

    [Fact]
    public async Task GetGameDetails_WithGuestParticipants_ReturnsAllParticipantsWithCorrectTypes()
    {
        // Arrange - Create a game with host
        var scenario = await Seeder.SeedCompleteGameAsync(
            participantCount: 0,
            configureGame: g => g
                .WithId(Guid.NewGuid())
                .AtLocation("Test Club"));

        // Add a regular participant
        var regularParticipant = await Seeder.SeedUserAsync(u => u
            .WithExternalId("auth0|participant")
            .WithName("Regular Player")
            .Intermediate());

        // Authenticate as the regular participant to join the game
        AuthenticateAs(regularParticipant.ExternalId);
        var joinResponse = await Client.PostAsync($"/games/{scenario.Game.Id}/players", 
            JsonContent.Create(new
            {
                userExternalId = regularParticipant.ExternalId,
                userName = regularParticipant.Name,
                userSkillLevel = regularParticipant.SkillLevel.ToString()
            }));
        joinResponse.EnsureSuccessStatusCode();

        // Add two guest participants
        var guest1Response = await Client.PostAsync($"/games/{scenario.Game.Id}/players/guest", 
            JsonContent.Create(new
            {
                name = "John Doe",
                phoneNumber = "+33612345678"
            }));
        guest1Response.EnsureSuccessStatusCode();

        var guest2Response = await Client.PostAsync($"/games/{scenario.Game.Id}/players/guest", 
            JsonContent.Create(new
            {
                name = "Jane Smith",
                email = "jane@example.com"
            }));
        guest2Response.EnsureSuccessStatusCode();

        // Act
        var response = await Client.GetAsync($"/games/{scenario.Game.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.ReadAsAsync<GameDetailsResponse>();
        result.Should().NotBeNull();
        result!.Participants.Should().HaveCount(4); // Host + 1 user + 2 guests

        // Verify types
        result.Participants.Should().Contain(p => p.Type == "User");
        result.Participants.Should().Contain(p => p.Type == "Guest");

        // Verify guest participants have correct properties
        var guestParticipants = result.Participants.Where(p => p.Type == "Guest").ToList();
        guestParticipants.Should().HaveCount(2);
        
        var johnDoe = guestParticipants.FirstOrDefault(g => g.DisplayName == "John Doe");
        johnDoe.Should().NotBeNull();
        johnDoe!.ParticipationId.Should().BeNull();
        johnDoe.Identifier.Should().Be("Guest: John Doe");
        johnDoe.ContactInfo.Should().Be("+33612345678");
        johnDoe.SkillLevel.Should().BeNull();
        johnDoe.IsHost.Should().BeFalse();

        var janeSmith = guestParticipants.FirstOrDefault(g => g.DisplayName == "Jane Smith");
        janeSmith.Should().NotBeNull();
        janeSmith!.ParticipationId.Should().BeNull();
        janeSmith.Identifier.Should().Be("Guest: Jane Smith");
        janeSmith.ContactInfo.Should().Be("jane@example.com");
        janeSmith.SkillLevel.Should().BeNull();

        // Verify host is first
        result.Participants[0].IsHost.Should().BeTrue();
        result.Participants[0].Type.Should().Be("User");
    }

    // Response DTOs
    private record GameDetailsResponse(
        Guid Id,
        DateTime DateTime,
        string Location,
        string SkillLevel,
        int MaxPlayers,
        int CurrentPlayers,
        string HostExternalId,
        string Status,
        DateTime CreatedAt,
        List<ParticipantResponse> Participants
    );

    private record ParticipantResponse(
        string Type,
        Guid? ParticipationId,
        string Identifier,
        string DisplayName,
        string? SkillLevel,
        string? ContactInfo,
        bool IsHost,
        DateTime JoinedAt
    );
}
