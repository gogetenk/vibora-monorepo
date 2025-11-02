# Implementation Guide: Search Games API

## Overview

**Feature**: Play (US29) - Game Search with Smart Matching
**Estimated Time**: 6 hours
**Priority**: P1 (MVP Critical)

This guide provides step-by-step instructions to implement the game search endpoint with tolerance-based matching.

---

## Prerequisites

Before starting, ensure:
- [x] You've read `ARCHITECTURE_ANALYSIS_SEARCH_API.md`
- [x] Backend is running: `dotnet run --project src/Vibora.AppHost`
- [x] Database is migrated (Aspire handles this automatically)
- [x] You have Swagger UI open: http://localhost:5000/swagger

---

## Phase 1: Create Query and Handler (3 hours)

### Step 1.1: Create Query File (15 min)

**File**: `F:\Repos\_perso\vibora-backend-v2\vibora-backend\src\modules\Games\Vibora.Games\Application\Queries\SearchGames\SearchGamesQuery.cs`

```csharp
using Ardalis.Result;
using MediatR;

namespace Vibora.Games.Application.Queries.SearchGames;

/// <summary>
/// Query to search games with tolerance-based matching
/// Implements smart matching with perfect (3 criteria) and partial (1-2 criteria) results
/// PO Spec: GET /games/search?when={iso8601}&where={string}&level={int}
/// </summary>
internal sealed record SearchGamesQuery(
    DateTime When,
    string Where,
    int? Level
) : IRequest<Result<SearchGamesQueryResponse>>;

/// <summary>
/// Response containing perfect matches (3/3 criteria) and partial matches (1-2/3 criteria)
/// </summary>
internal sealed record SearchGamesQueryResponse(
    List<GameMatchDto> PerfectMatches,  // All 3 criteria matched
    List<GameMatchDto> PartialMatches   // 1 or 2 criteria matched
);

/// <summary>
/// DTO for a matched game with its match score
/// Score: 3 = perfect match, 2 = good match, 1 = fair match
/// </summary>
internal sealed record GameMatchDto(
    Guid Id,
    DateTime DateTime,
    string Location,
    string SkillLevel,
    int MaxPlayers,
    int CurrentPlayers,
    string HostExternalId,
    string Status,
    int MatchScore  // 3 = perfect, 1-2 = partial
);
```

**Validation**:
```bash
cd F:\Repos\_perso\vibora-backend-v2\vibora-backend
dotnet build src/modules/Games/Vibora.Games/Vibora.Games.csproj
```

Expected: Build succeeds with no errors.

---

### Step 1.2: Create Handler File (2 hours)

**File**: `F:\Repos\_perso\vibora-backend-v2\vibora-backend\src\modules\Games\Vibora.Games\Application\Queries\SearchGames\SearchGamesQueryHandler.cs`

