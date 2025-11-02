using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Vibora.Integration.Tests.Infrastructure;
using Vibora.Users.Domain;
using Xunit;

namespace Vibora.Integration.Tests.Users;

public class UserProfileIntegrationTests : IntegrationTestBaseImproved
{
    public UserProfileIntegrationTests(ViboraWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetUserById_WithExistingUser_ShouldReturnUserWithBio()
    {
        // Arrange - Seed user with bio using builder
        var user = await Seeder.SeedUserAsync(u => u
            .WithExternalId("auth0|test-user-123")
            .WithName("John Doe")
            .Intermediate()
            .WithBio("Padel enthusiast from Paris 🎾"));

        // Authenticate
        AuthenticateAs(user.ExternalId);

        // Act
        var response = await Client.GetAsync($"/users/{user.ExternalId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.ReadAsAsync<GetUserByIdResponse>();
        result.ExternalId.Should().Be(user.ExternalId);
        result.Name.Should().Be("John Doe");
        result.SkillLevel.Should().Be("Intermediate");
        result.Bio.Should().Be("Padel enthusiast from Paris 🎾");
    }

    [Fact]
    public async Task GetUserById_WithUserWithoutBio_ShouldReturnNullBio()
    {
        // Arrange - Seed user without bio
        var user = await Seeder.SeedUserAsync(u => u
            .WithExternalId("auth0|no-bio-user")
            .WithName("Jane Smith")
            .Beginner());

        // Authenticate
        AuthenticateAs(user.ExternalId);

        // Act
        var response = await Client.GetAsync($"/users/{user.ExternalId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.ReadAsAsync<GetUserByIdResponse>();
        result.Bio.Should().BeNull();
    }

    [Fact]
    public async Task GetUserById_WithNonExistentUser_ShouldReturnNotFound()
    {
        // Arrange - No user seeded
        var externalId = "auth0|nonexistent";

        // Authenticate
        AuthenticateAs(externalId);

        // Act
        var response = await Client.GetAsync($"/users/{externalId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateProfile_WithBio_ShouldUpdateUserProfile()
    {
        // Arrange - Seed user with initial data
        var user = await Seeder.SeedUserAsync(u => u
            .WithExternalId("auth0|update-test")
            .WithName("Old Name")
            .Beginner());

        var updateRequest = new
        {
            name = "Updated Name",
            skillLevel = "Advanced",
            bio = "New bio content"
        };

        // Authenticate
        AuthenticateAs(user.ExternalId);

        // Act
        var response = await Client.PutAsJsonAsync("/users/me", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.ReadAsAsync<UpdateUserProfileResponse>();
        result.FirstName.Should().Be("Updated");
        result.LastName.Should().Be("Name");
        result.SkillLevel.Should().Be("Advanced");
        result.Bio.Should().Be("New bio content");

        // Verify in database
        var userInDb = await Seeder.QueryUsersAsync(async context =>
            await context.Users.FindAsync(user.ExternalId));

        userInDb.Should().NotBeNull();
        userInDb!.Name.Should().Be("Updated Name");
        userInDb.SkillLevel.Should().Be(9); // Advanced
        userInDb.Bio.Should().Be("New bio content");
    }

    [Fact]
    public async Task UpdateProfile_WithNullBio_ShouldClearBio()
    {
        // Arrange - Seed user with existing bio
        var user = await Seeder.SeedUserAsync(u => u
            .WithExternalId("auth0|clear-bio-test")
            .WithName("User")
            .Intermediate()
            .WithBio("Old bio"));

        var updateRequest = new
        {
            name = "User",
            skillLevel = "Intermediate",
            bio = (string?)null
        };

        // Authenticate
        AuthenticateAs(user.ExternalId);

        // Act
        var response = await Client.PutAsJsonAsync("/users/me", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.ReadAsAsync<UpdateUserProfileResponse>();
        result.Bio.Should().BeNull();

        // Verify in database
        var userInDb = await Seeder.QueryUsersAsync(async context =>
            await context.Users.FindAsync(user.ExternalId));

        userInDb!.Bio.Should().BeNull();
    }

    [Fact]
    public async Task GetCurrentUser_ShouldReturnCurrentUserWithBio()
    {
        // Arrange - Seed user with bio
        var user = await Seeder.SeedUserAsync(u => u
            .WithExternalId("auth0|current-user")
            .WithName("Current User")
            .Advanced()
            .WithBio("I play every weekend!"));

        // Authenticate
        AuthenticateAs(user.ExternalId);

        // Act
        var response = await Client.GetAsync("/users/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.ReadAsAsync<GetCurrentUserResponse>();
        result.ExternalId.Should().Be(user.ExternalId);
        result.Name.Should().Be("Current User");
        result.SkillLevel.Should().Be("Advanced");
        result.Bio.Should().Be("I play every weekend!");
    }

    // Response DTOs for deserialization
    private record GetUserByIdResponse(string ExternalId, string Name, string SkillLevel, string? Bio);
    private record UpdateUserProfileResponse(string ExternalId, string FirstName, string? LastName, string SkillLevel, string? Bio);
    private record GetCurrentUserResponse(string ExternalId, string Name, string SkillLevel, string? Bio);
}
