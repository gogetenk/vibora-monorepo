using System.Net;
using FluentAssertions;
using Vibora.Games.Contracts.Events;
using Vibora.Integration.Tests.Infrastructure;

namespace Vibora.Integration.Tests.Games;

/// <summary>
/// Integration tests for integration events published when canceling a game
/// Uses MassTransit Test Harness to verify events are published correctly
/// </summary>
public class CancelGameEventIntegrationTests : EventIntegrationTestBase
{
    public CancelGameEventIntegrationTests(ViboraWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task CancelGame_ShouldPublishGameCanceledIntegrationEvent()
    {
        // Arrange - Create a game with host
        var (host, game) = await Seeder.SeedGameWithHostAsync("auth0|host");

        AuthenticateAs(host.ExternalId);

        // Act - Cancel the game via HTTP endpoint
        var response = await Client.PostAsync($"/games/{game.Id}/cancel?hostExternalId={host.ExternalId}", null);

        // Assert - Verify HTTP response
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Wait for event with polling (no Task.Delay!)
        var eventReceived = await WaitForEventAsync<GameCanceledEvent>(
            msg => msg.Context.Message.GameId == game.Id && msg.Context.Message.HostExternalId == host.ExternalId,
            timeout: TimeSpan.FromSeconds(5));

        eventReceived.Should().BeTrue("GameCanceledEvent should be published to the message bus");

        // Verify event details
        var publishedMessages = Harness.Published.Select<GameCanceledEvent>().ToList();
        var ourEvent = publishedMessages
            .Select(m => m.Context.Message)
            .FirstOrDefault(e => e.GameId == game.Id && e.HostExternalId == host.ExternalId);

        ourEvent.Should().NotBeNull("Should find the GameCanceledEvent for our game");
        ourEvent!.Location.Should().Be("Test Club");
        ourEvent.TotalParticipants.Should().Be(1); // Only host
        ourEvent.CanceledAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task CancelGame_WithMultipleParticipants_ShouldPublishEventWithCorrectCount()
    {
        // Arrange - Create a game with multiple participants
        var scenario = await Seeder.SeedCompleteGameAsync(
            participantCount: 2, // Host + 2 participants = 3 total
            configureGame: g => g.WithId(Guid.NewGuid()));

        AuthenticateAs(scenario.Host.ExternalId);

        // Act - Host cancels the game
        var response = await Client.PostAsync($"/games/{scenario.Game.Id}/cancel?hostExternalId={scenario.Host.ExternalId}", null);

        // Assert - Verify HTTP response
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Wait for event with polling
        var eventReceived = await WaitForEventAsync<GameCanceledEvent>(
            msg => msg.Context.Message.GameId == scenario.Game.Id && msg.Context.Message.HostExternalId == scenario.Host.ExternalId,
            timeout: TimeSpan.FromSeconds(5));

        eventReceived.Should().BeTrue();

        // Verify event details
        var publishedMessages = Harness.Published.Select<GameCanceledEvent>().ToList();
        var ourEvent = publishedMessages
            .Select(m => m.Context.Message)
            .FirstOrDefault(e => e.GameId == scenario.Game.Id && e.HostExternalId == scenario.Host.ExternalId);

        ourEvent.Should().NotBeNull();
        ourEvent!.TotalParticipants.Should().Be(3); // Host + 2 players
        ourEvent.Location.Should().Be("Test Club");
    }
}