```csharp
using Ardalis.Result;
using MediatR;
using Vibora.Games.Domain;

namespace Vibora.Games.Application.Queries.SearchGames;

/// <summary>
/// Handler for game search with tolerance-based matching
/// Business Rules (from PO):
/// - Perfect Match (AC2): All 3 criteria (time ±2h, location exact, level ±1)
/// - Partial Match (AC3): At least 1 criterion (time ±4h, location exact, level ±2)
/// - Only returns Open games
/// - Limits: 3 perfect + 3 partial
/// </summary>
internal sealed class SearchGamesQueryHandler
    : IRequestHandler<SearchGamesQuery, Result<SearchGamesQueryResponse>>
{
    private readonly IGameRepository _gameRepository;

    public SearchGamesQueryHandler(IGameRepository gameRepository)
    {
        _gameRepository = gameRepository;
    }

    public async Task<Result<SearchGamesQueryResponse>> Handle(
        SearchGamesQuery request,
        CancellationToken cancellationToken)
    {
        // 1. Validate query parameters
        var validationErrors = ValidateQuery(request);
        if (validationErrors.Any())
        {
            return Result.Invalid(validationErrors);
        }

        // 2. Get all open games (future dates only)
        var openGames = await _gameRepository.GetOpenGamesAsync(
            DateTime.UtcNow,
            cancellationToken);

        // 3. Calculate match scores for each game
        var gamesWithScores = openGames
            .Select(g => new
            {
                Game = g,
                Score = CalculateMatchScore(g, request),
                IsPerfect = IsPerfectMatch(g, request)
            })
            .Where(x => x.Score > 0) // At least 1 criterion matches
            .ToList();

        // 4. Separate perfect (3/3) vs partial (1-2/3) matches
        var perfectMatches = gamesWithScores
            .Where(x => x.IsPerfect)
            .OrderBy(x => Math.Abs((x.Game.DateTime - request.When).TotalHours)) // Closest time first (AC2)
            .Take(3) // Max 3 perfect matches (AC2)
            .Select(x => MapToDto(x.Game, 3))
            .ToList();

        var partialMatches = gamesWithScores
            .Where(x => !x.IsPerfect)
            .OrderByDescending(x => x.Score) // Best score first (AC3)
            .ThenBy(x => Math.Abs((x.Game.DateTime - request.When).TotalHours)) // Then closest time (AC3)
            .Take(3) // Max 3 partial matches (AC3)
            .Select(x => MapToDto(x.Game, x.Score))
            .ToList();

        return Result.Success(new SearchGamesQueryResponse(perfectMatches, partialMatches));
    }

    /// <summary>
    /// Perfect match: All 3 criteria met (AC2)
    /// - Time: ±2 hours
    /// - Location: Case-insensitive exact match
    /// - Level: ±1 level (if provided)
    /// </summary>
    private bool IsPerfectMatch(Game game, SearchGamesQuery query)
    {
        // Time match: ±2 hours (AC2)
        var timeMatch = Math.Abs((game.DateTime - query.When).TotalHours) <= 2;

        // Location match: Case-insensitive exact match (AC2)
        var locationMatch = game.Location.Equals(query.Where, StringComparison.OrdinalIgnoreCase);

        // Level match: ±1 level (AC2)
        // If user didn't provide level, treat as "any level matches"
        bool levelMatch = true;
        if (query.Level.HasValue && int.TryParse(game.SkillLevel, out int gameLevel))
        {
            levelMatch = Math.Abs(gameLevel - query.Level.Value) <= 1;
        }

        return timeMatch && locationMatch && levelMatch;
    }

    /// <summary>
    /// Calculates match score (1-3) for partial matches (AC3)
    /// - 3 = Perfect (all criteria)
    /// - 2 = Good (2 criteria)
    /// - 1 = Fair (1 criterion)
    /// - 0 = No match
    ///
    /// Criteria for partial match (AC3):
    /// - Time: ±4 hours (wider than perfect)
    /// - Location: Case-insensitive exact match (same as perfect)
    /// - Level: ±2 levels (wider than perfect)
    /// </summary>
    private int CalculateMatchScore(Game game, SearchGamesQuery query)
    {
        int score = 0;

        // Time match: ±4 hours for partial (AC3)
        if (Math.Abs((game.DateTime - query.When).TotalHours) <= 4)
            score++;

        // Location match: Case-insensitive exact match (AC3)
        if (game.Location.Equals(query.Where, StringComparison.OrdinalIgnoreCase))
            score++;

        // Level match: ±2 levels for partial (AC3)
        if (query.Level.HasValue && int.TryParse(game.SkillLevel, out int gameLevel))
        {
            if (Math.Abs(gameLevel - query.Level.Value) <= 2)
                score++;
        }
        else if (!query.Level.HasValue)
        {
            // If no level provided, count as match (user doesn't care about level)
            score++;
        }

        return score;
    }

    /// <summary>
    /// Maps domain Game entity to DTO with match score
    /// </summary>
    private GameMatchDto MapToDto(Game game, int matchScore)
    {
        return new GameMatchDto(
            game.Id,
            game.DateTime,
            game.Location,
            game.SkillLevel,
            game.MaxPlayers,
            game.CurrentPlayers,
            game.HostExternalId,
            game.Status.ToString(),
            matchScore
        );
    }

    /// <summary>
    /// Validates query parameters before processing
    /// </summary>
    private List<ValidationError> ValidateQuery(SearchGamesQuery query)
    {
        var errors = new List<ValidationError>();

        // Don't allow searching for games too far in the past (1 hour tolerance)
        if (query.When < DateTime.UtcNow.AddHours(-1))
        {
            errors.Add(new ValidationError("Cannot search for games more than 1 hour in the past"));
        }

        if (string.IsNullOrWhiteSpace(query.Where))
        {
            errors.Add(new ValidationError("Location (where) is required"));
        }
        else if (query.Where.Length > 200)
        {
            errors.Add(new ValidationError("Location must not exceed 200 characters"));
        }

        if (query.Level.HasValue && (query.Level.Value < 1 || query.Level.Value > 10))
        {
            errors.Add(new ValidationError("Skill level must be between 1 and 10"));
        }

        return errors;
    }
}
```

**Validation**:
```bash
dotnet build src/modules/Games/Vibora.Games/Vibora.Games.csproj
```

Expected: Build succeeds with no errors.

---

### Step 1.3: Register Endpoint (45 min)

**File**: `F:\Repos\_perso\vibora-backend-v2\vibora-backend\src\modules\Games\Vibora.Games\Api\GameEndpoints.cs`

**1. Add using statement** (at top of file, around line 20):

```csharp
using Vibora.Games.Application.Queries.SearchGames;
```

**2. Register endpoint** (in `MapGameEndpoints()` method, after line 36):

```csharp
// Add this after GetAvailableGames endpoint (line 36)
gamesGroup.MapGet("/search", SearchGames)
    .WithName("SearchGames")
    .Produces<SearchGamesQueryResponse>(StatusCodes.Status200OK)
    .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
    .AllowAnonymous(); // Guest-friendly (PO requirement - zero-friction onboarding)
```

