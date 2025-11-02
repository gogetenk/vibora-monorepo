using Ardalis.Result;
using MediatR;
using Microsoft.Extensions.Logging;
using Vibora.Games.Application;
using Vibora.Games.Contracts.Commands;
using Vibora.Games.Domain;
using Vibora.Shared.Application;

namespace Vibora.Games.Application.Commands.ClaimGuestParticipations;

/// <summary>
/// Internal MediatR request that implements IRequest<> for the handler
/// Maps from public DTO to internal request type
/// </summary>
internal sealed record ClaimGuestParticipationsRequest(
    string UserExternalId,
    string? PhoneNumber,
    string? Email
) : IRequest<Result<ClaimGuestParticipationsResultDto>>;

/// <summary>
/// Handler for claiming guest participations during user signup (Phase 3B)
/// Converts GuestParticipants to regular Participations when user creates account
/// </summary>
internal sealed class ClaimGuestParticipationsCommandHandler
    : IRequestHandler<ClaimGuestParticipationsRequest, Result<ClaimGuestParticipationsResultDto>>
{
    private readonly IGameRepository _gameRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ClaimGuestParticipationsCommandHandler> _logger;

    public ClaimGuestParticipationsCommandHandler(
        IGameRepository gameRepository,
        IUnitOfWork unitOfWork,
        ILogger<ClaimGuestParticipationsCommandHandler> logger)
    {
        _gameRepository = gameRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<ClaimGuestParticipationsResultDto>> Handle(
        ClaimGuestParticipationsRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Find all guest participations matching the contact info
            var guestParticipants = await _gameRepository.GetGuestParticipantsByContactAsync(
                request.PhoneNumber,
                request.Email,
                cancellationToken);

            if (guestParticipants.Count == 0)
            {
                _logger.LogDebug(
                    "No guest participations found for user {UserExternalId} with phone={Phone}, email={Email}",
                    request.UserExternalId, request.PhoneNumber, request.Email);

                return Result.Success(new ClaimGuestParticipationsResultDto(0));
            }

            // For each guest participation, find the game and convert to regular participation
            int claimedCount = 0;
            var gameIds = guestParticipants.Select(gp => gp.GameId).Distinct();

            foreach (var gameId in gameIds)
            {
                // Get the game with all participations
                var gameResult = await _gameRepository.GetByIdWithParticipationsAsync(gameId, cancellationToken);
                if (!gameResult.IsSuccess)
                {
                    _logger.LogWarning(
                        "Game {GameId} not found when claiming participations for user {UserExternalId}",
                        gameId, request.UserExternalId);
                    continue;
                }

                var game = gameResult.Value;
                var guestsInThisGame = guestParticipants.Where(gp => gp.GameId == gameId).ToList();

                foreach (var guestParticipant in guestsInThisGame)
                {
                    // Create regular participation from guest
                    var participation = Participation.Create(
                        game.Id,
                        request.UserExternalId,
                        guestParticipant.Name,
                        "Intermediate", // Default skill level from guest join
                        isHost: false);

                    // Check if user already joined this game
                    if (game.IsUserJoined(request.UserExternalId))
                    {
                        _logger.LogWarning(
                            "User {UserExternalId} already participated in game {GameId}, skipping conversion",
                            request.UserExternalId, gameId);
                        continue;
                    }

                    // Add the participation to the game
                    var addResult = game.AddParticipant(
                        request.UserExternalId,
                        guestParticipant.Name,
                        "Intermediate",
                        isHost: false);

                    if (addResult.IsSuccess)
                    {
                        // Remove the guest participant
                        _gameRepository.RemoveGuestParticipant(guestParticipant);
                        _gameRepository.Update(game);
                        claimedCount++;

                        _logger.LogInformation(
                            "Guest participation claimed: User {UserExternalId} -> Game {GameId}, claimed_count={ClaimedCount}",
                            request.UserExternalId, gameId, claimedCount);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Failed to add participation for user {UserExternalId} to game {GameId}: {Error}",
                            request.UserExternalId, gameId, string.Join(", ", addResult.Errors));
                    }
                }
            }

            // Save all changes in a single transaction
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Auto-claimed {ClaimedCount} guest participations for user {UserExternalId}",
                claimedCount, request.UserExternalId);

            return Result.Success(new ClaimGuestParticipationsResultDto(claimedCount));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error claiming guest participations for user {UserExternalId}",
                request.UserExternalId);

            return Result.Error($"Failed to claim guest participations: {ex.Message}");
        }
    }
}
