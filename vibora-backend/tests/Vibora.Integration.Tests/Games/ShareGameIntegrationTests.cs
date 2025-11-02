using System.Net;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Vibora.Integration.Tests.Infrastructure;

namespace Vibora.Integration.Tests.Games;

public class ShareGameIntegrationTests : IntegrationTestBaseImproved
{
    public ShareGameIntegrationTests(ViboraWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task CreateGameShare_WithValidRequest_ShouldCreateShareSuccessfully()
    {
        // Arrange - Create a game
        var (host, game) = await Seeder.SeedGameWithHostAsync("auth0|user123");

        AuthenticateAs(host.ExternalId);

        // Act - Create a share link
        var response = await Client.PostAsync($"/games/{game.Id}/shares?sharedByUserExternalId={host.ExternalId}", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.ReadAsAsync<CreateGameShareResponse>();
        result.Should().NotBeNull();
        result!.GameShareId.Should().NotBeEmpty();
        result.ShareToken.Should().NotBeNullOrEmpty();
        result.ShareToken.Length.Should().Be(8);
        result.ShareUrl.Should().Contain(result.ShareToken);

        // Verify in database
        var shareInDb = await Seeder.QueryGamesAsync(db =>
            db.GameShares.FirstOrDefaultAsync(gs => gs.ShareToken == result.ShareToken)
        );

        shareInDb.Should().NotBeNull();
        shareInDb!.GameId.Should().Be(game.Id);
        shareInDb.SharedByUserExternalId.Should().Be(host.ExternalId);
        shareInDb.ViewCount.Should().Be(0);
    }

    [Fact]
    public async Task CreateGameShare_WithNonExistentGame_ShouldReturnNotFound()
    {
        // Arrange
        var user = await Seeder.SeedUserAsync(u => u.WithExternalId("auth0|user123"));
        var nonExistentGameId = Guid.NewGuid();

        AuthenticateAs(user.ExternalId);

        // Act
        var response = await Client.PostAsync($"/games/{nonExistentGameId}/shares?sharedByUserExternalId={user.ExternalId}", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetShareByToken_WithValidToken_ShouldReturnShareAndIncrementViewCount()
    {
        // Arrange - Create a game and share
        var (host, game) = await Seeder.SeedGameWithHostAsync("auth0|user123");

        AuthenticateAs(host.ExternalId);
        var createResponse = await Client.PostAsync($"/games/{game.Id}/shares?sharedByUserExternalId={host.ExternalId}", null);
        var createResult = await createResponse.ReadAsAsync<CreateGameShareResponse>();

        // Act - Get share by token (should increment view count)
        var response = await Client.GetAsync($"/shares/{createResult!.ShareToken}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.ReadAsAsync<GetShareByTokenResponse>();
        result.Should().NotBeNull();
        result!.GameId.Should().Be(game.Id);
        result.ShareToken.Should().Be(createResult.ShareToken);
        result.ViewCount.Should().Be(1);
        result.IsExpired.Should().BeFalse();

        // Verify view count was incremented in database
        var shareInDb = await Seeder.QueryGamesAsync(db =>
            db.GameShares.FirstOrDefaultAsync(gs => gs.ShareToken == createResult.ShareToken)
        );

        shareInDb!.ViewCount.Should().Be(1);
    }

    [Fact]
    public async Task GetShareByToken_MultipleViews_ShouldIncrementViewCountEachTime()
    {
        // Arrange
        var (host, game) = await Seeder.SeedGameWithHostAsync("auth0|user123");

        AuthenticateAs(host.ExternalId);
        var createResponse = await Client.PostAsync($"/games/{game.Id}/shares?sharedByUserExternalId={host.ExternalId}", null);
        var createResult = await createResponse.ReadAsAsync<CreateGameShareResponse>();

        // Act - Get share multiple times
        await Client.GetAsync($"/shares/{createResult!.ShareToken}");
        await Client.GetAsync($"/shares/{createResult.ShareToken}");
        var response = await Client.GetAsync($"/shares/{createResult.ShareToken}");

        // Assert
        var result = await response.ReadAsAsync<GetShareByTokenResponse>();
        result!.ViewCount.Should().Be(3);
    }

    [Fact]
    public async Task GetShareByToken_WithNonExistentToken_ShouldReturnNotFound()
    {
        // Arrange
        var invalidToken = "INVALID1";

        // Act
        var response = await Client.GetAsync($"/shares/{invalidToken}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetShareMetadata_WithValidToken_ShouldReturnMetadata()
    {
        // Arrange
        var (host, game) = await Seeder.SeedGameWithHostAsync("auth0|user123");

        AuthenticateAs(host.ExternalId);
        var createResponse = await Client.PostAsync($"/games/{game.Id}/shares?sharedByUserExternalId={host.ExternalId}", null);
        var createResult = await createResponse.ReadAsAsync<CreateGameShareResponse>();

        // Act
        var response = await Client.GetAsync($"/shares/{createResult!.ShareToken}/metadata");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.ReadAsAsync<GetShareMetadataResponse>();
        result.Should().NotBeNull();
        result!.Title.Should().Contain("Test Club");
        result.Description.Should().Contain("Intermediate");
        result.Location.Should().Be("Test Club");
        result.SkillLevel.Should().Be("Intermediate");
        result.CurrentPlayers.Should().Be(1);
        result.MaxPlayers.Should().Be(4);
    }

    [Fact]
    public async Task GetShareMetadata_WithNonExistentToken_ShouldReturnNotFound()
    {
        // Arrange
        var invalidToken = "INVALID1";

        // Act
        var response = await Client.GetAsync($"/shares/{invalidToken}/metadata");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // Response DTOs
    private record CreateGameShareResponse(
        Guid GameShareId,
        string ShareToken,
        string ShareUrl
    );

    private record GetShareByTokenResponse(
        Guid GameId,
        Guid GameShareId,
        string ShareToken,
        int ViewCount,
        bool IsExpired
    );

    private record GetShareMetadataResponse(
        string Title,
        string Description,
        string Location,
        DateTime GameDateTime,
        string SkillLevel,
        int CurrentPlayers,
        int MaxPlayers,
        string GameStatus
    );
}