**3. Add handler method** (at end of class, before request DTOs around line 395):

```csharp
// GET /games/search - Smart search with tolerance-based matching
private static async Task<HttpResult> SearchGames(
    [FromQuery] string when,
    [FromQuery] string where,
    [FromQuery] int? level,
    ISender sender)
{
    // Parse ISO 8601 datetime
    if (!DateTime.TryParse(when, out var dateTime))
    {
        return Results.BadRequest(new
        {
            error = "Invalid 'when' parameter. Expected ISO 8601 format (e.g., 2025-10-25T19:00:00Z)",
            example = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-ddTHH:mm:ssZ")
        });
    }

    var query = new SearchGamesQuery(dateTime, where, level);
    var result = await sender.Send(query);

    return result.IsSuccess
        ? Results.Ok(result.Value)
        : result.ToMinimalApiResult();
}
```

**Validation**:
```bash
dotnet build Vibora.sln
```

Expected: Build succeeds with no errors.

---

### Step 1.4: Manual Test via Swagger (30 min)

**1. Start the application**:
```bash
cd F:\Repos\_perso\vibora-backend-v2\vibora-backend
dotnet run --project src/Vibora.AppHost/Vibora.AppHost.csproj
```

**2. Open Swagger UI**: http://localhost:5000/swagger

**3. Test the new endpoint**:
   - Find `GET /games/search` in the list
   - Click "Try it out"
   - Fill parameters:
     - `when`: `2025-10-31T19:00:00Z` (tomorrow evening)
     - `where`: `Club Paris`
     - `level`: `5`
   - Click "Execute"

**Expected Response** (if no data exists):
```json
{
  "perfectMatches": [],
  "partialMatches": []
}
```

**4. Seed test data via Swagger**:
   - Use `POST /games` to create a test game:
     ```json
     {
       "dateTime": "2025-10-31T19:00:00Z",
       "location": "Club Paris",
       "skillLevel": 5,
       "maxPlayers": 4
     }
     ```
   - You'll need a JWT token (create via `POST /users/sync` or use existing test user)

**5. Retry search**:
   - Should now return 1 perfect match with `matchScore: 3`

**Success Criteria**:
- [ ] Endpoint returns 200 OK
- [ ] Response has `perfectMatches` and `partialMatches` arrays
- [ ] Perfect match has `matchScore: 3`
- [ ] Invalid `when` parameter returns 400 Bad Request

---

## Phase 2: Create Integration Tests (2 hours)

### Step 2.1: Create Test File (1.5 hours)

**File**: `F:\Repos\_perso\vibora-backend-v2\vibora-backend\tests\Vibora.Integration.Tests\Games\SearchGamesIntegrationTests.cs`

