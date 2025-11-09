using Ardalis.Result;
using MediatR;

namespace Vibora.Users.Application.Commands.ClaimGuestParticipations;

/// <summary>
/// Command to claim guest participations when a user creates an account
/// Converts all guest participations matching the contact info to regular user participations
/// </summary>
internal sealed record ClaimGuestParticipationsCommand(
    string ExternalId,
    string? PhoneNumber,
    string? Email
) : IRequest<Result<ClaimGuestParticipationsResult>>;

internal sealed record ClaimGuestParticipationsResult(
    int ClaimedParticipations,
    List<ClaimedGameDto> ClaimedGames
);

internal sealed record ClaimedGameDto(
    Guid GameId,
    DateTime GameDateTime,
    string Location
);
