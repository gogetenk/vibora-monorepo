using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Vibora.Integration.Tests.Infrastructure;
using Vibora.Users.Application.Commands.ClaimGuestParticipations;

namespace Vibora.Integration.Tests.Users;

public class ClaimGuestParticipationsIntegrationTests : IntegrationTestBaseImproved
{
    public ClaimGuestParticipationsIntegrationTests(ViboraWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task ClaimGuestParticipations_WithMatchingPhoneNumber_ShouldConvertParticipations()
    {
        // Arrange - Create user with phone
        var user = await Seeder.SeedUserAsync(u => u
            .WithExternalId("auth0|testuser")
            .WithName("John Doe")
            .Intermediate());

        // Create games with guest participations using the same phone
        var phoneNumber = "+33612345678";
        var (host, game1) = await Seeder.SeedGameWithHostAsync("auth0|host1");
        var (_, game2) = await Seeder.SeedGameWithHostAsync("auth0|host2");

        // Add guest participations with matching phone number
        await Seeder.ExecuteGamesAsync(async db =>
        {
            var guestParticipant1 = Vibora.Games.Domain.GuestParticipant.Create(
                game1.Id, "Guest John", phoneNumber, null);
            var guestParticipant2 = Vibora.Games.Domain.GuestParticipant.Create(
                game2.Id, "Guest John", phoneNumber, null);

            if (guestParticipant1.IsSuccess && guestParticipant2.IsSuccess)
            {
                db.GuestParticipants.Add(guestParticipant1.Value);
                db.GuestParticipants.Add(guestParticipant2.Value);
                
                // Update games - reload them from db to avoid detached entity issues
                var dbGame1 = await db.Games.FindAsync(game1.Id);
                var dbGame2 = await db.Games.FindAsync(game2.Id);
                
                if (dbGame1 != null && dbGame2 != null)
                {
                    dbGame1.AddGuestParticipant("Guest John", phoneNumber, null);
                    dbGame2.AddGuestParticipant("Guest John", phoneNumber, null);
                }
                
                await db.SaveChangesAsync();
            }
        });

        AuthenticateAs(user.ExternalId);

        var request = new
        {
            PhoneNumber = phoneNumber,
            Email = (string?)null
        };

        // Act
        var response = await Client.PostAsJsonAsync("/users/claim-guest-participations", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.ReadAsAsync<ClaimGuestParticipationsResult>();
        result.Should().NotBeNull();
        result!.ClaimedParticipations.Should().Be(2);
        result.ClaimedGames.Should().HaveCount(2);

        // Verify guest participations are removed
        var guestParticipationsCount = await Seeder.QueryGamesAsync(db =>
            db.GuestParticipants.CountAsync(gp => gp.PhoneNumber == phoneNumber));
        guestParticipationsCount.Should().Be(0);

        // Verify regular participations are created
        var participationsCount = await Seeder.QueryGamesAsync(db =>
            db.Participations.CountAsync(p => p.UserExternalId == user.ExternalId));
        participationsCount.Should().Be(2);

        // Verify participations have correct data
        var participations = await Seeder.QueryGamesAsync(db =>
            db.Participations
                .Where(p => p.UserExternalId == user.ExternalId)
                .ToListAsync());

        participations.Should().AllSatisfy(p =>
        {
            p.UserName.Should().Be(user.Name);
            p.UserSkillLevel.Should().Be(user.SkillLevel.ToString());
            p.IsHost.Should().BeFalse();
        });
    }

    [Fact]
    public async Task ClaimGuestParticipations_WithMatchingEmail_ShouldConvertParticipations()
    {
        // Arrange
        var user = await Seeder.SeedUserAsync(u => u
            .WithExternalId("auth0|testuser2")
            .WithName("Jane Smith")
            .Advanced());

        var email = "guest@example.com";
        var (host, game) = await Seeder.SeedGameWithHostAsync();

        // Add guest participation with matching email
        await Seeder.ExecuteGamesAsync(async db =>
        {
            var guestParticipant = Vibora.Games.Domain.GuestParticipant.Create(
                game.Id, "Guest Jane", null, email);

            if (guestParticipant.IsSuccess)
            {
                db.GuestParticipants.Add(guestParticipant.Value);
                
                var dbGame = await db.Games.FindAsync(game.Id);
                if (dbGame != null)
                {
                    dbGame.AddGuestParticipant("Guest Jane", null, email);
                }
                
                await db.SaveChangesAsync();
            }
        });

        AuthenticateAs(user.ExternalId);

        var request = new
        {
            PhoneNumber = (string?)null,
            Email = email
        };

        // Act
        var response = await Client.PostAsJsonAsync("/users/claim-guest-participations", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.ReadAsAsync<ClaimGuestParticipationsResult>();
        result.Should().NotBeNull();
        result!.ClaimedParticipations.Should().Be(1);

        // Verify conversion
        var guestExists = await Seeder.QueryGamesAsync(db =>
            db.GuestParticipants.AnyAsync(gp => gp.Email == email));
        guestExists.Should().BeFalse();

        var participationExists = await Seeder.QueryGamesAsync(db =>
            db.Participations.AnyAsync(p => p.UserExternalId == user.ExternalId && p.GameId == game.Id));
        participationExists.Should().BeTrue();
    }

    [Fact]
    public async Task ClaimGuestParticipations_WithBothPhoneAndEmail_ShouldMatchEither()
    {
        // Arrange
        var user = await Seeder.SeedUserAsync();

        var phoneNumber = "+33698765432";
        var email = "multi@example.com";

        var (_, game1) = await Seeder.SeedGameWithHostAsync();
        var (_, game2) = await Seeder.SeedGameWithHostAsync("auth0|host2");

        // One guest with phone, one with email
        await Seeder.ExecuteGamesAsync(async db =>
        {
            var guest1 = Vibora.Games.Domain.GuestParticipant.Create(
                game1.Id, "Guest Phone", phoneNumber, null);
            var guest2 = Vibora.Games.Domain.GuestParticipant.Create(
                game2.Id, "Guest Email", null, email);

            if (guest1.IsSuccess && guest2.IsSuccess)
            {
                db.GuestParticipants.Add(guest1.Value);
                db.GuestParticipants.Add(guest2.Value);
                
                var dbGame1 = await db.Games.FindAsync(game1.Id);
                var dbGame2 = await db.Games.FindAsync(game2.Id);
                
                if (dbGame1 != null && dbGame2 != null)
                {
                    dbGame1.AddGuestParticipant("Guest Phone", phoneNumber, null);
                    dbGame2.AddGuestParticipant("Guest Email", null, email);
                }
                
                await db.SaveChangesAsync();
            }
        });

        AuthenticateAs(user.ExternalId);

        var request = new
        {
            PhoneNumber = phoneNumber,
            Email = email
        };

        // Act
        var response = await Client.PostAsJsonAsync("/users/claim-guest-participations", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.ReadAsAsync<ClaimGuestParticipationsResult>();
        result!.ClaimedParticipations.Should().Be(2);
    }

    [Fact]
    public async Task ClaimGuestParticipations_WithNoMatchingGuests_ShouldReturnZero()
    {
        // Arrange
        var user = await Seeder.SeedUserAsync();
        AuthenticateAs(user.ExternalId);

        var request = new
        {
            PhoneNumber = "+33600000000",
            Email = "nomatch@example.com"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/users/claim-guest-participations", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.ReadAsAsync<ClaimGuestParticipationsResult>();
        result!.ClaimedParticipations.Should().Be(0);
        result.ClaimedGames.Should().BeEmpty();
    }

    [Fact]
    public async Task ClaimGuestParticipations_WithoutContactInfo_ShouldReturnValidationError()
    {
        // Arrange
        var user = await Seeder.SeedUserAsync();
        AuthenticateAs(user.ExternalId);

        var request = new
        {
            PhoneNumber = (string?)null,
            Email = (string?)null
        };

        // Act
        var response = await Client.PostAsJsonAsync("/users/claim-guest-participations", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task ClaimGuestParticipations_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange - No authentication
        var request = new
        {
            PhoneNumber = "+33612345678",
            Email = (string?)null
        };

        // Act
        var response = await Client.PostAsJsonAsync("/users/claim-guest-participations", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ClaimGuestParticipations_ForCanceledGame_ShouldSkipConversion()
    {
        // Arrange
        var user = await Seeder.SeedUserAsync();
        var phoneNumber = "+33611111111";

        var (host, game) = await Seeder.SeedGameWithHostAsync();

        // Add guest participation
        await Seeder.ExecuteGamesAsync(async db =>
        {
            var guest = Vibora.Games.Domain.GuestParticipant.Create(
                game.Id, "Guest", phoneNumber, null);

            if (guest.IsSuccess)
            {
                db.GuestParticipants.Add(guest.Value);
                
                var dbGame = await db.Games.FindAsync(game.Id);
                if (dbGame != null)
                {
                    dbGame.AddGuestParticipant("Guest", phoneNumber, null);
                    dbGame.Cancel();
                }
                
                await db.SaveChangesAsync();
            }
        });

        AuthenticateAs(user.ExternalId);

        var request = new
        {
            PhoneNumber = phoneNumber,
            Email = (string?)null
        };

        // Act
        var response = await Client.PostAsJsonAsync("/users/claim-guest-participations", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.ReadAsAsync<ClaimGuestParticipationsResult>();
        // Should skip canceled game
        result!.ClaimedParticipations.Should().Be(0);
    }

    [Fact]
    public async Task ClaimGuestParticipations_WhenAlreadyParticipating_ShouldSkipDuplicate()
    {
        // Arrange
        var user = await Seeder.SeedUserAsync();
        var phoneNumber = "+33622222222";

        var (host, game) = await Seeder.SeedGameWithHostAsync();

        // User already participates in the game
        await Seeder.ExecuteGamesAsync(async db =>
        {
            var participation = Vibora.Games.Domain.Participation.Create(
                game.Id, user.ExternalId, user.Name, user.SkillLevel.ToString(), false);
            db.Participations.Add(participation);
            
            // Also add guest participation with same phone
            var guest = Vibora.Games.Domain.GuestParticipant.Create(
                game.Id, "Guest", phoneNumber, null);
            if (guest.IsSuccess)
            {
                db.GuestParticipants.Add(guest.Value);
                
                // Update game's guest count
                var dbGame = await db.Games.FindAsync(game.Id);
                if (dbGame != null)
                {
                    dbGame.AddGuestParticipant("Guest", phoneNumber, null);
                }
            }
            
            await db.SaveChangesAsync();
        });

        AuthenticateAs(user.ExternalId);

        var request = new
        {
            PhoneNumber = phoneNumber,
            Email = (string?)null
        };

        // Act
        var response = await Client.PostAsJsonAsync("/users/claim-guest-participations", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.ReadAsAsync<ClaimGuestParticipationsResult>();
        // Should skip since user already participates
        result!.ClaimedParticipations.Should().Be(0);

        // Verify no duplicate participation created
        var participationsCount = await Seeder.QueryGamesAsync(db =>
            db.Participations.CountAsync(p => p.UserExternalId == user.ExternalId && p.GameId == game.Id));
        participationsCount.Should().Be(1);
    }
}