```csharp
using System.Net;
using FluentAssertions;
using Vibora.Integration.Tests.Infrastructure;

namespace Vibora.Integration.Tests.Games;

public class SearchGamesIntegrationTests : IntegrationTestBaseImproved
{
    public SearchGamesIntegrationTests(ViboraWebApplicationFactory factory) : base(factory)
    {
    }

    #region Perfect Matches Tests

    [Fact]
    public async Task SearchGames_WithAllCriteriaMet_ShouldReturnPerfectMatches()
    {
        // Arrange - Seed games
        var host = await Seeder.SeedUserAsync(u => u.WithExternalId("auth0|host-perfect").Intermediate());

        var targetDate = DateTime.UtcNow.AddDays(2);

        // Perfect match: Time ±1h, Location exact, Level exact (5)
        await Seeder.SeedGameAsync(g => g
            .WithHost(host.ExternalId)
            .At(targetDate.AddHours(1)) // Within ±2h
            .AtLocation("Club Paris")
            .WithSkillLevel("5")
            .Open());

        // Partial match: Time OK, Location wrong
        await Seeder.SeedGameAsync(g => g
            .WithHost(host.ExternalId)
            .At(targetDate.AddHours(1))
            .AtLocation("Club Lyon") // Different location
            .WithSkillLevel("5")
            .Open());

        // Act - Search for perfect match
        var when = targetDate.ToString("yyyy-MM-ddTHH:mm:ssZ");
        var response = await Client.GetAsync($"/games/search?when={when}&where=Club Paris&level=5");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.ReadAsAsync<SearchGamesResponse>();
        result.Should().NotBeNull();
        result!.PerfectMatches.Should().HaveCount(1);
        result.PerfectMatches[0].Location.Should().Be("Club Paris");
        result.PerfectMatches[0].MatchScore.Should().Be(3);

        result.PartialMatches.Should().HaveCount(1);
        result.PartialMatches[0].Location.Should().Be("Club Lyon");
        result.PartialMatches[0].MatchScore.Should().BeLessThan(3);
    }

    [Fact]
    public async Task SearchGames_WithTimeExactlyPlus2Hours_ShouldBePerfectMatch()
    {
        // Arrange
        var host = await Seeder.SeedUserAsync(u => u.WithExternalId("auth0|host-time").Intermediate());

        var targetDate = DateTime.UtcNow.AddDays(2);

        // Game at exactly +2h (boundary test)
        await Seeder.SeedGameAsync(g => g
            .WithHost(host.ExternalId)
            .At(targetDate.AddHours(2)) // Exactly ±2h
            .AtLocation("Club Paris")
            .WithSkillLevel("5")
            .Open());

        // Act
        var when = targetDate.ToString("yyyy-MM-ddTHH:mm:ssZ");
        var response = await Client.GetAsync($"/games/search?when={when}&where=Club Paris&level=5");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.ReadAsAsync<SearchGamesResponse>();
        result!.PerfectMatches.Should().HaveCount(1);
        result.PerfectMatches[0].MatchScore.Should().Be(3);
    }

    [Fact]
    public async Task SearchGames_WithLevelPlusOrMinus1_ShouldBePerfectMatch()
    {
        // Arrange
        var host = await Seeder.SeedUserAsync(u => u.WithExternalId("auth0|host-level").Intermediate());

        var targetDate = DateTime.UtcNow.AddDays(2);

        // Game with level 6 (user searches for level 5, within ±1)
        await Seeder.SeedGameAsync(g => g
            .WithHost(host.ExternalId)
            .At(targetDate.AddHours(1))
            .AtLocation("Club Paris")
            .WithSkillLevel("6") // Level 6 (user level 5 ± 1 → accepts 4-6)
            .Open());

        // Act
        var when = targetDate.ToString("yyyy-MM-ddTHH:mm:ssZ");
        var response = await Client.GetAsync($"/games/search?when={when}&where=Club Paris&level=5");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.ReadAsAsync<SearchGamesResponse>();
        result!.PerfectMatches.Should().HaveCount(1);
        result.PerfectMatches[0].MatchScore.Should().Be(3);
    }

    [Fact]
    public async Task SearchGames_WithCaseInsensitiveLocation_ShouldBePerfectMatch()
    {
        // Arrange
        var host = await Seeder.SeedUserAsync(u => u.WithExternalId("auth0|host-case").Intermediate());

        var targetDate = DateTime.UtcNow.AddDays(2);

        // Game with "CLUB PARIS" (uppercase)
        await Seeder.SeedGameAsync(g => g
            .WithHost(host.ExternalId)
            .At(targetDate.AddHours(1))
            .AtLocation("CLUB PARIS") // Uppercase
            .WithSkillLevel("5")
            .Open());

        // Act - Search with lowercase
        var when = targetDate.ToString("yyyy-MM-ddTHH:mm:ssZ");
        var response = await Client.GetAsync($"/games/search?when={when}&where=club paris&level=5");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.ReadAsAsync<SearchGamesResponse>();
        result!.PerfectMatches.Should().HaveCount(1);
    }

    #endregion

    #region Partial Matches Tests

    [Fact]
    public async Task SearchGames_WithOnlyLocationMatch_ShouldReturnPartialMatch()
    {
        // Arrange
        var host = await Seeder.SeedUserAsync(u => u.WithExternalId("auth0|host-location").Intermediate());

        var targetDate = DateTime.UtcNow.AddDays(2);

        // Game with location match only (time +5h, level 8)
        await Seeder.SeedGameAsync(g => g
            .WithHost(host.ExternalId)
            .At(targetDate.AddHours(5)) // Outside ±2h but inside ±4h
            .AtLocation("Club Paris")
            .WithSkillLevel("8") // Outside ±1 but inside ±2
            .Open());

        // Act
        var when = targetDate.ToString("yyyy-MM-ddTHH:mm:ssZ");
        var response = await Client.GetAsync($"/games/search?when={when}&where=Club Paris&level=5");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.ReadAsAsync<SearchGamesResponse>();
        result!.PerfectMatches.Should().BeEmpty();
        result.PartialMatches.Should().HaveCount(1);
        result.PartialMatches[0].MatchScore.Should().Be(1); // Only location matches
    }

    [Fact]
    public async Task SearchGames_WithTwoCriteria_ShouldReturnPartialMatchWithScore2()
    {
        // Arrange
        var host = await Seeder.SeedUserAsync(u => u.WithExternalId("auth0|host-two").Intermediate());

        var targetDate = DateTime.UtcNow.AddDays(2);

        // Game with 2/3 criteria (time OK, location OK, level wrong)
        await Seeder.SeedGameAsync(g => g
            .WithHost(host.ExternalId)
            .At(targetDate.AddHours(3)) // Inside ±4h (partial)
            .AtLocation("Club Paris") // Match
            .WithSkillLevel("9") // Outside ±2
            .Open());

        // Act
        var when = targetDate.ToString("yyyy-MM-ddTHH:mm:ssZ");
        var response = await Client.GetAsync($"/games/search?when={when}&where=Club Paris&level=5");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.ReadAsAsync<SearchGamesResponse>();
        result!.PartialMatches.Should().HaveCount(1);
        result.PartialMatches[0].MatchScore.Should().Be(2); // Time + Location
    }

    #endregion

    #region Edge Cases Tests

    [Fact]
    public async Task SearchGames_WithNoCriteriaMet_ShouldReturnEmptyResults()
    {
        // Arrange
        var host = await Seeder.SeedUserAsync(u => u.WithExternalId("auth0|host-none").Intermediate());

        var targetDate = DateTime.UtcNow.AddDays(2);

        // Game with no criteria matching (time +10h, location wrong, level wrong)
        await Seeder.SeedGameAsync(g => g
            .WithHost(host.ExternalId)
            .At(targetDate.AddHours(10)) // Outside ±4h
            .AtLocation("Club Lyon")
            .WithSkillLevel("10") // Outside ±2
            .Open());

        // Act
        var when = targetDate.ToString("yyyy-MM-ddTHH:mm:ssZ");
        var response = await Client.GetAsync($"/games/search?when={when}&where=Club Paris&level=5");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.ReadAsAsync<SearchGamesResponse>();
        result!.PerfectMatches.Should().BeEmpty();
        result.PartialMatches.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchGames_WithMoreThan3PerfectMatches_ShouldLimitTo3()
    {
        // Arrange
        var host = await Seeder.SeedUserAsync(u => u.WithExternalId("auth0|host-limit").Intermediate());

        var targetDate = DateTime.UtcNow.AddDays(2);

        // Create 5 perfect matches
        for (int i = 0; i < 5; i++)
        {
            await Seeder.SeedGameAsync(g => g
                .WithHost(host.ExternalId)
                .At(targetDate.AddHours(i * 0.5)) // Spread over 2.5 hours (all within ±2h)
                .AtLocation("Club Paris")
                .WithSkillLevel("5")
                .Open());
        }

        // Act
        var when = targetDate.ToString("yyyy-MM-ddTHH:mm:ssZ");
        var response = await Client.GetAsync($"/games/search?when={when}&where=Club Paris&level=5");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.ReadAsAsync<SearchGamesResponse>();
        result!.PerfectMatches.Should().HaveCount(3); // Limited to 3 (AC2)
    }

    [Fact]
    public async Task SearchGames_WithNoLevelProvided_ShouldMatchAnyLevel()
    {
        // Arrange
        var host = await Seeder.SeedUserAsync(u => u.WithExternalId("auth0|host-nolevel").Intermediate());

        var targetDate = DateTime.UtcNow.AddDays(2);

        // Game with level 10 (would not match if level filter was applied)
        await Seeder.SeedGameAsync(g => g
            .WithHost(host.ExternalId)
            .At(targetDate.AddHours(1))
            .AtLocation("Club Paris")
            .WithSkillLevel("10")
            .Open());

        // Act - No level parameter
        var when = targetDate.ToString("yyyy-MM-ddTHH:mm:ssZ");
        var response = await Client.GetAsync($"/games/search?when={when}&where=Club Paris");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.ReadAsAsync<SearchGamesResponse>();
        result!.PerfectMatches.Should().HaveCount(1); // Matches because no level filter
    }

    #endregion

    #region Ordering Tests

    [Fact]
    public async Task SearchGames_PerfectMatches_ShouldOrderByClosestTime()
    {
        // Arrange
        var host = await Seeder.SeedUserAsync(u => u.WithExternalId("auth0|host-order").Intermediate());

        var targetDate = DateTime.UtcNow.AddDays(2);

        // Create 3 perfect matches with different times
        await Seeder.SeedGameAsync(g => g
            .WithHost(host.ExternalId)
            .At(targetDate.AddHours(2)) // Furthest
            .AtLocation("Club Paris")
            .WithSkillLevel("5")
            .Open());

        await Seeder.SeedGameAsync(g => g
            .WithHost(host.ExternalId)
            .At(targetDate.AddMinutes(30)) // Closest
            .AtLocation("Club Paris")
            .WithSkillLevel("5")
            .Open());

        await Seeder.SeedGameAsync(g => g
            .WithHost(host.ExternalId)
            .At(targetDate.AddHours(1)) // Middle
            .AtLocation("Club Paris")
            .WithSkillLevel("5")
            .Open());

        // Act
        var when = targetDate.ToString("yyyy-MM-ddTHH:mm:ssZ");
        var response = await Client.GetAsync($"/games/search?when={when}&where=Club Paris&level=5");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.ReadAsAsync<SearchGamesResponse>();
        result!.PerfectMatches.Should().HaveCount(3);

        // Verify ordering: Closest first
        var times = result.PerfectMatches.Select(m => m.DateTime).ToList();
        times[0].Should().Be(targetDate.AddMinutes(30));
        times[1].Should().Be(targetDate.AddHours(1));
        times[2].Should().Be(targetDate.AddHours(2));
    }

    [Fact]
    public async Task SearchGames_PartialMatches_ShouldOrderByScoreThenTime()
    {
        // Arrange
        var host = await Seeder.SeedUserAsync(u => u.WithExternalId("auth0|host-partial").Intermediate());

        var targetDate = DateTime.UtcNow.AddDays(2);

        // Game 1: Score 2 (time + location), time +3h
        await Seeder.SeedGameAsync(g => g
            .WithHost(host.ExternalId)
            .At(targetDate.AddHours(3))
            .AtLocation("Club Paris")
            .WithSkillLevel("9") // No level match
            .Open());

        // Game 2: Score 1 (location only), time +5h
        await Seeder.SeedGameAsync(g => g
            .WithHost(host.ExternalId)
            .At(targetDate.AddHours(5))
            .AtLocation("Club Paris")
            .WithSkillLevel("10") // No level match
            .Open());

        // Game 3: Score 2 (time + level), time +2.5h (closer than Game 1)
        await Seeder.SeedGameAsync(g => g
            .WithHost(host.ExternalId)
            .At(targetDate.AddHours(2.5))
            .AtLocation("Club Lyon") // No location match
            .WithSkillLevel("5")
            .Open());

        // Act
        var when = targetDate.ToString("yyyy-MM-ddTHH:mm:ssZ");
        var response = await Client.GetAsync($"/games/search?when={when}&where=Club Paris&level=5");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.ReadAsAsync<SearchGamesResponse>();
        result!.PartialMatches.Should().HaveCount(3);

        // Verify ordering: Score DESC, then time ASC
        result.PartialMatches[0].MatchScore.Should().Be(2); // Game 3 (score 2, closest)
        result.PartialMatches[1].MatchScore.Should().Be(2); // Game 1 (score 2, further)
        result.PartialMatches[2].MatchScore.Should().Be(1); // Game 2 (score 1)
    }

    #endregion

    #region Validation Tests

    [Fact]
    public async Task SearchGames_WithPastDate_ShouldReturnBadRequest()
    {
        // Arrange
        var pastDate = DateTime.UtcNow.AddDays(-2);

        // Act
        var when = pastDate.ToString("yyyy-MM-ddTHH:mm:ssZ");
        var response = await Client.GetAsync($"/games/search?when={when}&where=Club Paris&level=5");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SearchGames_WithInvalidLevel_ShouldReturnBadRequest()
    {
        // Arrange
        var targetDate = DateTime.UtcNow.AddDays(2);

        // Act - Invalid level (11 > 10)
        var when = targetDate.ToString("yyyy-MM-ddTHH:mm:ssZ");
        var response = await Client.GetAsync($"/games/search?when={when}&where=Club Paris&level=11");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SearchGames_WithEmptyLocation_ShouldReturnBadRequest()
    {
        // Arrange
        var targetDate = DateTime.UtcNow.AddDays(2);

        // Act - Empty location
        var when = targetDate.ToString("yyyy-MM-ddTHH:mm:ssZ");
        var response = await Client.GetAsync($"/games/search?when={when}&where=&level=5");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Status Filter Tests

    [Fact]
    public async Task SearchGames_ShouldOnlyReturnOpenGames()
    {
        // Arrange
        var host = await Seeder.SeedUserAsync(u => u.WithExternalId("auth0|host-status").Intermediate());

        var targetDate = DateTime.UtcNow.AddDays(2);

        // Open game (should appear)
        await Seeder.SeedGameAsync(g => g
            .WithHost(host.ExternalId)
            .At(targetDate.AddHours(1))
            .AtLocation("Club Paris")
            .WithSkillLevel("5")
            .Open());

        // Full game (should NOT appear - per PO AC2)
        await Seeder.SeedGameAsync(g => g
            .WithHost(host.ExternalId)
            .At(targetDate.AddHours(1))
            .AtLocation("Club Paris")
            .WithSkillLevel("5")
            .WithMaxPlayers(4)
            .WithParticipants(4)); // Full game

        // Act
        var when = targetDate.ToString("yyyy-MM-ddTHH:mm:ssZ");
        var response = await Client.GetAsync($"/games/search?when={when}&where=Club Paris&level=5");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.ReadAsAsync<SearchGamesResponse>();
        result!.PerfectMatches.Should().HaveCount(1); // Only Open game
        result.PerfectMatches[0].Status.Should().Be("Open");
    }

    [Fact]
    public async Task SearchGames_ShouldNotReturnCanceledGames()
    {
        // Arrange
        var host = await Seeder.SeedUserAsync(u => u.WithExternalId("auth0|host-canceled").Intermediate());

        var targetDate = DateTime.UtcNow.AddDays(2);

        // Canceled game (should NOT appear)
        await Seeder.SeedGameAsync(g => g
            .WithHost(host.ExternalId)
            .At(targetDate.AddHours(1))
            .AtLocation("Club Paris")
            .WithSkillLevel("5")
            .Canceled());

        // Act
        var when = targetDate.ToString("yyyy-MM-ddTHH:mm:ssZ");
        var response = await Client.GetAsync($"/games/search?when={when}&where=Club Paris&level=5");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.ReadAsAsync<SearchGamesResponse>();
        result!.PerfectMatches.Should().BeEmpty();
        result.PartialMatches.Should().BeEmpty();
    }

    #endregion

    // Response DTOs
    private record SearchGamesResponse(
        List<GameMatchResponse> PerfectMatches,
        List<GameMatchResponse> PartialMatches
    );

    private record GameMatchResponse(
        Guid Id,
        DateTime DateTime,
        string Location,
        string SkillLevel,
        int MaxPlayers,
        int CurrentPlayers,
        string HostExternalId,
        string Status,
        int MatchScore
    );
}
```

