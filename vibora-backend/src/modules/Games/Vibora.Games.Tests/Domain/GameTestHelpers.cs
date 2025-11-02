using Vibora.Games.Domain;

namespace Vibora.Games.Tests.Domain;

public static class GameTestHelpers
{
    public static Game CreateValidGame(int maxPlayers = 4, string skillLevel = "5")
    {
        var result = Game.Create(
            "auth0|host123",
            "Host User",
            "5",
            DateTime.UtcNow.AddDays(1),
            "Test Club",
            skillLevel,
            maxPlayers);

        return result.Value;
    }
}
