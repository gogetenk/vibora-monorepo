using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vibora.Games.Domain;
using Vibora.Games.Infrastructure.Data;
using Vibora.Integration.Tests.Infrastructure.TestDataBuilders;
using Vibora.Notifications.Domain;
using Vibora.Notifications.Infrastructure.Data;
using Vibora.Users.Domain;
using Vibora.Users.Infrastructure.Data;

namespace Vibora.Integration.Tests.Infrastructure;

/// <summary>
/// Centralized test data seeding helper
/// Eliminates duplication and provides consistent test data setup
/// </summary>
public class TestDataSeeder
{
    private readonly IServiceProvider _serviceProvider;

    public TestDataSeeder(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Seed a user with builder support
    /// </summary>
    public async Task<User> SeedUserAsync(Action<UserBuilder>? configure = null)
    {
        var builder = new UserBuilder();
        configure?.Invoke(builder);
        var user = builder.Build();

        await using var scope = _serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<UsersDbContext>();
        
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        return user;
    }

    /// <summary>
    /// Seed multiple users
    /// </summary>
    public async Task<List<User>> SeedUsersAsync(params Action<UserBuilder>[] configurations)
    {
        var users = new List<User>();

        await using var scope = _serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<UsersDbContext>();

        foreach (var config in configurations)
        {
            var builder = new UserBuilder();
            config(builder);
            var user = builder.Build();
            dbContext.Users.Add(user);
            users.Add(user);
        }

        await dbContext.SaveChangesAsync();
        return users;
    }

    /// <summary>
    /// Seed a game with builder support
    /// </summary>
    public async Task<Game> SeedGameAsync(Action<GameBuilder>? configure = null)
    {
        var builder = new GameBuilder();
        configure?.Invoke(builder);
        var game = builder.Build();

        await using var scope = _serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GamesDbContext>();
        
        dbContext.Games.Add(game);
        await dbContext.SaveChangesAsync();

        return game;
    }

    /// <summary>
    /// Seed a game with host (creates user automatically)
    /// </summary>
    public async Task<(User host, Game game)> SeedGameWithHostAsync(
        string hostExternalId = "auth0|test-host",
        Action<GameBuilder>? configureGame = null)
    {
        // Create host user
        var host = await SeedUserAsync(u => u
            .WithExternalId(hostExternalId)
            .WithName("Host")
            .Intermediate());

        // Create game
        var game = await SeedGameAsync(g =>
        {
            g.WithHost(hostExternalId, "Host", "Intermediate");
            configureGame?.Invoke(g);
        });

        return (host, game);
    }

    /// <summary>
    /// Seed a complete game scenario with host and participants
    /// </summary>
    public async Task<GameScenario> SeedCompleteGameAsync(
        int participantCount = 0,
        Action<GameBuilder>? configureGame = null)
    {
        var hostId = "auth0|test-host";

        // Create host
        var host = await SeedUserAsync(u => u
            .WithExternalId(hostId)
            .WithName("Host")
            .Intermediate());

        // Create participants
        var participants = new List<User>();
        for (int i = 0; i < participantCount; i++)
        {
            var participant = await SeedUserAsync(u => u
                .WithExternalId($"auth0|player-{i + 1}")
                .WithName($"Player {i + 1}")
                .Intermediate());
            participants.Add(participant);
        }

        // Create game with participants
        var game = await SeedGameAsync(g =>
        {
            g.WithHost(hostId, "Host", "Intermediate");
            
            foreach (var participant in participants)
            {
                g.WithParticipant(
                    participant.ExternalId,
                    GetUserName(participant),
                    GetUserSkillLevel(participant));
            }

            configureGame?.Invoke(g);
        });

        return new GameScenario(host, participants, game);
    }

    /// <summary>
    /// Seed notification settings for a user
    /// </summary>
    public async Task<UserNotificationPreferences> SeedNotificationSettingsAsync(
        string userExternalId,
        string? deviceToken = null,
        bool pushEnabled = true,
        bool emailEnabled = false,
        bool smsEnabled = false)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();

        var settings = UserNotificationPreferences.CreateDefault(userExternalId);

        if (deviceToken != null)
        {
            settings.UpdateDeviceToken(deviceToken);
        }

        settings.UpdatePreferences(pushEnabled, smsEnabled, emailEnabled);

        dbContext.UserNotificationPreferences.Add(settings);
        await dbContext.SaveChangesAsync();

        return settings;
    }

    /// <summary>
    /// Query games database
    /// </summary>
    public async Task<T> QueryGamesAsync<T>(Func<GamesDbContext, Task<T>> query)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GamesDbContext>();
        return await query(dbContext);
    }

    /// <summary>
    /// Execute operations on games database with save
    /// </summary>
    public async Task ExecuteGamesAsync(Func<GamesDbContext, Task> operation)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GamesDbContext>();
        await operation(dbContext);
    }

    /// <summary>
    /// Query users database
    /// </summary>
    public async Task<T> QueryUsersAsync<T>(Func<UsersDbContext, Task<T>> query)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<UsersDbContext>();
        return await query(dbContext);
    }

    /// <summary>
    /// Query notifications database
    /// </summary>
    public async Task<T> QueryNotificationsAsync<T>(Func<NotificationsDbContext, Task<T>> query)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
        return await query(dbContext);
    }

    // Helper methods to extract user properties (adjust based on actual User class)
    private string GetUserName(User user)
    {
        var prop = typeof(User).GetProperty("Name");
        return prop?.GetValue(user) as string ?? "Unknown";
    }

    private string GetUserSkillLevel(User user)
    {
        var prop = typeof(User).GetProperty("SkillLevel");
        return prop?.GetValue(user) as string ?? "Intermediate";
    }
}

/// <summary>
/// Encapsulates a complete game test scenario
/// </summary>
public record GameScenario(
    User Host,
    List<User> Participants,
    Game Game);
