namespace Vibora.Games.Contracts.Commands;

/// <summary>
/// Public DTO for claiming guest participations during user signup (Phase 3B)
/// Allows cross-module communication between Users and Games modules
/// </summary>
public sealed record ClaimGuestParticipationsRequestDto(
    string UserExternalId,
    string? PhoneNumber,
    string? Email
);

public sealed record ClaimGuestParticipationsResultDto(
    int ClaimedParticipations
);
