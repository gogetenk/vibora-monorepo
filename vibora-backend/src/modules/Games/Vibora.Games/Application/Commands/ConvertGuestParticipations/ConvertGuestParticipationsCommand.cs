using Ardalis.Result;
using MediatR;

namespace Vibora.Games.Application.Commands.ConvertGuestParticipations;

/// <summary>
/// Command to convert guest participations to regular user participations
/// Called when a guest user creates an account
/// </summary>
internal sealed record ConvertGuestParticipationsCommand(
    List<Guid> GuestParticipantIds,
    string UserExternalId,
    string UserName,
    string UserSkillLevel
) : IRequest<Result<ConvertGuestParticipationsResult>>;

internal sealed record ConvertGuestParticipationsResult(
    int ConvertedCount
);
