using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Vibora.Games.Contracts.Events;
using Vibora.Integration.Tests.Infrastructure;
using Vibora.Notifications.Domain;

namespace Vibora.Integration.Tests.Notifications;

/// <summary>
/// End-to-end integration tests verifying notification system respects user preferences
/// Tests the complete flow: API Endpoint → Domain Event → Consumer → Database
///
/// REFACTORED: Uses EventIntegrationTestBase with WaitForEventAsync (no Task.Delay)
/// </summary>
public class NotificationWithUserPreferencesTests : EventIntegrationTestBase
{
    public NotificationWithUserPreferencesTests(ViboraWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task PlayerJoinsGame_ShouldCreateNotification_WhenHostHasDeviceTokenAndPushEnabled()
    {
        // Arrange - Create game with host who HAS device token and push enabled
        var gameId = Guid.NewGuid();
        var hostExternalId = "auth0|host-with-token";
        var playerExternalId = "auth0|player-1";

        // Seed host with notification settings
        var host = await Seeder.SeedUserAsync(u => u
            .WithExternalId(hostExternalId)
            .WithName("Host")
            .Intermediate());

        await Seeder.SeedNotificationSettingsAsync(
            hostExternalId,
            deviceToken: "valid-device-token-123",
            pushEnabled: true);

        // Seed game
        var game = await Seeder.SeedGameAsync(g => g
            .WithId(gameId)
            .WithHost(hostExternalId, "Host", "Intermediate"));

        // Seed player
        var player = await Seeder.SeedUserAsync(u => u
            .WithExternalId(playerExternalId)
            .WithName("PlayerOne")
            .Intermediate());

        AuthenticateAs(playerExternalId);

        var joinRequest = new
        {
            UserName = "PlayerOne",
            UserSkillLevel = "Intermediate",
            UserExternalId = playerExternalId
        };

        // Act - Player joins game → PlayerJoinedEvent published → PlayerJoinedEventConsumer triggered
        var response = await Client.PostAsJsonAsync($"/games/{gameId}/players", joinRequest);

        // Assert HTTP response
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Wait for event with polling (NO Task.Delay!)
        var eventReceived = await WaitForEventAsync<PlayerJoinedEvent>(
            msg => msg.Context.Message.GameId == gameId,
            timeout: TimeSpan.FromSeconds(5));

        eventReceived.Should().BeTrue("PlayerJoinedEvent should be published to the message bus");

        // Verify notification was created in database with polling
        // This proves the complete end-to-end flow: API → Domain Event → Integration Event → Consumer → DB
        Notification? notification = null;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        while (stopwatch.Elapsed < TimeSpan.FromSeconds(5) && notification == null)
        {
            notification = await Seeder.QueryNotificationsAsync(async db =>
            {
                return await db.Notifications
                    .Where(n => n.UserId == hostExternalId && n.Type == NotificationType.PlayerJoined)
                    .FirstOrDefaultAsync();
            });

            if (notification == null)
            {
                await Task.Delay(100);
            }
        }

        notification.Should().NotBeNull("Consumer should create notification for host with device token and push enabled");
        notification!.UserId.Should().Be(hostExternalId);
        notification.Type.Should().Be(NotificationType.PlayerJoined);
        notification.Channel.Should().Be(NotificationChannel.Push);
        notification.Content.Title.Should().NotBeNullOrEmpty();
        notification.Content.Body.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task PlayerJoinsGame_ShouldNotCreateNotification_WhenHostHasNoDeviceToken()
    {
        // Arrange - Create game with host who has NO device token
        var gameId = Guid.NewGuid();
        var hostExternalId = "auth0|host-no-token";
        var playerExternalId = "auth0|player-2";

        // Seed host with notification settings (NO device token)
        var host = await Seeder.SeedUserAsync(u => u
            .WithExternalId(hostExternalId)
            .WithName("Host")
            .Intermediate());

        await Seeder.SeedNotificationSettingsAsync(
            hostExternalId,
            deviceToken: null, // NO DEVICE TOKEN
            pushEnabled: true);

        // Seed game
        var game = await Seeder.SeedGameAsync(g => g
            .WithId(gameId)
            .WithHost(hostExternalId, "Host", "Intermediate"));

        // Seed player
        var player = await Seeder.SeedUserAsync(u => u
            .WithExternalId(playerExternalId)
            .WithName("PlayerTwo")
            .Intermediate());

        AuthenticateAs(playerExternalId);

        var joinRequest = new
        {
            UserName = "PlayerTwo",
            UserSkillLevel = "Intermediate",
            UserExternalId = playerExternalId
        };

        // Act - Player joins game
        var response = await Client.PostAsJsonAsync($"/games/{gameId}/players", joinRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Wait for event with polling
        var eventReceived = await WaitForEventAsync<PlayerJoinedEvent>(
            msg => msg.Context.Message.GameId == gameId,
            timeout: TimeSpan.FromSeconds(5));

        eventReceived.Should().BeTrue("PlayerJoinedEvent should be published even if host has no device token");

        // Verify NO notification was created (consumer should skip when no device token)
        var notification = await Seeder.QueryNotificationsAsync(async db =>
        {
            return await db.Notifications
                .Where(n => n.UserId == hostExternalId)
                .FirstOrDefaultAsync();
        });

        notification.Should().BeNull("Consumer should skip notification when host has no device token");
    }

    [Fact]
    public async Task PlayerJoinsGame_ShouldNotCreateNotification_WhenHostPushDisabled()
    {
        // Arrange - Create game with host who has push DISABLED
        var gameId = Guid.NewGuid();
        var hostExternalId = "auth0|host-push-disabled";
        var playerExternalId = "auth0|player-3";

        // Seed host with notification settings (push DISABLED)
        var host = await Seeder.SeedUserAsync(u => u
            .WithExternalId(hostExternalId)
            .WithName("Host")
            .Intermediate());

        await Seeder.SeedNotificationSettingsAsync(
            hostExternalId,
            deviceToken: "device-token-but-disabled",
            pushEnabled: false); // PUSH DISABLED

        // Seed game
        var game = await Seeder.SeedGameAsync(g => g
            .WithId(gameId)
            .WithHost(hostExternalId, "Host", "Intermediate"));

        // Seed player
        var player = await Seeder.SeedUserAsync(u => u
            .WithExternalId(playerExternalId)
            .WithName("PlayerThree")
            .Intermediate());

        AuthenticateAs(playerExternalId);

        var joinRequest = new
        {
            UserName = "PlayerThree",
            UserSkillLevel = "Intermediate",
            UserExternalId = playerExternalId
        };

        // Act - Player joins game
        var response = await Client.PostAsJsonAsync($"/games/{gameId}/players", joinRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Wait for event with polling
        var eventReceived = await WaitForEventAsync<PlayerJoinedEvent>(
            msg => msg.Context.Message.GameId == gameId,
            timeout: TimeSpan.FromSeconds(5));

        eventReceived.Should().BeTrue("PlayerJoinedEvent should be published even if host has push disabled");

        // Verify NO notification was created (consumer should skip when push disabled)
        var notification = await Seeder.QueryNotificationsAsync(async db =>
        {
            return await db.Notifications
                .Where(n => n.UserId == hostExternalId)
                .FirstOrDefaultAsync();
        });

        notification.Should().BeNull("Consumer should skip notification when host has push notifications disabled");
    }

    [Fact]
    public async Task CancelGame_WithParticipants_ShouldPublishGameCanceledEvent()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var hostExternalId = "auth0|cancel-host";
        var participant1 = "auth0|participant-1";

        // Seed host and participant
        var host = await Seeder.SeedUserAsync(u => u
            .WithExternalId(hostExternalId)
            .WithName("Host")
            .Intermediate());

        var p1 = await Seeder.SeedUserAsync(u => u
            .WithExternalId(participant1)
            .WithName("P1")
            .Intermediate());

        // Seed game with participant
        var game = await Seeder.SeedGameAsync(g => g
            .WithId(gameId)
            .WithHost(hostExternalId, "Host", "Intermediate")
            .WithParticipant(participant1, "P1", "Intermediate"));

        AuthenticateAs(hostExternalId);

        // Act - Cancel game
        var response = await Client.PostAsync($"/games/{gameId}/cancel?hostExternalId={hostExternalId}", null);

        // Assert HTTP response
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Wait for GameCanceledEvent with polling
        var eventReceived = await WaitForEventAsync<GameCanceledEvent>(
            msg => msg.Context.Message.GameId == gameId,
            timeout: TimeSpan.FromSeconds(5));

        eventReceived.Should().BeTrue("GameCanceledEvent should be published to the message bus");

        // Verify event details
        var publishedMessages = Harness.Published.Select<GameCanceledEvent>().ToList();
        var ourEvent = publishedMessages
            .Select(m => m.Context.Message)
            .FirstOrDefault(e => e.GameId == gameId);

        ourEvent.Should().NotBeNull();
        ourEvent!.Participants.Should().HaveCount(1, "game has one non-host participant");
        ourEvent.Participants[0].UserExternalId.Should().Be(participant1);

        // NOTE: Consumer batch processing behavior (filtering by device tokens/preferences)
        // should be tested in consumer unit tests
    }

    [Fact]
    public async Task CancelGame_ShouldCreateNotificationForParticipantWithDeviceToken()
    {
        // Arrange - Create game with participant who has device token + push enabled
        var gameId = Guid.NewGuid();
        var hostExternalId = "auth0|cancel-host-2";
        var participantExternalId = "auth0|participant-with-token-2";

        // Seed host
        var host = await Seeder.SeedUserAsync(u => u
            .WithExternalId(hostExternalId)
            .WithName("Host")
            .Intermediate());

        // Seed participant with notification settings
        var participant = await Seeder.SeedUserAsync(u => u
            .WithExternalId(participantExternalId)
            .WithName("Participant")
            .Intermediate());

        await Seeder.SeedNotificationSettingsAsync(
            participantExternalId,
            deviceToken: "valid-token-participant",
            pushEnabled: true);

        // Seed game with participant
        var game = await Seeder.SeedGameAsync(g => g
            .WithId(gameId)
            .WithHost(hostExternalId, "Host", "Intermediate")
            .WithParticipant(participantExternalId, "Participant", "Intermediate"));

        AuthenticateAs(hostExternalId);

        // Act - Cancel game → GameCanceledEvent published → GameCanceledEventConsumer processes
        var response = await Client.PostAsync($"/games/{gameId}/cancel?hostExternalId={hostExternalId}", null);

        // Assert HTTP response
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Wait for GameCanceledEvent with polling
        var eventReceived = await WaitForEventAsync<GameCanceledEvent>(
            msg => msg.Context.Message.GameId == gameId,
            timeout: TimeSpan.FromSeconds(5));

        eventReceived.Should().BeTrue("GameCanceledEvent should be published to the message bus");

        // Verify notification was created in database for the participant with polling
        // This proves the complete end-to-end flow for game cancellation
        Notification? notification = null;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        while (stopwatch.Elapsed < TimeSpan.FromSeconds(5) && notification == null)
        {
            notification = await Seeder.QueryNotificationsAsync(async db =>
            {
                return await db.Notifications
                    .Where(n => n.UserId == participantExternalId && n.Type == NotificationType.GameCancelled)
                    .FirstOrDefaultAsync();
            });

            if (notification == null)
            {
                await Task.Delay(100);
            }
        }

        notification.Should().NotBeNull("Consumer should create notification for participant with device token");
        notification!.UserId.Should().Be(participantExternalId);
        notification.Type.Should().Be(NotificationType.GameCancelled);
        notification.Channel.Should().Be(NotificationChannel.Push);
        notification.Content.Title.Should().NotBeNullOrEmpty();
        notification.Content.Body.Should().NotBeNullOrEmpty();
    }
}
