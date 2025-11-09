using Ardalis.Result;
using NetTopologySuite.Geometries;
using Vibora.Games.Domain.Events;
using Vibora.Shared.Domain;

namespace Vibora.Games.Domain;

/// <summary>
/// Game aggregate root - Represents a padel/tennis match session
/// </summary>
public sealed class Game : AggregateRoot
{
    public Guid Id { get; private set; }
    public DateTime DateTime { get; private set; }
    public string Location { get; private set; } = string.Empty;
    public string SkillLevel { get; private set; } = string.Empty;
    public int MaxPlayers { get; private set; }
    public int CurrentPlayers { get; private set; }
    public string HostExternalId { get; private set; } = string.Empty; // User ExternalId from Auth0/Supabase
    public GameStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public double? Latitude { get; private set; }
    public double? Longitude { get; private set; }
    public Point? LocationGeog { get; private set; } // PostGIS geography (auto-populated by trigger)

    private readonly List<Participation> _participations = new();
    public IReadOnlyCollection<Participation> Participations => _participations.AsReadOnly();

    private readonly List<GuestParticipant> _guestParticipants = new();
    public IReadOnlyCollection<GuestParticipant> GuestParticipants => _guestParticipants.AsReadOnly();

    // Business rule: Maximum number of guests per game (configurable)
    private const int MaxGuestsPerGame = 2;

    // EF Core constructor
    private Game() { }

    /// <summary>
    /// Validate game creation parameters
    /// </summary>
    public static Result Validate(
        DateTime dateTime,
        string location,
        string skillLevel,
        int maxPlayers,
        double? latitude = null,
        double? longitude = null)
    {
        var errors = new List<ValidationError>();

        if (dateTime <= System.DateTime.UtcNow)
        {
            errors.Add(new ValidationError("Game date/time must be in the future"));
        }

        if (string.IsNullOrWhiteSpace(location))
        {
            errors.Add(new ValidationError("Location is required"));
        }
        else if (location.Length > 500)
        {
            errors.Add(new ValidationError("Location must not exceed 500 characters"));
        }

        if (string.IsNullOrWhiteSpace(skillLevel))
        {
            errors.Add(new ValidationError("Skill level is required"));
        }
        else if (skillLevel.Length > 50)
        {
            errors.Add(new ValidationError("Skill level must not exceed 50 characters"));
        }

        if (maxPlayers < 2 || maxPlayers > 10)
        {
            errors.Add(new ValidationError("Max players must be between 2 and 10"));
        }

        // GPS validation
        if ((latitude.HasValue && !longitude.HasValue) || (!latitude.HasValue && longitude.HasValue))
        {
            errors.Add(new ValidationError("Both latitude and longitude must be provided together"));
        }

        if (latitude.HasValue && (latitude < -90 || latitude > 90))
        {
            errors.Add(new ValidationError("Latitude must be between -90 and 90"));
        }

        if (longitude.HasValue && (longitude < -180 || longitude > 180))
        {
            errors.Add(new ValidationError("Longitude must be between -180 and 180"));
        }

        return errors.Any()
            ? Result.Invalid(errors)
            : Result.Success();
    }

    /// <summary>
    /// Create a new game - Host is automatically added as first participant
    /// Assumes validation has been done via Validate() method
    /// </summary>
    public static Result<Game> Create(
        string hostExternalId,
        string hostName,
        string hostSkillLevel,
        DateTime dateTime,
        string location,
        string skillLevel,
        int maxPlayers,
        double? latitude = null,
        double? longitude = null)
    {
        // Validate first
        var validationResult = Validate(dateTime, location, skillLevel, maxPlayers, latitude, longitude);
        if (!validationResult.IsSuccess)
        {
            return Result<Game>.Invalid(validationResult.ValidationErrors);
        }

        var game = new Game
        {
            Id = Guid.NewGuid(),
            DateTime = dateTime,
            Location = location,
            SkillLevel = skillLevel,
            MaxPlayers = maxPlayers,
            CurrentPlayers = 0,
            HostExternalId = hostExternalId,
            Status = GameStatus.Open,
            CreatedAt = System.DateTime.UtcNow,
            Latitude = latitude,
            Longitude = longitude
        };

        // Host automatically joins as first participant (with metadata cached)
        var addResult = game.AddParticipant(hostExternalId, hostName, hostSkillLevel, isHost: true);
        if (!addResult.IsSuccess)
        {
            return Result<Game>.Error(string.Join(", ", addResult.Errors));
        }

        // Raise domain event (will be published by Unit of Work after transaction)
        game.AddDomainEvent(new GameCreatedDomainEvent(
            game.Id,
            game.HostExternalId,
            game.DateTime,
            game.Location,
            game.SkillLevel,
            game.MaxPlayers
        ));

        return Result<Game>.Success(game);
    }

