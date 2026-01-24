using MediatR;
using Microsoft.EntityFrameworkCore;
using Ardalis.Result;
using Vibora.Games.Domain;
using Vibora.Games.Infrastructure.Data;
using NetTopologySuite.Geometries;

namespace Vibora.Games.Application.Queries.SearchGames;

internal class SearchGamesQueryHandler : IRequestHandler<SearchGamesQuery, Result<SearchGamesQueryResponse>>
{
    private readonly GamesDbContext _context;
    private const int MaxCandidates = 50; // Cap for scalability

    public SearchGamesQueryHandler(GamesDbContext context)
    {
        _context = context;
    }

    public async Task<Result<SearchGamesQueryResponse>> Handle(SearchGamesQuery request, CancellationToken cancellationToken)
    {
        // Validate query parameters
        var validationResult = ValidateQuery(request);
        if (!validationResult.IsSuccess)
        {
            return Result<SearchGamesQueryResponse>.Invalid(validationResult.ValidationErrors);
        }

        var when = validationResult.Value;

        // Step 1: SQL-side filtering with PostGIS (or fallback to text search)
        List<CandidateGame> candidates;

        if (request.Latitude.HasValue && request.Longitude.HasValue)
        {
            candidates = await GetGpsBasedCandidates(request, when, cancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(request.Where))
        {
            candidates = await GetTextBasedCandidates(request, when, cancellationToken);
        }
        else
        {
            // No location criteria: return empty (or all open games sorted by time)
            return Result.Success(new SearchGamesQueryResponse([], []));
        }

        if (candidates.Count == 0)
        {
            return Result.Success(new SearchGamesQueryResponse([], []));
        }

        // Step 2: Load Participations only for selected IDs (2nd query)
        var gameIds = candidates.Select(c => c.Id).ToList();
        var gamesWithParticipations = await _context.Games
            .Where(g => gameIds.Contains(g.Id))
            .Include(g => g.Participations)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        // Create lookup for fast access
        var gameLookup = gamesWithParticipations.ToDictionary(g => g.Id);

        // Step 3: Score in-memory (small set of 50 max)
        var scoredGames = candidates
            .Where(c => gameLookup.ContainsKey(c.Id))
            .Select(c => new
            {
                Game = gameLookup[c.Id],
                Distance = c.DistanceKm,
                Score = CalculateMatchScore(gameLookup[c.Id], request, when, c.DistanceKm),
                IsPerfect = IsPerfectMatch(gameLookup[c.Id], request, when, c.DistanceKm)
            })
            .Where(x => x.Score > 0)
            .ToList();

        // Step 4: Separate perfect vs partial
        var perfectMatches = scoredGames
            .Where(x => x.IsPerfect)
            .OrderBy(x => x.Distance ?? double.MaxValue)
            .ThenBy(x => Math.Abs((x.Game.DateTime - when).TotalHours))
            .Take(3)
            .Select(x => MapToDto(x.Game, 3, x.Distance))
            .ToList();

        var partialMatches = scoredGames
            .Where(x => !x.IsPerfect)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Distance ?? double.MaxValue)
            .ThenBy(x => Math.Abs((x.Game.DateTime - when).TotalHours))
            .Take(3)
            .Select(x => MapToDto(x.Game, x.Score, x.Distance))
            .ToList();

        return Result.Success(new SearchGamesQueryResponse(perfectMatches, partialMatches));
    }

