using Ardalis.Result;
using Vibora.Games.Domain.Events;
using Vibora.Shared.Domain;

namespace Vibora.Games.Domain;

/// <summary>
/// Represents a shareable link for a game
/// Tracks who shared, when, and how many times the link was viewed
/// This is an Aggregate Root because it raises domain events
/// </summary>
public class GameShare : AggregateRoot
{
    public Guid Id { get; private set; }
    public Guid GameId { get; private set; }
    public string SharedByUserExternalId { get; private set; } = string.Empty;
    public string ShareToken { get; private set; } = string.Empty;
    public int ViewCount { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ExpiresAt { get; private set; }

    // Navigation property
    public Game Game { get; private set; } = null!;

    // EF Core constructor
    private GameShare() { }

    public static Result<GameShare> Create(
        Guid gameId,
        string sharedByUserExternalId,
        DateTime? expiresAt = null)
    {
        var errors = new List<ValidationError>();

        if (gameId == Guid.Empty)
        {
            errors.Add(new ValidationError("GameId cannot be empty"));
        }
        
        if (string.IsNullOrWhiteSpace(sharedByUserExternalId))
        {
            errors.Add(new ValidationError("SharedByUserExternalId is required"));
        }

        if (expiresAt.HasValue && expiresAt.Value <= DateTime.UtcNow)
        {
            errors.Add(new ValidationError("ExpiresAt must be in the future"));
        }

        if (errors.Any())
        {
            return Result<GameShare>.Invalid(errors);
        }

        // Generate unique token (8 characters, URL-safe)
        var shareToken = GenerateUniqueToken();

        var gameShare = new GameShare
        {
            Id = Guid.NewGuid(),
            GameId = gameId,
            SharedByUserExternalId = sharedByUserExternalId,
            ShareToken = shareToken,
            ViewCount = 0,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt
        };

        // Raise domain event (will be dispatched after Unit of Work commit)
        gameShare.AddDomainEvent(new GameSharedDomainEvent(
            gameShare.GameId,
            gameShare.Id,
            gameShare.SharedByUserExternalId,
            gameShare.ShareToken
        ));
        
        return Result.Success(gameShare);
    }

    public Result IncrementViewCount()
    {
        if (IsExpired())
            return Result.Error("Cannot increment view count on expired share link");

        ViewCount++;
        return Result.Success();
    }

    public bool IsExpired()
    {
        return ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow;
    }

    private static string GenerateUniqueToken()
    {
        // Generate a short, URL-safe token (8 characters)
        // Using base64 encoding of random bytes, URL-safe variant
        var randomBytes = new byte[6]; // 6 bytes = 8 base64 characters
        System.Security.Cryptography.RandomNumberGenerator.Fill(randomBytes);
        
        // Use URL-safe base64: replace + with - and / with _
        var token = Convert.ToBase64String(randomBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('='); // Remove padding
        
        // Ensure we always return exactly 8 characters
        return token.Length >= 8 ? token.Substring(0, 8) : token.PadRight(8, 'A');
    }
}
