using Vibora.Games.Domain;

namespace Vibora.Integration.Tests.Infrastructure.TestDataBuilders;

/// <summary>
/// Fluent builder for creating Game test data
/// Provides sensible defaults and eliminates duplication
/// </summary>
public class GameBuilder
{
    private Guid? _id;
    private string _hostExternalId = "auth0|test-host";
    private string _hostName = "Test Host";
    private string _hostSkillLevel = "Intermediate";
    private DateTime _dateTime = DateTime.UtcNow.AddDays(1);
    private string _location = "Test Club";
    private string _skillLevel = "Intermediate";
    private int _maxPlayers = 4;
    private string? _notes;
    private readonly List<(string externalId, string name, string skillLevel)> _participants = new();
    private readonly List<(string name, string? phone, string? email)> _guestParticipants = new();
    private bool _isCanceled;
    private GameStatus? _overrideStatus;

    /// <summary>
    /// Set a specific ID (uses reflection to bypass domain encapsulation)
    /// </summary>
    public GameBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    /// <summary>
    /// Configure the host
    /// </summary>
    public GameBuilder WithHost(string externalId, string name = "Host", string skillLevel = "Intermediate")
    {
        _hostExternalId = externalId;
        _hostName = name;
        _hostSkillLevel = skillLevel;
        return this;
    }

    /// <summary>
    /// Set game date (absolute)
    /// </summary>
    public GameBuilder At(DateTime dateTime)
    {
        _dateTime = dateTime;
        return this;
    }

    /// <summary>
    /// Set game date (relative to now)
    /// </summary>
    public GameBuilder InFuture(int days = 1, int hours = 0)
    {
        _dateTime = DateTime.UtcNow.AddDays(days).AddHours(hours);
        return this;
    }

    /// <summary>
    /// Set game in the past (for negative tests)
    /// </summary>
    public GameBuilder InPast(int days = 1)
    {
        _dateTime = DateTime.UtcNow.AddDays(-days);
        return this;
    }

    /// <summary>
    /// Set location
    /// </summary>
    public GameBuilder AtLocation(string location)
    {
        _location = location;
        return this;
    }

    /// <summary>
    /// Set skill level
    /// </summary>
    public GameBuilder WithSkillLevel(string skillLevel)
    {
        _skillLevel = skillLevel;
        return this;
    }

    /// <summary>
    /// Set max players
    /// </summary>
    public GameBuilder WithMaxPlayers(int maxPlayers)
    {
        _maxPlayers = maxPlayers;
        return this;
    }

    /// <summary>
    /// Set notes
    /// </summary>
    public GameBuilder WithNotes(string notes)
    {
        _notes = notes;
        return this;
    }

    /// <summary>
    /// Add a participant (non-host)
    /// </summary>
    public GameBuilder WithParticipant(string externalId, string name = "Player", string skillLevel = "Intermediate")
    {
        _participants.Add((externalId, name, skillLevel));
        return this;
    }

    /// <summary>
    /// Add multiple participants
    /// </summary>
    public GameBuilder WithParticipants(int count)
    {
        for (int i = 0; i < count; i++)
        {
            _participants.Add(($"auth0|player-{i + 1}", $"Player {i + 1}", "Intermediate"));
        }
        return this;
    }

    /// <summary>
    /// Add a guest participant
    /// </summary>
    public GameBuilder WithGuest(string name, string? phone = null, string? email = null)
    {
        _guestParticipants.Add((name, phone, email));
        return this;
    }

    /// <summary>
    /// Make the game full (MaxPlayers participants)
    /// </summary>
    public GameBuilder Full()
    {
        var needed = _maxPlayers - 1; // -1 for host
        for (int i = _participants.Count; i < needed; i++)
        {
            _participants.Add(($"auth0|player-{i + 1}", $"Player {i + 1}", "Intermediate"));
        }
        return this;
    }

    /// <summary>
    /// Mark game as canceled
    /// </summary>
    public GameBuilder Canceled()
    {
        _isCanceled = true;
        return this;
    }

    /// <summary>
    /// Override game status (advanced scenarios)
    /// </summary>
    public GameBuilder WithStatus(GameStatus status)
    {
        _overrideStatus = status;
        return this;
    }

    /// <summary>
    /// Build the Game entity
    /// </summary>
    public Game Build()
    {
        var result = Game.Create(
            _hostExternalId,
            _hostName,
            _hostSkillLevel,
            _dateTime,
            _location,
            _skillLevel,
            _maxPlayers
        );

        if (!result.IsSuccess)
        {
            throw new InvalidOperationException($"Failed to create game: {string.Join(", ", result.Errors)}");
        }

        var game = result.Value;

        // Set ID via reflection if specified
        if (_id.HasValue)
        {
            typeof(Game).GetProperty("Id")!.SetValue(game, _id.Value);
        }

        // Add participants
        foreach (var (externalId, name, skillLevel) in _participants)
        {
            var addResult = game.AddParticipant(externalId, name, skillLevel);
            if (!addResult.IsSuccess)
            {
                throw new InvalidOperationException($"Failed to add participant: {string.Join(", ", addResult.Errors)}");
            }
        }

        // Add guest participants
        foreach (var (name, phone, email) in _guestParticipants)
        {
            var addResult = game.AddGuestParticipant(name, phone, email);
            if (!addResult.IsSuccess)
            {
                throw new InvalidOperationException($"Failed to add guest: {string.Join(", ", addResult.Errors)}");
            }
        }

        // Cancel if requested
        if (_isCanceled)
        {
            var cancelResult = game.Cancel();
            if (!cancelResult.IsSuccess)
            {
                throw new InvalidOperationException($"Failed to cancel game: {string.Join(", ", cancelResult.Errors)}");
            }
        }

        // Override status if specified (via reflection for edge cases)
        if (_overrideStatus.HasValue)
        {
            typeof(Game).GetProperty("Status")!.SetValue(game, _overrideStatus.Value);
        }

        return game;
    }
}