**Note**: Some methods like `.WithParticipants()` may not exist in your Seeder. Adjust based on your test infrastructure.

---

### Step 2.2: Run Tests (30 min)

**Run all search tests**:
```bash
cd F:\Repos\_perso\vibora-backend-v2\vibora-backend
dotnet test --filter SearchGamesIntegrationTests
```

**Expected Output**:
```
Total tests: 18
Passed: 18
Failed: 0
```

**If tests fail**:
1. Check test output for specific error
2. Verify handler logic matches test expectations
3. Debug with breakpoints in handler

**Common Issues**:
- DateTime comparison precision (use `.AddMinutes()` instead of `.AddHours()` for closer boundaries)
- Case sensitivity in location matching
- Off-by-one errors in level matching

---

## Phase 3: Documentation (1 hour)

### Step 3.1: Update Swagger Annotations (15 min)

**File**: `F:\Repos\_perso\vibora-backend-v2\vibora-backend\src\modules\Games\Vibora.Games\Api\GameEndpoints.cs`

Enhance endpoint registration with better documentation:

```csharp
gamesGroup.MapGet("/search", SearchGames)
    .WithName("SearchGames")
    .WithDescription("Smart search for games with tolerance-based matching")
    .WithSummary("Search games by time (±2h perfect, ±4h partial), location (exact), and skill level (±1 perfect, ±2 partial)")
    .WithTags("Games")
    .Produces<SearchGamesQueryResponse>(StatusCodes.Status200OK, "application/json")
    .Produces<ProblemDetails>(StatusCodes.Status400BadRequest, "application/json")
    .AllowAnonymous(); // Guest-friendly
```

