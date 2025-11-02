# Architecture Analysis: Search Games API (US29 - Play Feature)

## Executive Summary

**Date**: 2025-10-30
**Architect**: Backend Architect Agent
**Feature**: Play (US29) - Game Search with Smart Matching
**Priority**: P1 (MVP Critical)

### Key Findings

1. **Existing API Assessment**: PARTIAL - The backend has `GET /games` with basic filtering, but it does NOT meet the PO's matching requirements
2. **Architecture Gap**: Current `GetAvailableGamesQuery` uses exact/partial string matching. PO requires **tolerance-based matching** (±2h, ±1 level) with priority scoring
3. **Recommendation**: **CREATE NEW ENDPOINT** `GET /games/search` with dedicated handler that implements smart matching logic
4. **Estimated Effort**: 4-6 hours (query/handler/tests)

---

## 1. Analysis of Existing Backend

### 1.1 Current State

#### Endpoint: `GET /games`
**Location**: `F:\Repos\_perso\vibora-backend-v2\vibora-backend\src\modules\Games\Vibora.Games\Api\GameEndpoints.cs:156-180`

**Signature**:
```csharp
GET /games?location={string}&skillLevel={string}&fromDate={iso8601}&toDate={iso8601}&pageNumber={int}&pageSize={int}
```

**Capabilities**:
- Filters by **exact location** substring (`Location.Contains(location)`)
- Filters by **exact skill level** string match (`SkillLevel == skillLevel`)
- Filters by **date range** (from/to)
- Returns games with status `Open` or `Full`
- Pagination support

**Limitations**:
- NO tolerance-based time matching (requires ±2h window)
- NO numeric skill level matching with ±1 tolerance
- NO match scoring or prioritization
- NO separation between "perfect" and "partial" matches
- Returns mixed results without quality indicators

### 1.2 Repository Layer

**Location**: `F:\Repos\_perso\vibora-backend-v2\vibora-backend\src\modules\Games\Vibora.Games\Infrastructure\Persistence\GameRepository.cs:88-135`

**Method**: `GetAvailableGamesAsync()`

**Current Implementation**:
```csharp
public async Task<(List<Game> Games, int TotalCount)> GetAvailableGamesAsync(
    string? location = null,
    string? skillLevel = null,
    DateTime? fromDate = null,
    DateTime? toDate = null,
    int pageNumber = 1,
    int pageSize = 20,
    CancellationToken cancellationToken = default)
{
    var query = _dbContext.Games
        .AsNoTracking()
        .Where(g => g.Status == GameStatus.Open || g.Status == GameStatus.Full)
        .Where(g => g.DateTime > DateTime.UtcNow);

    if (!string.IsNullOrWhiteSpace(location))
        query = query.Where(g => g.Location.Contains(location));

    if (!string.IsNullOrWhiteSpace(skillLevel))
        query = query.Where(g => g.SkillLevel == skillLevel);

    if (fromDate.HasValue)
        query = query.Where(g => g.DateTime >= fromDate.Value);

    if (toDate.HasValue)
        query = query.Where(g => g.DateTime <= toDate.Value);

    var totalCount = await query.CountAsync(cancellationToken);

    var games = await query
        .OrderBy(g => g.DateTime)
        .Skip((pageNumber - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync(cancellationToken);

    return (games, totalCount);
}
```

**Analysis**: This method is optimized for simple filtering but does NOT support:
- Tolerance-based matching (±2h, ±1 level)
- Match scoring
- "Best match first" ordering

---

## 2. PO Requirements Mapping

### 2.1 Backend Acceptance Criteria (AC2)

**Perfect Matches** (Score = 3):
| Criterion | PO Requirement | Current API Support | Gap |
|-----------|---------------|---------------------|-----|
| **DateTime** | ±2 hours tolerance | NO - Only exact range | MAJOR GAP |
| **Location** | Case-insensitive exact match | PARTIAL - Substring match | MINOR GAP |
| **Level** | ±1 level (e.g., level 5 → accepts 4-6) | NO - String exact match | MAJOR GAP |
| **Status** | Must be `Open` | YES - Already filtered | OK |
| **Ordering** | Closest DateTime first | YES - Already sorted | OK |
| **Limit** | Max 3 results | NO - Pagination only | MINOR GAP |

