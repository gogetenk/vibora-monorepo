using Ardalis.Result;
using MediatR;
using Vibora.Games.Contracts.Services;
using Vibora.Users.Domain;

namespace Vibora.Users.Application.Commands.ClaimGuestParticipations;

internal sealed class ClaimGuestParticipationsCommandHandler
    : IRequestHandler<ClaimGuestParticipationsCommand, Result<ClaimGuestParticipationsResult>>
{
    private readonly IUserRepository _userRepository;
    private readonly IGamesServiceClient _gamesServiceClient;

    public ClaimGuestParticipationsCommandHandler(
        IUserRepository userRepository,
        IGamesServiceClient gamesServiceClient)
    {
        _userRepository = userRepository;
        _gamesServiceClient = gamesServiceClient;
    }

    public async Task<Result<ClaimGuestParticipationsResult>> Handle(
        ClaimGuestParticipationsCommand request,
        CancellationToken cancellationToken)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(request.ExternalId))
        {
            return Result<ClaimGuestParticipationsResult>.Invalid(
                new ValidationError("ExternalId is required"));
        }

        // At least one contact method must be provided
        if (string.IsNullOrWhiteSpace(request.PhoneNumber) && string.IsNullOrWhiteSpace(request.Email))
        {
            return Result<ClaimGuestParticipationsResult>.Invalid(
                new ValidationError("Either phone number or email is required to claim guest participations"));
        }

        // Get user from repository
        var user = await _userRepository.GetNonGuestByExternalIdAsync(request.ExternalId, cancellationToken);
        if (user == null)
        {
            return Result<ClaimGuestParticipationsResult>.NotFound(
                "User not found. Please ensure the user account is created before claiming guest participations.");
        }

        // Query Games module for guest participations matching contact info
        var guestParticipations = await _gamesServiceClient.GetGuestParticipationsByContactAsync(
            request.PhoneNumber,
            request.Email,
            cancellationToken);

        if (!guestParticipations.Any())
        {
            // No guest participations found - return success with 0 count
            return Result.Success(new ClaimGuestParticipationsResult(
                ClaimedParticipations: 0,
                ClaimedGames: new List<ClaimedGameDto>()));
        }

        // Convert guest participations to regular participations
        var guestParticipantIds = guestParticipations.Select(gp => gp.GuestParticipantId).ToList();
        var convertedCount = await _gamesServiceClient.ConvertGuestParticipationsAsync(
            guestParticipantIds,
            user.ExternalId,
            user.Name,
            user.SkillLevel.ToString(),
            cancellationToken);

        // Build list of claimed games (for response)
        var claimedGames = guestParticipations
            .Select(gp => new ClaimedGameDto(
                gp.GameId,
                gp.JoinedAt, // Use JoinedAt as approximation (actual game date not in DTO)
                "Game")) // Location not available in DTO
            .Take(convertedCount) // Only include successfully converted ones
            .ToList();

        return Result.Success(new ClaimGuestParticipationsResult(
            ClaimedParticipations: convertedCount,
            ClaimedGames: claimedGames));
    }
}