---

### Step 3.2: Update API Reference (30 min)

**File**: `F:\Repos\_perso\vibora-backend-v2\vibora-backend\docs\Vibora API (MVP) – REST Endpoint Reference.md`

Add to the **Games** section:

```markdown
### `GET /games/search` - Smart Game Search (US29)

**Description**: Search for games with tolerance-based matching. Returns perfect matches (all 3 criteria) and partial matches (1-2 criteria).

**Authentication**: Not required (guest-friendly)

**Query Parameters**:
| Parameter | Type | Required | Description | Example |
|-----------|------|----------|-------------|---------|
| `when` | ISO 8601 | Yes | Target date/time for the game | `2025-10-31T19:00:00Z` |
| `where` | string | Yes | Location (exact match, case-insensitive) | `Club Paris` |
| `level` | int | No | Skill level (1-10) | `5` |

**Response (200 OK)**:
```json
{
  "perfectMatches": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "dateTime": "2025-10-31T19:00:00Z",
      "location": "Club Paris",
      "skillLevel": "5",
      "maxPlayers": 4,
      "currentPlayers": 2,
      "hostExternalId": "auth0|abc123",
      "status": "Open",
      "matchScore": 3
    }
  ],
  "partialMatches": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa7",
      "dateTime": "2025-10-31T21:00:00Z",
      "location": "Club Lyon",
      "skillLevel": "6",
      "maxPlayers": 4,
      "currentPlayers": 1,
      "hostExternalId": "auth0|def456",
      "status": "Open",
      "matchScore": 2
    }
  ]
}
```

**Matching Rules**:
- **Perfect Match** (score = 3): All 3 criteria met
  - Time: ±2 hours
  - Location: Exact match (case-insensitive)
  - Level: ±1 level (if provided)
- **Partial Match** (score = 1-2): At least 1 criterion met
  - Time: ±4 hours
  - Location: Exact match (case-insensitive)
  - Level: ±2 levels (if provided)

**Ordering**:
- Perfect matches: Closest time first (max 3 results)
- Partial matches: Best score first, then closest time (max 3 results)

**Validation Errors (400 Bad Request)**:
- `when` is more than 1 hour in the past
- `where` is empty or > 200 characters
- `level` is < 1 or > 10

**Example**:
```bash
curl -X GET "http://localhost:5000/games/search?when=2025-10-31T19:00:00Z&where=Club%20Paris&level=5"
```
```