    public Result AddParticipant(string userExternalId, string userName, string userSkillLevel, bool isHost = false)
    {
        if (CurrentPlayers >= MaxPlayers)
        {
            return Result.Error("Game is already full");
        }

        if (_participations.Any(p => p.UserExternalId == userExternalId))
        {
            return Result.Error("User already joined this game");
        }

        // Validate skill level compatibility (unless it's the host joining during creation)
        if (!isHost && !string.IsNullOrWhiteSpace(SkillLevel))
        {
            var skillLevelValidation = ValidateSkillLevelCompatibility(userSkillLevel);
            if (!skillLevelValidation.IsSuccess)
            {
                return skillLevelValidation;
            }
        }

        var participation = Participation.Create(Id, userExternalId, userName, userSkillLevel, isHost);
        _participations.Add(participation);
        CurrentPlayers++;

        if (CurrentPlayers >= MaxPlayers)
        {
            Status = GameStatus.Full;
        }

        // Raise domain event only for non-host participants (host joining is covered by GameCreatedDomainEvent)
        if (!isHost)
        {
            AddDomainEvent(new PlayerJoinedDomainEvent(
                Id,
                participation.Id,
                userExternalId,
                userName,
                userSkillLevel,
                HostExternalId,
                DateTime,
                Location,
                CurrentPlayers,
                MaxPlayers
            ));
        }

        return Result.Success();
    }

    public Result RemoveParticipant(string userExternalId)
    {
        var participation = _participations.FirstOrDefault(p => p.UserExternalId == userExternalId);
        if (participation == null)
        {
            return Result.Error("User is not a participant of this game");
        }

        // Removed host restriction: Host can now leave like any other participant
        // The game continues without the host (simplified UX - "Less is More")

        // Store user info before removing
        var userName = participation.UserName;
        
        _participations.Remove(participation);
        CurrentPlayers--;

        if (Status == GameStatus.Full)
        {
            Status = GameStatus.Open;
        }

        // Raise domain event (will be published by Unit of Work after transaction)
        AddDomainEvent(new ParticipationRemovedDomainEvent(
            Id,
            userExternalId,
            userName,
            CurrentPlayers,
            Status
        ));

        return Result.Success();
    }

    public Result<GuestParticipant> AddGuestParticipant(string name, string? phoneNumber, string? email, string? guestExternalId = null)
    {
        if (CurrentPlayers >= MaxPlayers)
        {
            return Result<GuestParticipant>.Error("Game is already full");
        }

        if (_guestParticipants.Count >= MaxGuestsPerGame)
        {
            return Result<GuestParticipant>.Error($"Maximum {MaxGuestsPerGame} guests allowed per game");
        }

        // Check if guest already joined (by phone or email)
        var existingGuest = _guestParticipants.FirstOrDefault(g => g.MatchesContact(phoneNumber, email));
        if (existingGuest != null)
        {
            return Result<GuestParticipant>.Error("This phone number or email has already joined this game");
        }

        var guestResult = GuestParticipant.Create(Id, name, phoneNumber, email, guestExternalId);
        if (!guestResult.IsSuccess)
        {
            return guestResult;
        }

        _guestParticipants.Add(guestResult.Value);
        CurrentPlayers++;

        if (CurrentPlayers >= MaxPlayers)
        {
            Status = GameStatus.Full;
        }

        // Raise domain event (will be published by Unit of Work after transaction)
        AddDomainEvent(new GuestJoinedGameDomainEvent(
            Id,
            guestResult.Value.Id,
            guestResult.Value.Name,
            guestResult.Value.GetContactIdentifier(),
            CurrentPlayers,
            Status
        ));

        return guestResult;
    }

    public Result Cancel()
    {
        if (Status == GameStatus.Canceled)
        {
            return Result.Error("Game is already canceled");
        }

        Status = GameStatus.Canceled;

        // Raise domain event (will be published by Unit of Work after transaction)
        // Pass participant collections so consumers don't need to query Games module
        AddDomainEvent(new GameCanceledDomainEvent(
            Id,
            HostExternalId,
            DateTime,
            Location,
            CurrentPlayers,
            Participations,
            GuestParticipants
        ));

        return Result.Success();
    }

    private Result ValidateSkillLevelCompatibility(string userSkillLevel)
    {
        // Parse game skill level
        if (!int.TryParse(SkillLevel, out int gameLevel))
        {
            // If game skill level is not numeric, allow any player
            return Result.Success();
        }

        // Parse user skill level
        if (!int.TryParse(userSkillLevel, out int playerLevel))
        {
            return Result.Error("Invalid skill level format");
        }

        // Check if player level is within ±1 of game level
        int difference = Math.Abs(gameLevel - playerLevel);
        if (difference > 1)
        {
            return Result.Error($"Your skill level ({playerLevel}) doesn't match this game's level ({gameLevel}). Only players within ±1 level can join.");
        }

        return Result.Success();
    }

    public bool IsHost(string userExternalId) => HostExternalId == userExternalId;
    public bool IsFull() => CurrentPlayers >= MaxPlayers;
    public bool IsUserJoined(string userExternalId) => _participations.Any(p => p.UserExternalId == userExternalId);
    public bool HasGpsCoordinates() => Latitude.HasValue && Longitude.HasValue;
}

public enum GameStatus
{
    Open,
    Full,
    InProgress,
    Completed,
    Canceled
}
