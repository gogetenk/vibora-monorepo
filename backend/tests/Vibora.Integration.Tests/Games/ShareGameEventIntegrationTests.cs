using System.Net;
using FluentAssertions;
using Vibora.Games.Contracts.Events;
using Vibora.Integration.Tests.Infrastructure;

namespace Vibora.Integration.Tests.Games;

/// <summary>
/// Integration tests for integration events published when sharing a game
/// Uses MassTransit Test Harness to verify events are published correctly
/// </summary>
public class ShareGameEventIntegrationTests : EventIntegrationTestBase
{
    public ShareGameEventIntegrationTests(ViboraWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task CreateGameShare_ShouldPublishGameSharedIntegrationEvent()
    {
        // Arrange - Create a game
        var (host, game) = await Seeder.SeedGameWithHostAsync("auth0|user123");

        AuthenticateAs(host.ExternalId);

        // Act - Create a share link
        var response = await Client.PostAsync($"/games/{game.Id}/shares?sharedByUserExternalId={host.ExternalId}", null);

        // Assert - Verify HTTP response
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        // Wait for event with polling (no Task.Delay!)
        var eventReceived = await WaitForEventAsync<GameSharedEvent>(
            msg => msg.Context.Message.GameId == game.Id && msg.Context.Message.SharedByUserExternalId == host.ExternalId,
            timeout: TimeSpan.FromSeconds(5));

        eventReceived.Should().BeTrue("GameSharedEvent should be published to the message bus");

        // Verify event details
        var publishedMessages = Harness.Published.Select<GameSharedEvent>().ToList();
        var ourEvent = publishedMessages
            .Select(m => m.Context.Message)
            .FirstOrDefault(e => e.GameId == game.Id && e.SharedByUserExternalId == host.ExternalId);

        ourEvent.Should().NotBeNull("Should find the GameSharedEvent for our game");
        ourEvent!.ShareToken.Should().NotBeNullOrEmpty();
        ourEvent.ShareToken.Length.Should().Be(8);
        ourEvent.SharedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task CreateGameShare_MultipleTimes_ShouldPublishMultipleEvents()
    {
        // Arrange - Create a game
        var (host, game) = await Seeder.SeedGameWithHostAsync("auth0|user123");

        // Seed second user
        var user2 = await Seeder.SeedUserAsync(u => u
            .WithExternalId("auth0|user456")
            .WithName("User 456")
            .Intermediate());

        // Act - Create multiple share links (different users can share the same game)
        AuthenticateAs(host.ExternalId);
        var response1 = await Client.PostAsync($"/games/{game.Id}/shares?sharedByUserExternalId={host.ExternalId}", null);

        AuthenticateAs(user2.ExternalId);
        var response2 = await Client.PostAsync($"/games/{game.Id}/shares?sharedByUserExternalId={user2.ExternalId}", null);

        // Assert - Verify HTTP responses
        response1.StatusCode.Should().Be(HttpStatusCode.Created);
        response2.StatusCode.Should().Be(HttpStatusCode.Created);

        // Wait for both events with polling
        var event1Received = await WaitForEventAsync<GameSharedEvent>(
            msg => msg.Context.Message.GameId == game.Id && msg.Context.Message.SharedByUserExternalId == host.ExternalId,
            timeout: TimeSpan.FromSeconds(5));

        var event2Received = await WaitForEventAsync<GameSharedEvent>(
            msg => msg.Context.Message.GameId == game.Id && msg.Context.Message.SharedByUserExternalId == user2.ExternalId,
            timeout: TimeSpan.FromSeconds(5));

        event1Received.Should().BeTrue();
        event2Received.Should().BeTrue();

        // Verify multiple integration events were published
        var publishedMessages = Harness.Published.Select<GameSharedEvent>().ToList();

        // Find events for our specific game
        var ourEvents = publishedMessages
            .Select(m => m.Context.Message)
            .Where(e => e.GameId == game.Id)
            .ToList();

        ourEvents.Should().HaveCountGreaterThanOrEqualTo(2, "Should have at least 2 GameSharedEvent for the game");
        ourEvents.Should().Contain(e => e.SharedByUserExternalId == host.ExternalId);
        ourEvents.Should().Contain(e => e.SharedByUserExternalId == user2.ExternalId);
    }
}