---

### Step 3.3: Create Frontend Integration Guide (15 min)

**File**: `F:\Repos\_perso\vibora-backend-v2\vibora-backend\docs\FRONTEND_INTEGRATION_SEARCH_API.md`

```markdown
# Frontend Integration: Search Games API

## TypeScript Types

Add to `vibora-frontend/lib/api/vibora-types.ts`:

```typescript
// Request
export interface SearchGamesRequest {
  when: string; // ISO 8601 format: "2025-10-31T19:00:00Z"
  where: string; // Location (e.g., "Club Paris")
  level?: number; // Skill level 1-10 (optional)
}

// Response
export interface SearchGamesResponse {
  perfectMatches: GameMatch[];
  partialMatches: GameMatch[];
}

export interface GameMatch {
  id: string;
  dateTime: string; // ISO 8601
  location: string;
  skillLevel: string;
  maxPlayers: number;
  currentPlayers: number;
  hostExternalId: string;
  status: string;
  matchScore: number; // 1-3
}
```

## API Client

Add to `vibora-frontend/lib/api/vibora-client.ts`:

```typescript
export const viboraApi = {
  games: {
    // ... existing methods ...

    async searchGames(request: SearchGamesRequest): Promise<ApiResponse<SearchGamesResponse>> {
      try {
        const headers = await getViboraAuthHeaders();
        const params = new URLSearchParams({
          when: request.when,
          where: request.where,
          ...(request.level && { level: request.level.toString() })
        });

        const response = await fetch(
          `${process.env.NEXT_PUBLIC_VIBORA_API_URL}/games/search?${params}`,
          { headers }
        );

        if (!response.ok) {
          const error = await response.json();
          return { data: null, error: error.message || "Search failed" };
        }

        const data = await response.json();
        return { data, error: null };
      } catch (error) {
        return { data: null, error: "Network error" };
      }
    }
  }
};
```

## Usage Example

```typescript
// app/search/page.tsx
"use client";