    /// <summary>
    /// Validates query parameters using Result pattern
    /// </summary>
    private static Result<DateTime> ValidateQuery(SearchGamesQuery request)
    {
        var errors = new List<ValidationError>();

        // Parse and validate 'when' parameter
        if (!DateTime.TryParse(request.When, null, System.Globalization.DateTimeStyles.AdjustToUniversal, out var dateTime))
        {
            errors.Add(new ValidationError("Invalid 'when' parameter. Expected ISO 8601 format."));
        }
        else
        {
            // Ensure UTC (safety check)
            dateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
        }

        // Validate level
        if (request.Level.HasValue && (request.Level < 1 || request.Level > 10))
        {
            errors.Add(new ValidationError("'level' must be between 1 and 10."));
        }

        // Validate latitude/longitude pair
        if ((request.Latitude.HasValue && !request.Longitude.HasValue) ||
            (!request.Latitude.HasValue && request.Longitude.HasValue))
        {
            errors.Add(new ValidationError("Both latitude and longitude must be provided together."));
        }

        // Validate latitude range
        if (request.Latitude.HasValue && (request.Latitude < -90 || request.Latitude > 90))
        {
            errors.Add(new ValidationError("Latitude must be between -90 and 90."));
        }

        // Validate longitude range
        if (request.Longitude.HasValue && (request.Longitude < -180 || request.Longitude > 180))
        {
            errors.Add(new ValidationError("Longitude must be between -180 and 180."));
        }

        // Validate radius
        if (request.RadiusKm < 1 || request.RadiusKm > 100)
        {
            errors.Add(new ValidationError("Radius must be between 1 and 100 km."));
        }

        return errors.Any()
            ? Result<DateTime>.Invalid(errors)
            : Result<DateTime>.Success(dateTime);
    }

    /// <summary>
    /// GPS-based spatial search (ready for PostGIS migration)
    /// TODO: Once LocationGeog column exists, switch to ST_DWithin for optimal performance
    /// </summary>
    private async Task<List<CandidateGame>> GetGpsBasedCandidates(
        SearchGamesQuery request,
        DateTime when,
        CancellationToken cancellationToken)
    {
        var timeWindow = TimeSpan.FromHours(6); // ±6h timebox

        // Step 1: Get candidates with GPS coordinates and time filter (SQL-side)
        var candidatesWithGps = await _context.Games
            .Where(g => g.Status == GameStatus.Open)
            .Where(g => g.Latitude != null && g.Longitude != null)
            .Where(g => g.DateTime >= when - timeWindow && g.DateTime <= when + timeWindow)
            .OrderBy(g => g.DateTime) // Simple ordering for now, will be distance-based with PostGIS
            .Take(MaxCandidates)
            .Select(g => new CandidateGame
            {
                Id = g.Id,
                DateTime = g.DateTime,
                Location = g.Location,
                SkillLevel = g.SkillLevel,
                CurrentPlayers = g.CurrentPlayers,
                MaxPlayers = g.MaxPlayers,
                Status = g.Status,
                HostExternalId = g.HostExternalId,
                Latitude = g.Latitude,
                Longitude = g.Longitude,
                DistanceKm = null // Will calculate after load
            })
            .ToListAsync(cancellationToken);

        // Step 2: Calculate distance in-memory (temporary until PostGIS migration)
        var userLat = request.Latitude!.Value;
        var userLng = request.Longitude!.Value;

        foreach (var candidate in candidatesWithGps)
        {
            candidate.DistanceKm = CalculateHaversineDistance(
                userLat, userLng,
                candidate.Latitude!.Value, candidate.Longitude!.Value
            );
        }

        // Step 3: Filter by radius and sort by distance
        return candidatesWithGps
            .Where(c => c.DistanceKm!.Value <= request.RadiusKm)
            .OrderBy(c => c.DistanceKm)
            .ToList();
    }

    /// <summary>
    /// Haversine distance calculation (temporary - will be replaced by PostGIS ST_Distance)
    /// </summary>
    private static double CalculateHaversineDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double EarthRadiusKm = 6371.0;
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return EarthRadiusKm * c;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;

