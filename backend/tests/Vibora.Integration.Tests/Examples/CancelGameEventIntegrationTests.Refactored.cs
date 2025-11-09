using System.Net;
using FluentAssertions;
using Vibora.Games.Domain;
using Vibora.Games.Contracts.Events;
using Vibora.Integration.Tests.Infrastructure;
using Vibora.Users.Domain;

namespace Vibora.Integration.Tests.Examples;

/// <summary>
/// EXAMPLE: Refactored version of CancelGameEventIntegrationTests
/// Demonstrates best practices with EventIntegrationTestBase
/// 
/// IMPROVEMENTS:
/// - Extends EventIntegrationTestBase (auto Start/Stop harness)
/// - Uses GameBuilder for game creation (no duplication)
/// - Uses WaitForEventAsync() with polling (no Task.Delay)
/// - Uses TestDataSeeder (consistent seeding)
/// - No try/finally blocks (handled by base class)
/// - Reduced from 181 lines to ~80 lines
/// </summary>
public class CancelGameEventIntegrationTestsRefactored : EventIntegrationTestBase
{
    public CancelGameEventIntegrationTestsRefactored(ViboraWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task CancelGame_ShouldPublishGameCanceledIntegrationEvent()
    {
        // Arrange - Seed game using builder (1 line instead of 30+)
        var (host, game) = await Seeder.SeedGameWithHostAsync("auth0|host");

        AuthenticateAs(host.ExternalId);

        // Act - Cancel the game
        var response = await Client.PostAsync($"/games/{game.Id}/cancel?hostExternalId={host.ExternalId}", null);

        // Assert - HTTP response
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Wait for event with polling (no Task.Delay!)
        var eventReceived = await WaitForEventAsync<GameCanceledEvent>(
            msg => msg.Context.Message.GameId == game.Id,
            timeout: TimeSpan.FromSeconds(5));

        eventReceived.Should().BeTrue("GameCanceledEvent should be published to the message bus");

        // Verify event details
        var publishedMessages = Harness.Published.Select<GameCanceledEvent>().ToList();
        var ourEvent = publishedMessages
            .Select(m => m.Context.Message)
            .FirstOrDefault(e => e.GameId == game.Id);

        ourEvent.Should().NotBeNull();
        ourEvent!.HostExternalId.Should().Be(host.ExternalId);
        ourEvent.TotalParticipants.Should().Be(1); // Only host
        ourEvent.CanceledAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task CancelGame_WithMultipleParticipants_ShouldPublishEventWithCorrectCount()
    {
        // Arrange - Seed game with 3 participants using builder
        var scenario = await Seeder.SeedCompleteGameAsync(
            participantCount: 2, // Host + 2 participants = 3 total
            configureGame: g => g.WithId(Guid.NewGuid()));

        AuthenticateAs(scenario.Host.ExternalId);

        // Act - Host cancels the game
        var response = await Client.PostAsync(
            $"/games/{scenario.Game.Id}/cancel?hostExternalId={scenario.Host.ExternalId}", 
            null);

        // Assert - HTTP response
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Wait for event with polling
        var eventReceived = await WaitForEventAsync<GameCanceledEvent>(
            msg => msg.Context.Message.GameId == scenario.Game.Id,
            timeout: TimeSpan.FromSeconds(5));

        eventReceived.Should().BeTrue();

        // Verify event details
        var publishedMessages = Harness.Published.Select<GameCanceledEvent>().ToList();
        var ourEvent = publishedMessages
            .Select(m => m.Context.Message)
            .FirstOrDefault(e => e.GameId == scenario.Game.Id);

        ourEvent.Should().NotBeNull();
        ourEvent!.TotalParticipants.Should().Be(3); // Host + 2 players
    }

    [Fact]
    public async Task CancelGame_WhenNotHost_ShouldReturnForbidden()
    {
        // Arrange
        var (host, game) = await Seeder.SeedGameWithHostAsync("auth0|host");
        var otherUser = await Seeder.SeedUserAsync(u => u.WithExternalId("auth0|other-user"));

        AuthenticateAs(otherUser.ExternalId); // Not the host

        // Record published events count BEFORE the action
        var eventCountBefore = Harness.Published.Select<GameCanceledEvent>().Count();

        // Act
        var response = await Client.PostAsync(
            $"/games/{game.Id}/cancel?hostExternalId={otherUser.ExternalId}", 
            null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Verify NO NEW event was published (count should be unchanged)
        var eventCountAfter = Harness.Published.Select<GameCanceledEvent>().Count();
        eventCountAfter.Should().Be(eventCountBefore, "No new event should be published for failed cancellation");
    }

    [Fact]
    public async Task CancelGame_AlreadyCanceled_ShouldReturnError()
    {
        // Arrange - Create already canceled game using builder
        var gameId = Guid.NewGuid();
        var host = await Seeder.SeedUserAsync(u => u.WithExternalId("auth0|host"));
        var game = await Seeder.SeedGameAsync(g => g
            .WithId(gameId)
            .WithHost(host.ExternalId)
            .Canceled()); // Already canceled

        AuthenticateAs(host.ExternalId);

        // Act - Try to cancel again
        var response = await Client.PostAsync(
            $"/games/{game.Id}/cancel?hostExternalId={host.ExternalId}", 
            null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var errorBody = await response.ReadErrorAsync();
        errorBody.Should().Contain("already canceled");
    }
}
