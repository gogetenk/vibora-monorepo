using Ardalis.Result;
using MediatR;
using Vibora.Games.Application;
using Vibora.Games.Domain;
using Vibora.Shared.Application;

namespace Vibora.Games.Application.Commands.LeaveGame;

/// <summary>
/// Handler for leaving a game
/// Uses repository and unit of work (Clean Architecture)
/// </summary>
internal sealed class LeaveGameCommandHandler : IRequestHandler<LeaveGameCommand, Result<LeaveGameResult>>
{
    private readonly IGameRepository _gameRepository;
    private readonly IUnitOfWork _unitOfWork;

    public LeaveGameCommandHandler(IGameRepository gameRepository, IUnitOfWork unitOfWork)
    {
        _gameRepository = gameRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<LeaveGameResult>> Handle(LeaveGameCommand request, CancellationToken cancellationToken)
    {
        var result = await ValidateRequest(request)
            .BindAsync(cmd => _gameRepository.GetByIdWithParticipationsAsync(cmd.GameId, cancellationToken))
            .BindAsync(ValidateGameStatus)
            .BindAsync(game => game.RemoveParticipant(request.UserExternalId).Map(_ => game))
            .TapAsync(async game => 
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return Result.Success();
            });

        return result.Map(game => new LeaveGameResult(
            game.Id,
            $"Successfully left game at {game.Location}"
        ));
    }

    private Result<LeaveGameCommand> ValidateRequest(LeaveGameCommand request)
    {
        var errors = new List<ValidationError>();

        if (request.GameId == Guid.Empty)
            errors.Add(new ValidationError("GameId cannot be empty"));

        if (string.IsNullOrWhiteSpace(request.UserExternalId))
            errors.Add(new ValidationError("UserExternalId is required"));

        return errors.Any()
            ? Result<LeaveGameCommand>.Invalid(errors)
            : Result.Success(request);
    }

    private Result<Game> ValidateGameStatus(Game game)
    {
        if (game.Status == GameStatus.Canceled)
            return Result<Game>.Error("Cannot leave a canceled game");

        if (game.DateTime <= DateTime.UtcNow)
            return Result<Game>.Error("Cannot leave a game that has already started or is in the past");

        return Result.Success(game);
    }
}