**Partial Matches** (AC3) (Score = 1-2):
| Criterion | PO Requirement | Current API Support | Gap |
|-----------|---------------|---------------------|-----|
| **Matching** | ≥1 criterion (location OR time ±4h OR level ±2) | NO - ALL filters are AND | MAJOR GAP |
| **Scoring** | Order by match count DESC | NO - No scoring | MAJOR GAP |
| **Ordering** | Then by DateTime ASC | YES - Already sorted | OK |
| **Limit** | Max 3 results | NO - Pagination only | MINOR GAP |

### 2.2 Endpoint Specification

**PO Spec**: `GET /games/search?when={iso8601}&where={string}&level={int}`

**Current API**: `GET /games?location={string}&skillLevel={string}&fromDate={iso8601}&toDate={iso8601}&pageNumber={int}&pageSize={int}`

**Differences**:
- Query param names differ (`when` vs `fromDate`, `where` vs `location`, `level` vs `skillLevel`)
- Current API accepts string skill level, PO wants integer
- Current API has pagination, PO wants fixed limits (3+3)
- Current API returns single list, PO wants two categories (perfect/partial)

---

## 3. Architecture Decision: Create New Endpoint

### 3.1 Rationale

**Option 1: Modify Existing `GET /games` Endpoint**
PROS:
- No new code
- Reuse existing tests

CONS:
- Breaking change for frontend (already using `/games`)
- Complex logic with backward compatibility
- Violates Single Responsibility (list vs search)

**Option 2: Create New `GET /games/search` Endpoint** ✅ RECOMMENDED
PROS:
- Clean separation of concerns (list vs search)
- No breaking changes to existing API
- Matches PO specification exactly
- Easier to test and maintain

CONS:
- Code duplication (minimal)
- One more endpoint to document

**Decision**: Create `GET /games/search` with dedicated CQRS query/handler.

---

## 4. Proposed Architecture Design

### 4.1 CQRS Structure

**Files to Create**:

```
vibora-backend/src/modules/Games/Vibora.Games/Application/Queries/SearchGames/
├── SearchGamesQuery.cs
├── SearchGamesQueryHandler.cs
└── SearchGamesQueryValidator.cs (optional - use guard clauses)
```

### 4.2 Query Definition

**File**: `SearchGamesQuery.cs`

```csharp
using Ardalis.Result;
using MediatR;

namespace Vibora.Games.Application.Queries.SearchGames;

/// <summary>
/// Query to search games with tolerance-based matching
/// Implements smart matching with perfect (3 criteria) and partial (1-2 criteria) results
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

### 4.3 Handler Implementation

**File**: `SearchGamesQueryHandler.cs`

```csharp
using Ardalis.Result;
using MediatR;
using Vibora.Games.Domain;

namespace Vibora.Games.Application.Queries.SearchGames;

