using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Vibora.Integration.Tests.Infrastructure;
using Xunit;

namespace Vibora.Integration.Tests.Users;

/// <summary>
/// Integration tests for automatic guest to account reconciliation
/// 
/// Tests the complete flow:
/// 1. Guest joins a game with phone/email
/// 2. Guest signs up via Auth0/Supabase (webhook)
/// 3. System automatically reconciles guest participations
/// 4. User sees their games without manual action (invisible UX)
/// </summary>
public class GuestToAccountReconciliationTests : IntegrationTestBaseImproved
{
    public GuestToAccountReconciliationTests(ViboraWebApplicationFactory factory) 
        : base(factory)
    {
    }

    [Fact]
    public async Task GuestJoinsGame_ThenSignsUp_ShouldAutomaticallyReconcileParticipations()
    {
        // Arrange
        const string guestName = "Alice Guest";
        const string guestPhone = "+33123456789";
        const string userExternalId = "auth0|alice123";

        // Step 1: Create a game (as host)
        var (host, game) = await Seeder.SeedGameWithHostAsync("auth0|host123");

        // Step 2: Guest joins the game with phone number
        var joinAsGuestResponse = await Client.PostAsJsonAsync($"/games/{game.Id}/players/guest", new
        {
            Name = guestName,
            PhoneNumber = guestPhone,
            Email = (string?)null
        });

        joinAsGuestResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var joinResult = await joinAsGuestResponse.ReadAsAsync<JoinGameAsGuestResponse>();
        var guestParticipantId = joinResult!.GuestParticipantId;

        // Step 3: Verify guest user was created in Users module
        var guestUser = await Seeder.QueryUsersAsync(db =>
            db.Users.FirstOrDefaultAsync(u => u.IsGuest && u.PhoneNumber == guestPhone));
        
        guestUser.Should().NotBeNull();
        guestUser!.Name.Should().Be(guestName);
        guestUser.PhoneNumber.Should().Be(guestPhone);

        // Step 4: Simulate user signup via Auth0 webhook
        var syncResponse = await Client.PostAsJsonAsync("/users/sync", new
        {
            ExternalId = userExternalId,
            Name = "Alice Smith",
            SkillLevel = "Intermediate",
            PhoneNumber = guestPhone, // 🔑 KEY: Same phone for matching
            Email = "alice@example.com"
        });

        syncResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var syncResult = await syncResponse.ReadAsAsync<SyncUserFromAuthResponse>();
        syncResult!.IsNewUser.Should().BeTrue();

        // Step 5: Verify guest participations were automatically converted
        var gameWithParticipants = await Seeder.QueryGamesAsync(db =>
            db.Games
                .Include(g => g.Participations)
                .Include(g => g.GuestParticipants)
                .FirstOrDefaultAsync(g => g.Id == game.Id));
        
        // Check that guest participant was converted to regular participant
        var regularParticipant = gameWithParticipants!.Participations
            .FirstOrDefault(p => p.UserExternalId == userExternalId);
        
        regularParticipant.Should().NotBeNull();
        regularParticipant!.UserName.Should().Be("Alice Smith");
        
        // Check that guest participant no longer exists
        var guestParticipant = gameWithParticipants.GuestParticipants
            .FirstOrDefault(gp => gp.Id == guestParticipantId);
        
        guestParticipant.Should().BeNull("Guest participant should be converted to regular participant");

        // Step 6: Verify user can see their games (UX validation)
        Client.WithUser(userExternalId);
        var myGamesResponse = await Client.GetAsync("/games/me");
        myGamesResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var myGames = await myGamesResponse.ReadAsAsync<MyGamesResponse>();
        myGames!.Games.Should().HaveCount(1);
        myGames.Games.First().Id.Should().Be(game.Id);
    }

    [Fact]
    public async Task GuestJoinsGame_WithEmail_ThenSignsUp_ShouldAutomaticallyReconcile()
    {
        // Arrange
        const string guestName = "Bob Guest";
        const string guestEmail = "bob.guest@example.com";
        const string userExternalId = "auth0|bob456";

        // Step 1: Create a game
        var (host, game) = await Seeder.SeedGameWithHostAsync("auth0|host456");

        // Step 2: Guest joins with email
        var joinAsGuestResponse = await Client.PostAsJsonAsync($"/games/{game.Id}/players/guest", new
        {
            Name = guestName,
            PhoneNumber = (string?)null,
            Email = guestEmail
        });

        joinAsGuestResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Step 3: User signs up with same email
        var syncResponse = await Client.PostAsJsonAsync("/users/sync", new
        {
            ExternalId = userExternalId,
            Name = "Bob Johnson",
            SkillLevel = "Beginner",
            PhoneNumber = (string?)null,
            Email = guestEmail // 🔑 KEY: Same email for matching
        });

        syncResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Step 4: Verify reconciliation happened
        var gameWithParticipants = await Seeder.QueryGamesAsync(db =>
            db.Games
                .Include(g => g.Participations)
                .FirstOrDefaultAsync(g => g.Id == game.Id));
        
        var regularParticipant = gameWithParticipants!.Participations
            .FirstOrDefault(p => p.UserExternalId == userExternalId);
        
        regularParticipant.Should().NotBeNull();
    }

    [Fact]
    public async Task UserSignsUp_WithoutMatchingGuestData_ShouldNotFail()
    {
        // Arrange
        const string userExternalId = "auth0|charlie789";

        // Step 1: User signs up without any prior guest participation
        var syncResponse = await Client.PostAsJsonAsync("/users/sync", new
        {
            ExternalId = userExternalId,
            Name = "Charlie Brown",
            SkillLevel = "Advanced",
            PhoneNumber = "+33987654321", // No matching guest data
            Email = "charlie@example.com"
        });

        // Step 2: Should succeed without errors
        syncResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var syncResult = await syncResponse.ReadAsAsync<SyncUserFromAuthResponse>();
        syncResult!.IsNewUser.Should().BeTrue();
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

    private record SyncUserFromAuthResponse(
        string ExternalId,
        string Name,
        string SkillLevel,
        bool IsNewUser
    );

    private record MyGamesResponse(
        List<MyGameDto> Games,
        int TotalCount
    );

    private record MyGameDto(
        Guid Id,
        DateTime DateTime,
        string Location,
        string SkillLevel,
        int CurrentPlayers,
        int MaxPlayers,
        string Status
    );
}