    /// <summary>
    /// Text-based fallback search (for games without GPS or users without location)
    /// </summary>
    private async Task<List<CandidateGame>> GetTextBasedCandidates(
        SearchGamesQuery request,
        DateTime when,
        CancellationToken cancellationToken)
    {
        var timeWindow = TimeSpan.FromHours(6);

        return await _context.Games
            .Where(g => g.Status == GameStatus.Open)
            .Where(g => g.DateTime >= when - timeWindow && g.DateTime <= when + timeWindow)
            // Don't filter by location here - scoring will handle location relevance for partial matches
            .OrderBy(g => g.DateTime)
            .Take(MaxCandidates)
            .Select(g => new CandidateGame
            {
                Id = g.Id,
                DateTime = g.DateTime,
                Location = g.Location,
                SkillLevel = g.SkillLevel,
                CurrentPlayers = g.CurrentPlayers,
                MaxPlayers = g.MaxPlayers,
                Status = g.Status,
                HostExternalId = g.HostExternalId,
                DistanceKm = null // No GPS
            })
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Perfect match criteria (score 4):
    /// - Time: ±2 hours
    /// - Location: < 2km (GPS) OR exact match (text)
    /// - Level: ±1 level
    /// </summary>
    private static bool IsPerfectMatch(Game game, SearchGamesQuery criteria, DateTime when, double? distance)
    {
        var timeMatch = Math.Abs((game.DateTime - when).TotalHours) <= 2;

        bool locationMatch;
        if (distance.HasValue)
        {
            locationMatch = distance.Value <= 2.0; // Within 2km
        }
        else if (!string.IsNullOrWhiteSpace(criteria.Where))
        {
            locationMatch = game.Location.Equals(criteria.Where, StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            locationMatch = false;
        }

        var gameLevel = int.TryParse(game.SkillLevel, out var gl) ? gl : 5;
        var criteriaLevel = criteria.Level ?? 5;
        var levelMatch = Math.Abs(gameLevel - criteriaLevel) <= 1;

        return timeMatch && locationMatch && levelMatch;
    }

    /// <summary>
    /// Scoring algorithm (4-point scale):
    /// +1 Time: ±4 hours
    /// +1 Location: within radius (GPS) OR text match
    /// +1 Level: ±2 levels
    /// +1 GPS bonus: game has GPS coordinates
    /// </summary>
    private static int CalculateMatchScore(Game game, SearchGamesQuery criteria, DateTime when, double? distance)
    {
        int score = 0;
        int criteriaMatched = 0;

        // Time criterion: ±6 hours
        if (Math.Abs((game.DateTime - when).TotalHours) <= 6)
            criteriaMatched++;

        // Location criterion
        if (distance.HasValue)
        {
            if (distance.Value <= criteria.RadiusKm)
                criteriaMatched++;
        }
        else if (!string.IsNullOrWhiteSpace(criteria.Where))
        {
            if (game.Location.Contains(criteria.Where, StringComparison.OrdinalIgnoreCase))
                criteriaMatched++;
        }

        // Level criterion: ±2 levels
        if (criteria.Level.HasValue)
        {
            var gameLevel = int.TryParse(game.SkillLevel, out var gl) ? gl : 5;
            if (Math.Abs(gameLevel - criteria.Level.Value) <= 2)
                criteriaMatched++;
        }
        else
        {
            criteriaMatched++; // No level criteria = match
        }

        // GPS bonus
        if (game.Latitude.HasValue && game.Longitude.HasValue)
            score++;

        // Score is based on number of criteria matched (1-3 for partial matches)
        // Perfect matches are handled separately with fixed score of 3
        score += criteriaMatched;

        // For partial matches (not perfect), cap score at 2 max
        // Perfect matches will get score 3 by MapToDto, so we don't need to cap those
        if (!IsPerfectMatch(game, criteria, when, distance) && score > 2)
            score = 2;

        return score;
    }

    private static GameMatchDto MapToDto(Game game, int score, double? distance)
    {
        int? skillLevel = int.TryParse(game.SkillLevel, out var level) ? level : null;
        var hostParticipation = game.Participations.FirstOrDefault(p => p.UserExternalId == game.HostExternalId);
        var hostDisplayName = hostParticipation?.UserName ?? "Organizer";

        return new GameMatchDto(
            game.Id,
            game.DateTime,
            game.Location,
            skillLevel,
            game.CurrentPlayers,
            game.MaxPlayers,
            game.Status.ToString(),
            score,
            hostDisplayName,
            distance.HasValue ? Math.Round(distance.Value, 1) : null
        );
    }

    /// <summary>
    /// Internal DTO for candidate games (without Participations)
    /// </summary>
    private class CandidateGame
    {
        public Guid Id { get; init; }
        public DateTime DateTime { get; init; }
        public string Location { get; init; } = string.Empty;
        public string SkillLevel { get; init; } = string.Empty;
        public int CurrentPlayers { get; init; }
        public int MaxPlayers { get; init; }
        public GameStatus Status { get; init; }
        public string HostExternalId { get; init; } = string.Empty;
        public double? Latitude { get; init; }
        public double? Longitude { get; init; }
        public double? DistanceKm { get; set; } // Set by in-memory calculation
    }
}
