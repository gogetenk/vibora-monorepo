using FluentAssertions;
using System.Net;
using System.Net.Http.Headers;
using Vibora.Integration.Tests.Infrastructure;

namespace Vibora.Integration.Tests.Users;

/// <summary>
/// E2E Integration tests for User Profile endpoints
/// Tests cover:
/// - GET /users/profile (authenticated user's full profile with statistics)
/// - PUT /users/profile (update profile with optional photo upload via multipart/form-data)
/// - GET /users/{externalId}/profile (public profile view)
///
/// Note: These tests target the NEW profile endpoints that support:
/// - FirstName/LastName split (replacing legacy Name field)
/// - Photo upload via multipart/form-data
/// - Profile statistics (games played, member since)
/// - Public profile view with privacy considerations
/// </summary>
public class ProfileEndpointsIntegrationTests : IntegrationTestBaseImproved
{
    public ProfileEndpointsIntegrationTests(ViboraWebApplicationFactory factory)
        : base(factory)
    {
    }

    #region GET /users/profile - Get current user's full profile

    [Fact]
    public async Task GetCurrentUserProfile_WithAuthenticatedUser_ReturnsProfile()
    {
        // Arrange - Seed user with complete profile
        var user = await Seeder.SeedUserAsync(u => u
            .WithExternalId("auth0|test-profile-1")
            .WithName("John Doe")
            .Intermediate()
            .WithBio("Passionate padel player"));

        // Authenticate as the user
        AuthenticateAs(user.ExternalId);

        // Act
        var response = await Client.GetAsync("/users/profile");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var profile = await response.ReadAsAsync<UserProfileDto>();
        profile.Should().NotBeNull();
        profile.ExternalId.Should().Be(user.ExternalId);
        profile.FirstName.Should().Be("John");
        profile.LastName.Should().Be("Doe");
        profile.SkillLevel.Should().Be("Intermediate");
        profile.Bio.Should().Be("Passionate padel player");
        profile.PhotoUrl.Should().BeNull(); // No photo uploaded
        profile.GamesPlayedCount.Should().Be(0); // New user, no games
        profile.MemberSince.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetCurrentUserProfile_WithUserWithoutLastName_ReturnsProfileWithNullLastName()
    {
        // Arrange - Seed user with only first name
        var user = await Seeder.SeedUserAsync(u => u
            .WithExternalId("auth0|test-profile-2")
            .WithName("Jane")
            .Beginner());

        AuthenticateAs(user.ExternalId);

        // Act
        var response = await Client.GetAsync("/users/profile");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var profile = await response.ReadAsAsync<UserProfileDto>();
        profile.FirstName.Should().Be("Jane");
        profile.LastName.Should().BeNull();
    }

    [Fact]
    public async Task GetCurrentUserProfile_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange - No authentication

        // Act
        var response = await Client.GetAsync("/users/profile");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCurrentUserProfile_WithNonExistentUser_ReturnsNotFound()
    {
        // Arrange - Authenticate but user doesn't exist in database
        var nonExistentUserId = "auth0|non-existent-user";
        AuthenticateAs(nonExistentUserId);

        // Act
        var response = await Client.GetAsync("/users/profile");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region PUT /users/profile - Update user profile

    [Fact]
    public async Task UpdateUserProfile_WithValidData_UpdatesProfileSuccessfully()
    {
        // Arrange - Seed user with initial data
        var user = await Seeder.SeedUserAsync(u => u
            .WithExternalId("auth0|test-update-1")
            .WithName("Old Name")
            .Beginner()
            .WithBio("Old bio"));

        AuthenticateAs(user.ExternalId);

        var updateRequest = new MultipartFormDataContent
        {
            { new StringContent("UpdatedFirstName"), "FirstName" },
            { new StringContent("UpdatedLastName"), "LastName" },
            { new StringContent("Advanced"), "SkillLevel" },
            { new StringContent("Updated bio content"), "Bio" }
        };

        // Act
        var response = await Client.PutAsync("/users/profile", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var profile = await response.ReadAsAsync<UserProfileDto>();
        profile.Should().NotBeNull();
        profile.FirstName.Should().Be("UpdatedFirstName");
        profile.LastName.Should().Be("UpdatedLastName");
        profile.SkillLevel.Should().Be("Advanced");
        profile.Bio.Should().Be("Updated bio content");

        // Verify in database
        var userInDb = await Seeder.QueryUsersAsync(async context =>
            await context.Users.FindAsync(user.ExternalId));

        userInDb.Should().NotBeNull();
        userInDb!.FirstName.Should().Be("UpdatedFirstName");
        userInDb.LastName.Should().Be("UpdatedLastName");
        userInDb.SkillLevel.Should().Be(9); // Advanced
        userInDb.Bio.Should().Be("Updated bio content");
    }

    [Fact]
    public async Task UpdateUserProfile_WithInvalidData_ReturnsUnprocessableEntity()
    {
        // Arrange
        var user = await Seeder.SeedUserAsync(u => u
            .WithExternalId("auth0|test-update-2")
            .WithName("John")
            .Intermediate());

        AuthenticateAs(user.ExternalId);

        var updateRequest = new MultipartFormDataContent
        {
            { new StringContent(""), "FirstName" }, // Empty FirstName (invalid)
            { new StringContent("Intermediate"), "SkillLevel" }
        };

        // Act
        var response = await Client.PutAsync("/users/profile", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task UpdateUserProfile_WithTooShortFirstName_ReturnsValidationError()
    {
        // Arrange
        var user = await Seeder.SeedUserAsync(u => u
            .WithExternalId("auth0|test-update-3")
            .WithName("John")
            .Intermediate());

        AuthenticateAs(user.ExternalId);

        var updateRequest = new MultipartFormDataContent
        {
            { new StringContent("J"), "FirstName" }, // Too short (< 2 chars)
            { new StringContent("Intermediate"), "SkillLevel" }
        };

        // Act
        var response = await Client.PutAsync("/users/profile", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task UpdateUserProfile_WithTooLongBio_ReturnsValidationError()
    {
        // Arrange
        var user = await Seeder.SeedUserAsync(u => u
            .WithExternalId("auth0|test-update-4")
            .WithName("John")
            .Intermediate());

        AuthenticateAs(user.ExternalId);

        var longBio = new string('x', 151); // > 150 chars (validation limit)

        var updateRequest = new MultipartFormDataContent
        {
            { new StringContent("John"), "FirstName" },
            { new StringContent("Intermediate"), "SkillLevel" },
            { new StringContent(longBio), "Bio" }
        };

        // Act
        var response = await Client.PutAsync("/users/profile", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task UpdateUserProfile_WithPhoto_UploadsAndUpdatesUrl()
    {
        // Arrange
        var user = await Seeder.SeedUserAsync(u => u
            .WithExternalId("auth0|test-update-photo-1")
            .WithName("John")
            .Intermediate());

        AuthenticateAs(user.ExternalId);

        // Create fake JPEG image (1x1 pixel red JPEG)
        var imageBytes = CreateTestJpegImage();
        var imageContent = new ByteArrayContent(imageBytes);
        imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");

        var updateRequest = new MultipartFormDataContent
        {
            { new StringContent("John"), "FirstName" },
            { new StringContent("Intermediate"), "SkillLevel" },
            { imageContent, "photo", "profile.jpg" }
        };

        // Act
        var response = await Client.PutAsync("/users/profile", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var profile = await response.ReadAsAsync<UserProfileDto>();
        profile.Should().NotBeNull();
        profile.PhotoUrl.Should().NotBeNullOrEmpty();
        // PhotoUrl contains sanitized user ID (pipe replaced with underscore for file paths)
        profile.PhotoUrl.Should().Contain(user.ExternalId.Replace("|", "_"));

        // Verify in database
        var userInDb = await Seeder.QueryUsersAsync(async context =>
            await context.Users.FindAsync(user.ExternalId));

        userInDb!.PhotoUrl.Should().NotBeNullOrEmpty();
        userInDb.PhotoUrl.Should().Be(profile.PhotoUrl);
    }

    [Fact]
    public async Task UpdateUserProfile_WithPngPhoto_UploadsSuccessfully()
    {
        // Arrange
        var user = await Seeder.SeedUserAsync(u => u
            .WithExternalId("auth0|test-update-photo-2")
            .WithName("Jane")
            .Advanced());

        AuthenticateAs(user.ExternalId);

        // Create fake PNG image
        var imageBytes = CreateTestPngImage();
        var imageContent = new ByteArrayContent(imageBytes);
        imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");

        var updateRequest = new MultipartFormDataContent
        {
            { new StringContent("Jane"), "FirstName" },
            { new StringContent("Advanced"), "SkillLevel" },
            { imageContent, "photo", "profile.png" }
        };

        // Act
        var response = await Client.PutAsync("/users/profile", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var profile = await response.ReadAsAsync<UserProfileDto>();
        profile.PhotoUrl.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task UpdateUserProfile_WithLargePhoto_ReturnsError()
    {
        // Arrange
        var user = await Seeder.SeedUserAsync(u => u
            .WithExternalId("auth0|test-update-photo-3")
            .WithName("John")
            .Intermediate());

        AuthenticateAs(user.ExternalId);

        // Create 2 MB image (exceeds 1 MB limit)
        var largeImageBytes = new byte[2 * 1024 * 1024];
        new Random().NextBytes(largeImageBytes); // Fill with random data
        var imageContent = new ByteArrayContent(largeImageBytes);
        imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");

        var updateRequest = new MultipartFormDataContent
        {
            { new StringContent("John"), "FirstName" },
            { new StringContent("Intermediate"), "SkillLevel" },
            { imageContent, "photo", "large-profile.jpg" }
        };

        // Act
        var response = await Client.PutAsync("/users/profile", updateRequest);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task UpdateUserProfile_WithInvalidImageFormat_ReturnsError()
    {
        // Arrange
        var user = await Seeder.SeedUserAsync(u => u
            .WithExternalId("auth0|test-update-photo-4")
            .WithName("John")
            .Intermediate());

        AuthenticateAs(user.ExternalId);

        // Create non-image file (text file)
        var textBytes = System.Text.Encoding.UTF8.GetBytes("This is not an image");
        var textContent = new ByteArrayContent(textBytes);
        textContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");

        var updateRequest = new MultipartFormDataContent
        {
            { new StringContent("John"), "FirstName" },
            { new StringContent("Intermediate"), "SkillLevel" },
            { textContent, "photo", "notanimage.txt" }
        };

        // Act
        var response = await Client.PutAsync("/users/profile", updateRequest);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task UpdateUserProfile_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange - No authentication
        var updateRequest = new MultipartFormDataContent
        {
            { new StringContent("Test"), "FirstName" },
            { new StringContent("Intermediate"), "SkillLevel" }
        };

        // Act
        var response = await Client.PutAsync("/users/profile", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateUserProfile_ReplacingExistingPhoto_UpdatesPhotoUrl()
    {
        // Arrange - User with existing photo
        var user = await Seeder.SeedUserAsync(u => u
            .WithExternalId("auth0|test-update-photo-5")
            .WithName("John")
            .Intermediate());

        AuthenticateAs(user.ExternalId);

        // First photo upload
        var firstImage = CreateTestJpegImage();
        var firstImageContent = new ByteArrayContent(firstImage);
        firstImageContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");

        var firstRequest = new MultipartFormDataContent
        {
            { new StringContent("John"), "FirstName" },
            { new StringContent("Intermediate"), "SkillLevel" },
            { firstImageContent, "photo", "first.jpg" }
        };

        var firstResponse = await Client.PutAsync("/users/profile", firstRequest);
        var firstProfile = await firstResponse.ReadAsAsync<UserProfileDto>();
        var firstPhotoUrl = firstProfile.PhotoUrl;

        // Second photo upload (replacement)
        var secondImage = CreateTestPngImage();
        var secondImageContent = new ByteArrayContent(secondImage);
        secondImageContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");

        var secondRequest = new MultipartFormDataContent
        {
            { new StringContent("John"), "FirstName" },
            { new StringContent("Intermediate"), "SkillLevel" },
            { secondImageContent, "photo", "second.png" }
        };

        // Act
        var response = await Client.PutAsync("/users/profile", secondRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var profile = await response.ReadAsAsync<UserProfileDto>();
        profile.PhotoUrl.Should().NotBeNullOrEmpty();
        profile.PhotoUrl.Should().NotBe(firstPhotoUrl); // New photo should have different URL
    }

    #endregion

    #region GET /users/{externalId}/profile - Get public profile

    [Fact]
    public async Task GetUserPublicProfile_WithExistingUser_ReturnsPublicProfile()
    {
        // Arrange - Seed target user with full profile
        var targetUser = await Seeder.SeedUserAsync(u => u
            .WithExternalId("auth0|public-profile-1")
            .WithName("Thomas Martin")
            .Advanced()
            .WithBio("Available for games on weekends"));

        // Act - No authentication required for public profiles
        var response = await Client.GetAsync($"/users/{targetUser.ExternalId}/profile");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var profile = await response.ReadAsAsync<UserPublicProfileDto>();
        profile.Should().NotBeNull();
        profile.FirstName.Should().Be("Thomas");
        profile.LastNameInitial.Should().Be("M."); // Privacy: only initial shown
        profile.SkillLevel.Should().Be("Advanced");
        profile.Bio.Should().Be("Available for games on weekends");
        profile.PhotoUrl.Should().BeNull(); // No photo uploaded
        profile.MemberSince.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetUserPublicProfile_WithUserWithoutLastName_ShowsNoInitial()
    {
        // Arrange
        var targetUser = await Seeder.SeedUserAsync(u => u
            .WithExternalId("auth0|public-profile-2")
            .WithName("Madonna")
            .Intermediate());

        // Act
        var response = await Client.GetAsync($"/users/{targetUser.ExternalId}/profile");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var profile = await response.ReadAsAsync<UserPublicProfileDto>();
        profile.FirstName.Should().Be("Madonna");
        profile.LastNameInitial.Should().BeNull(); // No last name
    }

    [Fact]
    public async Task GetUserPublicProfile_WithNonExistentUser_ReturnsNotFound()
    {
        // Act
        var response = await Client.GetAsync("/users/auth0|non-existent-public/profile");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetUserPublicProfile_WithUserWithPhoto_ReturnsPhotoUrl()
    {
        // Arrange - Create user and upload photo
        var user = await Seeder.SeedUserAsync(u => u
            .WithExternalId("auth0|public-profile-photo")
            .WithName("John Doe")
            .Advanced());

        AuthenticateAs(user.ExternalId);

        // Upload photo
        var imageBytes = CreateTestJpegImage();
        var imageContent = new ByteArrayContent(imageBytes);
        imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");

        var updateRequest = new MultipartFormDataContent
        {
            { new StringContent("John"), "FirstName" },
            { new StringContent("Doe"), "LastName" },
            { new StringContent("Advanced"), "SkillLevel" },
            { imageContent, "photo", "profile.jpg" }
        };

        await Client.PutAsync("/users/profile", updateRequest);

        // Clear authentication to test public access
        ClearAuthentication();

        // Act - Get public profile
        var response = await Client.GetAsync($"/users/{user.ExternalId}/profile");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var profile = await response.ReadAsAsync<UserPublicProfileDto>();
        profile.PhotoUrl.Should().NotBeNullOrEmpty();
        profile.FirstName.Should().Be("John");
        profile.LastNameInitial.Should().Be("D.");
    }

    [Fact]
    public async Task GetUserPublicProfile_DoesNotRequireAuthentication()
    {
        // Arrange
        var user = await Seeder.SeedUserAsync(u => u
            .WithExternalId("auth0|public-profile-3")
            .WithName("Public User")
            .Intermediate());

        // Act - No authentication
        var response = await Client.GetAsync($"/users/{user.ExternalId}/profile");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var profile = await response.ReadAsAsync<UserPublicProfileDto>();
        profile.FirstName.Should().Be("Public");
        profile.LastNameInitial.Should().Be("U."); // "User" becomes "U."
    }

    #endregion

    #region Cross-endpoint consistency tests

    [Fact]
    public async Task ProfileEndpoints_GetProfileAndGetPublicProfile_ReturnConsistentData()
    {
        // Arrange
        var user = await Seeder.SeedUserAsync(u => u
            .WithExternalId("auth0|consistency-test")
            .WithName("John Smith")
            .Advanced()
            .WithBio("Testing consistency"));

        AuthenticateAs(user.ExternalId);

        // Act - Get full profile
        var fullProfileResponse = await Client.GetAsync("/users/profile");
        var fullProfile = await fullProfileResponse.ReadAsAsync<UserProfileDto>();

        // Clear authentication
        ClearAuthentication();

        // Act - Get public profile
        var publicProfileResponse = await Client.GetAsync($"/users/{user.ExternalId}/profile");
        var publicProfile = await publicProfileResponse.ReadAsAsync<UserPublicProfileDto>();

        // Assert - Data should be consistent
        fullProfile.FirstName.Should().Be(publicProfile.FirstName);
        fullProfile.SkillLevel.Should().Be(publicProfile.SkillLevel);
        fullProfile.Bio.Should().Be(publicProfile.Bio);
        fullProfile.PhotoUrl.Should().Be(publicProfile.PhotoUrl);

        // Public profile should hide full last name
        if (fullProfile.LastName != null)
        {
            publicProfile.LastNameInitial.Should().Be($"{fullProfile.LastName[0]}.");
        }
    }

    [Fact]
    public async Task UpdateProfile_ThenGetProfile_ReturnsUpdatedData()
    {
        // Arrange
        var user = await Seeder.SeedUserAsync(u => u
            .WithExternalId("auth0|update-then-get")
            .WithName("Original")
            .Beginner());

        AuthenticateAs(user.ExternalId);

        var updateRequest = new MultipartFormDataContent
        {
            { new StringContent("Updated"), "FirstName" },
            { new StringContent("Name"), "LastName" },
            { new StringContent("Advanced"), "SkillLevel" },
            { new StringContent("New bio"), "Bio" }
        };

        // Act - Update
        await Client.PutAsync("/users/profile", updateRequest);

        // Act - Get
        var getResponse = await Client.GetAsync("/users/profile");
        var profile = await getResponse.ReadAsAsync<UserProfileDto>();

        // Assert - Should see updated data
        profile.FirstName.Should().Be("Updated");
        profile.LastName.Should().Be("Name");
        profile.SkillLevel.Should().Be("Advanced");
        profile.Bio.Should().Be("New bio");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a minimal valid JPEG image (1x1 pixel red JPEG)
    /// Size: ~631 bytes
    /// </summary>
    private byte[] CreateTestJpegImage()
    {
        // Minimal valid JPEG: 1x1 red pixel
        return new byte[]
        {
            0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01, 0x01, 0x01, 0x00, 0x48,
            0x00, 0x48, 0x00, 0x00, 0xFF, 0xDB, 0x00, 0x43, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xC0, 0x00, 0x0B,
            0x08, 0x00, 0x01, 0x00, 0x01, 0x01, 0x01, 0x11, 0x00, 0xFF, 0xC4, 0x00, 0x14, 0x00, 0x01, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF,
            0xC4, 0x00, 0x14, 0x10, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xDA, 0x00, 0x08, 0x01, 0x01, 0x00, 0x00, 0x3F, 0x00, 0x7F,
            0xFF, 0xD9
        };
    }

    /// <summary>
    /// Creates a minimal valid PNG image (1x1 pixel transparent PNG)
    /// Size: 67 bytes
    /// </summary>
    private byte[] CreateTestPngImage()
    {
        // Minimal valid PNG: 1x1 transparent pixel
        return new byte[]
        {
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
            0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x08, 0x06, 0x00, 0x00, 0x00, 0x1F, 0x15, 0xC4,
            0x89, 0x00, 0x00, 0x00, 0x0A, 0x49, 0x44, 0x41, 0x54, 0x78, 0x9C, 0x63, 0x00, 0x01, 0x00, 0x00,
            0x05, 0x00, 0x01, 0x0D, 0x0A, 0x2D, 0xB4, 0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44, 0xAE,
            0x42, 0x60, 0x82
        };
    }

    #endregion

    // Response DTOs for deserialization
    private record UserProfileDto(
        string ExternalId,
        string FirstName,
        string? LastName,
        string SkillLevel,
        string? Bio,
        string? PhotoUrl,
        int GamesPlayedCount,
        DateTime MemberSince
    );

    private record UserPublicProfileDto(
        string FirstName,
        string? LastNameInitial,
        string SkillLevel,
        string? Bio,
        string? PhotoUrl,
        DateTime MemberSince
    );
}
