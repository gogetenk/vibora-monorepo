using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vibora.Games.Infrastructure.Data;
using Vibora.Notifications.Infrastructure.Data;
using Vibora.Users.Infrastructure.Data;

namespace Vibora.Integration.Tests.Infrastructure;

/// <summary>
/// Base class for integration tests
/// Provides access to HttpClient, DbContexts for seeding, and ensures database migration
/// </summary>
public abstract class IntegrationTestBase : IClassFixture<ViboraWebApplicationFactory>, IAsyncLifetime
{
    protected readonly ViboraWebApplicationFactory Factory;
    protected readonly HttpClient Client;

    protected IntegrationTestBase(ViboraWebApplicationFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
    }

    /// <summary>
    /// Authenticate the HttpClient with a specific user
    /// </summary>
    protected void AuthenticateAs(string userExternalId, string? email = null)
    {
        Client.WithUser(userExternalId, email);
    }

    /// <summary>
    /// Clear authentication from the HttpClient
    /// </summary>
    protected void ClearAuthentication()
    {
        Client.WithoutAuth();
    }

    public Task InitializeAsync()
    {
        // Database schema is created in ViboraWebApplicationFactory.CreateHost()
        // Nothing to do here
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        // Clean up database after each test - delete data, not schema
        await using var scope = Factory.Services.CreateAsyncScope();

        var gamesDb = scope.ServiceProvider.GetRequiredService<GamesDbContext>();
        var usersDb = scope.ServiceProvider.GetRequiredService<UsersDbContext>();
        var notificationsDb = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();

        // Delete data instead of dropping database
        // Order matters: delete child tables first (foreign keys)
        await gamesDb.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"GameShares\", \"GuestParticipants\", \"Participations\", \"Games\" CASCADE");
        await usersDb.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"Users\" CASCADE");
        await notificationsDb.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"Notifications\" CASCADE");

        // Clear authentication for next test
        ClearAuthentication();
    }
}