import { useState } from "react";
import { viboraApi, SearchGamesRequest } from "@/lib/api/vibora-client";

export default function SearchPage() {
  const [results, setResults] = useState<SearchGamesResponse | null>(null);

  const handleSearch = async () => {
    const request: SearchGamesRequest = {
      when: "2025-10-31T19:00:00Z",
      where: "Club Paris",
      level: 5
    };

    const { data, error } = await viboraApi.games.searchGames(request);
    if (data) setResults(data);
  };

  return (
    <div>
      <button onClick={handleSearch}>Search</button>

      {results && (
        <>
          <h2>Perfect Matches ({results.perfectMatches.length})</h2>
          {results.perfectMatches.map(game => (
            <GameCard key={game.id} game={game} />
          ))}

          <h2>Partial Matches ({results.partialMatches.length})</h2>
          {results.partialMatches.map(game => (
            <GameCard key={game.id} game={game} />
          ))}
        </>
      )}
    </div>
  );
}
```

## Notes
- No authentication required (guest-friendly)
- Add debouncing on search input (500ms) to reduce API calls
- Show loading skeleton while searching
- Display match scores as badges (3 stars = perfect, 1-2 stars = partial)
```

---

## Final Validation

Before submitting, verify:

### Code Checklist
- [ ] `SearchGamesQuery.cs` created with correct namespace
- [ ] `SearchGamesQueryHandler.cs` created with matching logic
- [ ] `GameEndpoints.cs` updated with new endpoint
- [ ] Endpoint registered with `.AllowAnonymous()`
- [ ] Solution builds without errors

### Test Checklist
- [ ] `SearchGamesIntegrationTests.cs` created with 18 tests
- [ ] All tests pass: `dotnet test --filter SearchGamesIntegrationTests`
- [ ] Manual test via Swagger confirms endpoint works
- [ ] Test with no data returns empty arrays (not null)

### Documentation Checklist
- [ ] Swagger UI shows new endpoint with description
- [ ] API reference doc updated
- [ ] Frontend integration guide created
- [ ] All acceptance criteria (AC2 + AC3) documented

---

## Troubleshooting

### Issue: Build fails with "Type or namespace not found"
**Solution**: Verify using statements at top of `GameEndpoints.cs`:
```csharp
using Vibora.Games.Application.Queries.SearchGames;
```

### Issue: Tests fail with "Method not found: WithParticipants"
**Solution**: Replace with manual seeding:
```csharp
var game = await Seeder.SeedGameAsync(...);
for (int i = 0; i < 4; i++)
{
    var player = await Seeder.SeedUserAsync(...);
    await Client.PostAsync($"/games/{game.Id}/players", ...);
}
```

### Issue: Endpoint returns 404
**Solution**: Verify endpoint registration in `MapGameEndpoints()` method. Restart application.

### Issue: DateTime comparison fails
**Solution**: Use `DateTime.UtcNow` consistently. Avoid local time.

---

## Success Criteria

**You're done when**:
1. All 18 integration tests pass
2. Swagger UI shows `/games/search` endpoint
3. Manual test returns correct perfect/partial matches
4. Documentation is complete

**Estimated Total Time**: 6 hours

---

**Questions?** Contact Backend Architect for clarification.

**Next Steps**: Frontend developer will integrate using `FRONTEND_INTEGRATION_SEARCH_API.md`.
