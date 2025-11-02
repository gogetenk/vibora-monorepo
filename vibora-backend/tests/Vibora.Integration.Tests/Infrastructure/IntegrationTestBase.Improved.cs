using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vibora.Games.Infrastructure.Data;
using Vibora.Notifications.Infrastructure.Data;
using Vibora.Users.Infrastructure.Data;

namespace Vibora.Integration.Tests.Infrastructure;

/// <summary>
/// IMPROVED version of IntegrationTestBase
/// Base class for integration tests with enhanced helpers and cleaner API
/// </summary>
public abstract class IntegrationTestBaseImproved : IClassFixture<ViboraWebApplicationFactory>, IAsyncLifetime
{
    protected readonly ViboraWebApplicationFactory Factory;
    protected readonly HttpClient Client;
    protected readonly TestDataSeeder Seeder;

    protected IntegrationTestBaseImproved(ViboraWebApplicationFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
        Seeder = new TestDataSeeder(factory.Services);
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
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        // Clean up database after each test using EF Core
        await CleanupDatabaseAsync();

        // Clear authentication for next test
        ClearAuthentication();
    }

    /// <summary>
    /// Clean up database using EF Core (more robust than raw SQL)
    /// </summary>
    private async Task CleanupDatabaseAsync()
    {
        await using var scope = Factory.Services.CreateAsyncScope();

        var gamesDb = scope.ServiceProvider.GetRequiredService<GamesDbContext>();
        var usersDb = scope.ServiceProvider.GetRequiredService<UsersDbContext>();
        var notificationsDb = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();

        // Delete data in correct order (respecting foreign keys)
        // Child tables first, parent tables last
        
        // Games module
        await gamesDb.Database.ExecuteSqlRawAsync("DELETE FROM \"GameShares\"");
        await gamesDb.Database.ExecuteSqlRawAsync("DELETE FROM \"GuestParticipants\"");
        await gamesDb.Database.ExecuteSqlRawAsync("DELETE FROM \"Participations\"");
        await gamesDb.Database.ExecuteSqlRawAsync("DELETE FROM \"Games\"");

        // Notifications module
        await notificationsDb.Database.ExecuteSqlRawAsync("DELETE FROM \"Notifications\"");

        // Users module (last because of FKs)
        await usersDb.Database.ExecuteSqlRawAsync("DELETE FROM \"UserNotificationSettings\"");
        await usersDb.Database.ExecuteSqlRawAsync("DELETE FROM \"Users\"");
    }

}
