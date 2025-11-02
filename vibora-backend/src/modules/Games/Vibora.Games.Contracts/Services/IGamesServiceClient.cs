namespace Vibora.Games.Contracts.Services;

/// <summary>
/// Client to query Games module for game-related information
/// PUBLIC interface for cross-module communication (HTTP or In-Process)
/// Shared by all modules that need to query Games data
/// </summary>
public interface IGamesServiceClient
{
    /// <summary>
    /// Gets the number of games a user has participated in
    /// Counts unique games where user is a participant (not canceled)
    /// </summary>
    Task<int> GetUserGamesCountAsync(
        string userExternalId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all guest participations matching the provided contact information
    /// Used for converting guest participations to user participations
    /// </summary>
    Task<List<GuestParticipationDto>> GetGuestParticipationsByContactAsync(
        string? phoneNumber,
        string? email,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Converts guest participations to regular user participations
    /// Creates Participation records and removes GuestParticipant records
    /// </summary>
    Task<int> ConvertGuestParticipationsAsync(
        List<Guid> guestParticipantIds,
        string userExternalId,
        string userName,
        string userSkillLevel,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// DTO for guest participation information (PUBLIC)
/// </summary>
public sealed record GuestParticipationDto(
    Guid GuestParticipantId,
    Guid GameId,
    string Name,
    string? PhoneNumber,
    string? Email,
    DateTime JoinedAt
);
