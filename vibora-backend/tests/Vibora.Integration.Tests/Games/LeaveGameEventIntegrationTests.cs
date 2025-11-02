using System.Net;
using FluentAssertions;
using Vibora.Games.Contracts.Events;
using Vibora.Integration.Tests.Infrastructure;

namespace Vibora.Integration.Tests.Games;

/// <summary>
/// Integration tests for integration events published when leaving a game
/// Uses MassTransit Test Harness to verify events are published correctly
/// </summary>
public class LeaveGameEventIntegrationTests : EventIntegrationTestBase
{
    public LeaveGameEventIntegrationTests(ViboraWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task LeaveGame_ShouldPublishParticipationRemovedIntegrationEvent()
    {
        // Arrange - Create a game with host and a participant
        var scenario = await Seeder.SeedCompleteGameAsync(
            participantCount: 1, // Host + 1 participant = 2 total
            configureGame: g => g.WithId(Guid.NewGuid()));

        var player = scenario.Participants.First();
        AuthenticateAs(player.ExternalId);

        // Act - Leave the game via HTTP endpoint
        var response = await Client.DeleteAsync($"/games/{scenario.Game.Id}/players?userExternalId={player.ExternalId}");

        // Assert - Verify HTTP response
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Wait for event with polling (no Task.Delay!)
        var eventReceived = await WaitForEventAsync<ParticipationRemovedEvent>(
            msg => msg.Context.Message.GameId == scenario.Game.Id && msg.Context.Message.UserExternalId == player.ExternalId,
            timeout: TimeSpan.FromSeconds(5));

        eventReceived.Should().BeTrue("ParticipationRemovedEvent should be published to the message bus");

        // Verify event details
        var publishedMessages = Harness.Published.Select<ParticipationRemovedEvent>().ToList();
        var ourEvent = publishedMessages
            .Select(m => m.Context.Message)
            .FirstOrDefault(e => e.GameId == scenario.Game.Id && e.UserExternalId == player.ExternalId);

        ourEvent.Should().NotBeNull("Should find the ParticipationRemovedEvent for our game");
        ourEvent!.UserName.Should().Be(player.Name);
        ourEvent.RemainingPlayers.Should().Be(1); // Only host remaining
        ourEvent.GameStatus.Should().Be("Open");
        ourEvent.RemovedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task LeaveGame_FromFullGame_ShouldPublishEventWithCorrectStatus()
    {
        // Arrange - Create a full game (4/4 players)
        var scenario = await Seeder.SeedCompleteGameAsync(
            participantCount: 3, // Host + 3 participants = 4 total (full)
            configureGame: g => g.WithId(Guid.NewGuid()).WithMaxPlayers(4));

        var player = scenario.Participants.First();
        AuthenticateAs(player.ExternalId);

        // Act - One player leaves
        var response = await Client.DeleteAsync($"/games/{scenario.Game.Id}/players?userExternalId={player.ExternalId}");

        // Assert - Verify HTTP response
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Wait for event with polling
        var eventReceived = await WaitForEventAsync<ParticipationRemovedEvent>(
            msg => msg.Context.Message.GameId == scenario.Game.Id && msg.Context.Message.UserExternalId == player.ExternalId,
            timeout: TimeSpan.FromSeconds(5));

        eventReceived.Should().BeTrue();

        // Verify event details
        var publishedMessages = Harness.Published.Select<ParticipationRemovedEvent>().ToList();
        var ourEvent = publishedMessages
            .Select(m => m.Context.Message)
            .FirstOrDefault(e => e.GameId == scenario.Game.Id && e.UserExternalId == player.ExternalId);

        ourEvent.Should().NotBeNull();
        ourEvent!.UserName.Should().Be(player.Name);
        ourEvent.RemainingPlayers.Should().Be(3); // 4 -> 3 players
        ourEvent.GameStatus.Should().Be("Open"); // Changed from Full to Open
    }
}
