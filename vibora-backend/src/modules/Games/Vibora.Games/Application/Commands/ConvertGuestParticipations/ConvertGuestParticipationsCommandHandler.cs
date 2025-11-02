using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Vibora.Games.Domain;
using Vibora.Games.Infrastructure.Data;

namespace Vibora.Games.Application.Commands.ConvertGuestParticipations;

internal sealed class ConvertGuestParticipationsCommandHandler
    : IRequestHandler<ConvertGuestParticipationsCommand, Result<ConvertGuestParticipationsResult>>
{
    private readonly GamesDbContext _dbContext;
    private readonly IGameRepository _gameRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ConvertGuestParticipationsCommandHandler(
        GamesDbContext dbContext,
        IGameRepository gameRepository,
        IUnitOfWork unitOfWork)
    {
        _dbContext = dbContext;
        _gameRepository = gameRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ConvertGuestParticipationsResult>> Handle(
        ConvertGuestParticipationsCommand request,
        CancellationToken cancellationToken)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(request.UserExternalId))
        {
            return Result<ConvertGuestParticipationsResult>.Invalid(
                new ValidationError("UserExternalId is required"));
        }

        if (request.GuestParticipantIds == null || !request.GuestParticipantIds.Any())
        {
            // No guest participations to convert - return success with 0 count
            return Result.Success(new ConvertGuestParticipationsResult(0));
        }

        // Retrieve all guest participations
        var guestParticipations = await _dbContext.GuestParticipants
            .Where(gp => request.GuestParticipantIds.Contains(gp.Id))
            .Include(gp => gp.Game)
            .ToListAsync(cancellationToken);

        if (!guestParticipations.Any())
        {
            return Result.Success(new ConvertGuestParticipationsResult(0));
        }

        int convertedCount = 0;

        // For each guest participation, create a regular participation
        foreach (var guestParticipation in guestParticipations)
        {
            var game = guestParticipation.Game;

            // Skip if game is canceled or completed
            if (game.Status == GameStatus.Canceled || game.Status == GameStatus.Completed)
            {
                continue;
            }

            // Check if user is already participating in this game
            var existingParticipation = await _dbContext.Participations
                .AnyAsync(p => p.GameId == game.Id && p.UserExternalId == request.UserExternalId, 
                    cancellationToken);

            if (existingParticipation)
            {
                // User already participates in this game, skip
                continue;
            }

            // Create new participation
            var participation = Participation.Create(
                game.Id,
                request.UserExternalId,
                request.UserName,
                request.UserSkillLevel,
                isHost: false);

            _gameRepository.AddParticipation(participation);
            convertedCount++;

            // Note: We don't increment CurrentPlayers because the guest was already counted
            // The guest participation will be removed, so the count remains the same
        }

        // Remove all guest participations
        _dbContext.GuestParticipants.RemoveRange(guestParticipations);

        // Save changes
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new ConvertGuestParticipationsResult(convertedCount));
    }
}