/// <summary>
/// Handler for game search with tolerance-based matching
/// Business Rules:
/// - Perfect Match: All 3 criteria (time ±2h, location exact, level ±1)
/// - Partial Match: At least 1 criterion (time ±4h, location exact, level ±2)
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
        // Validation
        var validationErrors = ValidateQuery(request);
        if (validationErrors.Any())
        {
            return Result.Invalid(validationErrors);
        }

        // Get all open games (future dates only)
        var openGames = await _gameRepository.GetOpenGamesAsync(
            DateTime.UtcNow,
            cancellationToken);

        // Calculate match scores for each game
        var gamesWithScores = openGames
            .Select(g => new
            {
                Game = g,
                Score = CalculateMatchScore(g, request),
                IsPerfect = IsPerfectMatch(g, request)
            })
            .Where(x => x.Score > 0) // At least 1 criterion matches
            .ToList();

        // Separate perfect (3/3) vs partial (1-2/3) matches
        var perfectMatches = gamesWithScores
            .Where(x => x.IsPerfect)
            .OrderBy(x => Math.Abs((x.Game.DateTime - request.When).TotalHours)) // Closest time first
            .Take(3) // Max 3 perfect matches (AC2)
            .Select(x => MapToDto(x.Game, 3))
            .ToList();

        var partialMatches = gamesWithScores
            .Where(x => !x.IsPerfect)
            .OrderByDescending(x => x.Score) // Best score first
            .ThenBy(x => Math.Abs((x.Game.DateTime - request.When).TotalHours)) // Then closest time
            .Take(3) // Max 3 partial matches (AC3)
            .Select(x => MapToDto(x.Game, x.Score))
            .ToList();

        return Result.Success(new SearchGamesQueryResponse(perfectMatches, partialMatches));
    }

    /// <summary>
    /// Perfect match: All 3 criteria met
    /// - Time: ±2 hours
    /// - Location: Case-insensitive exact match
    /// - Level: ±1 level (if provided)
    /// </summary>
    private bool IsPerfectMatch(Game game, SearchGamesQuery query)
    {
        var timeMatch = Math.Abs((game.DateTime - query.When).TotalHours) <= 2;
        var locationMatch = game.Location.Equals(query.Where, StringComparison.OrdinalIgnoreCase);

        // Level matching: If user didn't provide level, treat as "any level matches"
        bool levelMatch = true;
        if (query.Level.HasValue && int.TryParse(game.SkillLevel, out int gameLevel))
        {
            levelMatch = Math.Abs(gameLevel - query.Level.Value) <= 1;
        }

        return timeMatch && locationMatch && levelMatch;
    }

    /// <summary>
    /// Calculates match score (1-3)
    /// - 3 = Perfect (all criteria)
    /// - 2 = Good (2 criteria)
    /// - 1 = Fair (1 criterion)
    /// - 0 = No match
    /// </summary>
    private int CalculateMatchScore(Game game, SearchGamesQuery query)
    {
        int score = 0;

        // Time match: ±4 hours for partial (AC3)
        if (Math.Abs((game.DateTime - query.When).TotalHours) <= 4)
            score++;

        // Location match: Case-insensitive exact match
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

### 4.4 Endpoint Registration

**File**: `GameEndpoints.cs`

**Add to `MapGameEndpoints()` method**:

```csharp
using Vibora.Games.Application.Queries.SearchGames;

// Add after line 36 (after GetAvailableGames)
gamesGroup.MapGet("/search", SearchGames)
    .WithName("SearchGames")
    .Produces<SearchGamesQueryResponse>(StatusCodes.Status200OK)
    .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
    .AllowAnonymous(); // Guest-friendly (PO requirement)

// Add at end of class (before helper records)
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
            error = "Invalid 'when' parameter. Expected ISO 8601 format (e.g., 2025-10-25T19:00:00Z)"
        });
    }

    var query = new SearchGamesQuery(dateTime, where, level);
    var result = await sender.Send(query);

    return result.IsSuccess
        ? Results.Ok(result.Value)
        : result.ToMinimalApiResult();
}
```

---

## 5. Architecture Validation

### 5.1 Module Boundaries ✅

- [x] Query handler in `Vibora.Games/Application/Queries/SearchGames/` (internal)
- [x] All classes `internal` except DTOs (if needed in Contracts)
- [x] No cross-module dependencies

**Verdict**: Clean architecture, respects module isolation.

### 5.2 Domain vs Application Logic ✅

**Question**: Should matching logic (`IsPerfectMatch`, `CalculateMatchScore`) be in Domain or Application?

**Answer**: **Application Layer** (correct placement)

**Rationale**:
- This is **query/filtering logic**, not a business rule of the Game aggregate
- Domain layer defines **invariants** (e.g., "Game cannot have > MaxPlayers")
- Search matching is a **read concern** (CQRS query side)
- No state mutation, no aggregate rules violated

**Alternative Considered**: Create `GameMatcher` domain service
**Rejected**: Over-engineering for MVP. Keep it simple in Application layer.

### 5.3 Performance ✅ (MVP Acceptable)

**Current Implementation**: In-memory filtering (LINQ)

**Pros**:
- Simple, no DB schema changes
- Fast for < 1000 games (expected in MVP)
- Easy to test and maintain

**Cons**:
- Loads all open games into memory
- Doesn't scale beyond ~10,000 games

**Recommendation for MVP**: ✅ ACCEPT in-memory approach
**V2 Optimization** (if needed):
- PostgreSQL full-text search for location
- Computed indexes on DateTime, SkillLevel
- Redis cache for hot searches

**Load Test Target** (MVP): 1000 concurrent searches on 500 games → Expected < 200ms p95

### 5.4 Repository Layer Decision

**Question**: Add `SearchGamesAsync()` to repository or reuse `GetOpenGamesAsync()`?

**Answer**: **Reuse `GetOpenGamesAsync()`** ✅

**Rationale**:
- Matching logic is in Application layer (correct separation)
- Repository provides raw data (Open games)
- No need for specialized repo method (avoid premature abstraction)

**If Performance Becomes Issue** (V2):
- Add `SearchGamesAsync()` with DB-side filtering
- Use PostgreSQL window functions for scoring
- Keep current implementation as fallback

---

## 6. Tests to Implement

### 6.1 Unit Tests (Handler)

**File**: `vibora-backend/tests/Vibora.Integration.Tests/Games/SearchGamesIntegrationTests.cs`

**Test Scenarios** (12 tests):

1. **Perfect Matches**:
   - `SearchGames_WithAllCriteriaMet_ShouldReturnPerfectMatches`
   - `SearchGames_WithTimeExactlyPlus2Hours_ShouldBePerfectMatch`
   - `SearchGames_WithLevelPlusOrMinus1_ShouldBePerfectMatch`
   - `SearchGames_WithCaseInsensitiveLocation_ShouldBePerfectMatch`

2. **Partial Matches**:
   - `SearchGames_WithOnlyLocationMatch_ShouldReturnPartialMatch`
   - `SearchGames_WithOnlyTimeMatch_ShouldReturnPartialMatch`
   - `SearchGames_WithOnlyLevelMatch_ShouldReturnPartialMatch`
   - `SearchGames_WithTwoCriteria_ShouldReturnPartialMatchWithScore2`

3. **Edge Cases**:
   - `SearchGames_WithNoCriteriaMet_ShouldReturnEmptyResults`
   - `SearchGames_WithMoreThan3PerfectMatches_ShouldLimitTo3`
   - `SearchGames_WithNoLevelProvided_ShouldMatchAnyLevel`

4. **Ordering**:
   - `SearchGames_PerfectMatches_ShouldOrderByClosestTime`
   - `SearchGames_PartialMatches_ShouldOrderByScoreThenTime`

5. **Validation**:
   - `SearchGames_WithPastDate_ShouldReturnBadRequest`
   - `SearchGames_WithInvalidLevel_ShouldReturnBadRequest`
   - `SearchGames_WithEmptyLocation_ShouldReturnBadRequest`

6. **Status Filter**:
   - `SearchGames_ShouldOnlyReturnOpenGames`
   - `SearchGames_ShouldNotReturnCanceledGames`

### 6.2 Test Example

```csharp
[Fact]
public async Task SearchGames_WithAllCriteriaMet_ShouldReturnPerfectMatches()
{
    // Arrange - Seed games
    var host = await Seeder.SeedUserAsync(u => u.WithExternalId("auth0|host").Intermediate());

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

    var result = await response.ReadAsAsync<SearchGamesQueryResponse>();
    result.PerfectMatches.Should().HaveCount(1);
    result.PerfectMatches[0].Location.Should().Be("Club Paris");
    result.PerfectMatches[0].MatchScore.Should().Be(3);

    result.PartialMatches.Should().HaveCount(1);
    result.PartialMatches[0].Location.Should().Be("Club Lyon");
    result.PartialMatches[0].MatchScore.Should().BeLessThan(3);
}
```

---

## 7. Authorization Decision

### 7.1 Requirement Analysis

**PO Context** (US29):
- "Zero-friction onboarding"
- "Guest mode allows joining without signup"
- "Play feature is entry point for new users"

**Frontend Flow**:
1. Guest opens app (no login)
2. Searches for games near them
3. If finds match → Joins as guest
4. After game → Soft conversion prompt

**Conclusion**: Search MUST be **guest-friendly** (no authentication required).

### 7.2 Recommendation

```csharp
gamesGroup.MapGet("/search", SearchGames)
    .AllowAnonymous(); // ✅ Required for MVP UX
```

**Risks**:
- API abuse (rate limiting needed in V2)
- No personalization (acceptable for MVP)

**Mitigations** (V2):
- Add rate limiting (10 req/min per IP)
- Add cache headers (5 min TTL)
- Add analytics to track guest searches

---

## 8. Data Model Considerations

### 8.1 SkillLevel Field Type

**Current Domain Model** (Game.cs):
```csharp
public string SkillLevel { get; private set; } = string.Empty;
```

**PO Requirement**: Numeric level (1-10)

**Analysis**:
- Domain stores as `string` (flexible: "Intermediate", "5", "Advanced")
- Search expects `int?` parameter
- Handler parses string to int for comparison

**Risk**: If DB contains "Intermediate", `int.TryParse()` fails → skill matching ignored

**Recommendation for MVP**: ✅ ACCEPT current approach
**Reason**:
- No DB migration needed
- Frontend already sends numeric levels
- Handler gracefully handles non-numeric (treats as "no match")

**V2 Enhancement**:
- Migrate SkillLevel to `int?` type
- Add lookup table for text labels ("Intermediate" → 5)

### 8.2 Location Matching

**PO Requirement**: "Exact string match (location input text) - Case insensitive"

**Current Implementation**: `Location.Contains(location)` (substring)

**Proposed Implementation**: `Location.Equals(location, StringComparison.OrdinalIgnoreCase)` (exact)

**Issue**: Frontend may send partial location ("Club Paris" vs "Club Paris - Court 1")

**Recommendation**:
- **For MVP**: Use exact match as per PO spec
- **If friction occurs**: Change to `StartsWith` in V2
- **Future**: Add location normalization/geocoding

---

## 9. Implementation Roadmap

### Phase 1: Core Implementation (3 hours)

1. **Create Query/Handler** (1h)
   - File: `SearchGamesQuery.cs`
   - File: `SearchGamesQueryHandler.cs`
   - Implement matching logic

2. **Register Endpoint** (30 min)
   - Modify: `GameEndpoints.cs`
   - Add `SearchGames` method
   - Add `.AllowAnonymous()`

3. **Manual Testing** (30 min)
   - Start Aspire: `dotnet run --project src/Vibora.AppHost`
   - Test via Swagger: http://localhost:5000/swagger
   - Verify perfect/partial match logic

### Phase 2: Automated Tests (2 hours)

4. **Create Integration Tests** (1.5h)
   - File: `SearchGamesIntegrationTests.cs`
   - Implement 12 test scenarios (see Section 6.1)

5. **Run Test Suite** (30 min)
   - Command: `dotnet test --filter SearchGamesIntegrationTests`
   - Fix any failures
   - Verify all 12 tests pass

### Phase 3: Documentation (1 hour)

6. **Update API Docs** (30 min)
   - Add Swagger annotations
   - Update `docs/Vibora API (MVP) – REST Endpoint Reference.md`

7. **Frontend Integration Guide** (30 min)
   - Document response format
   - Provide TypeScript types
   - Example API calls

**Total Estimated Time**: 6 hours

---

## 10. Acceptance Checklist

Before marking US29 as complete, verify:

### Backend (AC2 - Perfect Matches)
- [ ] DateTime tolerance: ±2 hours (verified with test)
- [ ] Location match: Case-insensitive exact (verified with test)
- [ ] Level match: ±1 level (verified with test)
- [ ] Status filter: Only `Open` games (verified with test)
- [ ] Ordering: Closest DateTime first (verified with test)
- [ ] Limit: Maximum 3 results (verified with test)

### Backend (AC3 - Partial Matches)
- [ ] Match at least 1 criterion (verified with test)
- [ ] Location OR time ±4h OR level ±2 (verified with test)
- [ ] Status: Only `Open` games (verified with test)
- [ ] Ordering: Match score DESC, then DateTime ASC (verified with test)
- [ ] Limit: Maximum 3 results (verified with test)

### Non-Functional Requirements
- [ ] Endpoint is guest-friendly (`.AllowAnonymous()`)
- [ ] Response time < 200ms for 100 games (load test)
- [ ] All 12 integration tests pass
- [ ] Swagger documentation updated
- [ ] Frontend integration tested E2E

---

## 11. Risks & Mitigations

### Risk 1: Performance Degradation (MEDIUM)
**Trigger**: > 5000 open games
**Impact**: Search takes > 1 second
**Mitigation**:
- V1: In-memory filtering (acceptable for MVP)
- V2: Move matching to DB query with indexes
- Monitoring: Add APM tracking for `/games/search`

### Risk 2: Location Matching Friction (LOW)
**Trigger**: Users type "Club Paris" but DB has "Club Paris - Court 1"
**Impact**: No matches found (poor UX)
**Mitigation**:
- V1: Exact match as per PO spec (test with real data)
- V2: Switch to `StartsWith` if friction occurs
- V3: Add location autocomplete/geocoding

### Risk 3: API Abuse (LOW - MVP)
**Trigger**: Bots spam `/games/search` (no auth)
**Impact**: Server overload
**Mitigation**:
- V1: Monitor logs for anomalies
- V2: Add rate limiting (Aspire.RateLimiting)
- V3: Add CAPTCHA for suspicious patterns

---

## 12. Dependencies

### Backend Module Dependencies
- [x] `Vibora.Shared` - `Result<T>`, `IRequest<T>`
- [x] MediatR - CQRS infrastructure
- [x] EF Core - `GamesDbContext`
- [x] Ardalis.Result - Result pattern
- [x] xUnit + FluentAssertions - Testing

**No cross-module dependencies** ✅ (feature is isolated to Games module)

### Frontend Dependencies (Not in Scope)
- Next.js API client update (`viboraApi.games.searchGames()`)
- TypeScript types (`SearchGamesRequest`, `SearchGamesResponse`)
- UI component for search results

---

## 13. Final Recommendations

### For Backend Developer

1. **START HERE**: Implement `SearchGamesQueryHandler.cs` (core logic)
2. **THEN**: Add endpoint in `GameEndpoints.cs`
3. **TEST**: Create `SearchGamesIntegrationTests.cs` with 12 scenarios
4. **VALIDATE**: Run full test suite (`dotnet test`)
5. **DOCUMENT**: Update Swagger + API reference doc

### For Product Owner

1. **REVIEW**: Test search endpoint via Swagger (http://localhost:5000/swagger)
2. **VALIDATE**: Verify perfect/partial match behavior with real data
3. **FEEDBACK**: Location matching may need adjustment (exact vs partial)
4. **APPROVE**: If AC2 + AC3 pass, mark US29 backend as DONE

### For Frontend Developer

1. **WAIT**: Backend implementation (estimated 6h)
2. **INTEGRATE**: Update `viboraApi.games.searchGames()` client
3. **TEST**: E2E test with guest user flow
4. **OPTIMIZE**: Add debouncing on search input (reduce API calls)

---

## 14. Conclusion

**Summary**: The existing `GET /games` endpoint does NOT meet PO requirements for smart game search. A new `GET /games/search` endpoint with tolerance-based matching is needed.

**Architecture**: New endpoint follows Clean Architecture + CQRS + Domain-Driven Design principles. All matching logic is in Application layer (correct separation).

**Effort**: 6 hours (3h implementation + 2h tests + 1h docs)

**Risk**: LOW - No breaking changes, no database migrations, isolated feature

**Approval**: Ready for backend developer implementation ✅

---

**Generated by**: Backend Architect Agent
**Date**: 2025-10-30
**Version**: 1.0
**Status**: Ready for Implementation
