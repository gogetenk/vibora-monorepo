using Vibora.Games.Domain;

namespace Vibora.Games.Tests.TestHelpers;

/// <summary>
/// Test fixture for creating valid Game instances for testing
/// </summary>
public static class GameTestFixture
{
    internal static Game CreateValidGame(
        string? hostExternalId = null,
        string? hostName = null,
        string? hostSkillLevel = null,
        DateTime? dateTime = null,
        string? location = null,
        string? skillLevel = null,
        int? maxPlayers = null)
    {
        var result = Game.Create(
            hostExternalId ?? "auth0|testhost123",
            hostName ?? "Test Host",
            hostSkillLevel ?? "Advanced",
            dateTime ?? DateTime.UtcNow.AddDays(1),
            location ?? "Test Club",
            skillLevel ?? "Intermediate",
            maxPlayers ?? 4);

        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(
                $"Failed to create test game: {string.Join(", ", result.Errors)}");
        }

        return result.Value;
    }

    internal static Game CreateFullGame(int maxPlayers = 4)
    {
        var game = CreateValidGame(maxPlayers: maxPlayers);

        // Add participants until full
        for (int i = 1; i < maxPlayers; i++)
        {
            game.AddParticipant($"auth0|player{i}", $"Player {i}", "Intermediate");
        }

        return game;
    }

    internal static Game CreateCanceledGame()
    {
        var game = CreateValidGame();
        game.Cancel();
        return game;
    }
}
