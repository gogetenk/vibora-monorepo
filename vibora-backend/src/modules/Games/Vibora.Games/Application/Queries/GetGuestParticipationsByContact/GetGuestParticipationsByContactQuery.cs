using Ardalis.Result;
using MediatR;

namespace Vibora.Games.Application.Queries.GetGuestParticipationsByContact;

/// <summary>
/// Query to retrieve all guest participations matching a contact (phone or email)
/// Used for converting guest participations to regular user participations
/// </summary>
internal sealed record GetGuestParticipationsByContactQuery(
    string? PhoneNumber,
    string? Email
) : IRequest<Result<GetGuestParticipationsByContactResult>>;

internal sealed record GetGuestParticipationsByContactResult(
    List<GuestParticipationDto> GuestParticipations
);

internal sealed record GuestParticipationDto(
    Guid GuestParticipantId,
    Guid GameId,
    string Name,
    string? PhoneNumber,
    string? Email,
    DateTime JoinedAt
);
