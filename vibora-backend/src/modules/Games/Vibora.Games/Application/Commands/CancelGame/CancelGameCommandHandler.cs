using Ardalis.Result;
using MediatR;
using Vibora.Games.Application;
using Vibora.Games.Domain;

namespace Vibora.Games.Application.Commands.CancelGame;

/// <summary>
/// Handler for canceling a game
/// Uses repository and unit of work (Clean Architecture)
/// </summary>
internal sealed class CancelGameCommandHandler : IRequestHandler<CancelGameCommand, Result<CancelGameResult>>
{
    private readonly IGameRepository _gameRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CancelGameCommandHandler(IGameRepository gameRepository, IUnitOfWork unitOfWork)
    {
        _gameRepository = gameRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CancelGameResult>> Handle(CancelGameCommand request, CancellationToken cancellationToken)
    {
        var validationResult = ValidateRequest(request);
        if (!validationResult.IsSuccess)
            return Result<CancelGameResult>.Invalid(validationResult.ValidationErrors);

        return await GetGameAsync(request.GameId, cancellationToken)
            .BindAsync(game => ValidateHostPermission(game, request.HostExternalId))
            .BindAsync(game => CancelGame(game))
            .BindAsync(async game => await SaveChangesAsync(game, cancellationToken))
            .MapAsync(game => new CancelGameResult(
                game.Id,
                $"Game at {game.Location} has been canceled successfully"
            ));
    }

    private static Result ValidateRequest(CancelGameCommand request)
    {
        var validationErrors = new List<ValidationError>();

        if (request.GameId == Guid.Empty)
        {
            validationErrors.Add(new ValidationError("GameId cannot be empty"));
        }

        if (string.IsNullOrWhiteSpace(request.HostExternalId))
        {
            validationErrors.Add(new ValidationError("HostExternalId is required"));
        }

        return validationErrors.Any()
            ? Result.Invalid(validationErrors)
            : Result.Success();
    }

    private Task<Result<Game>> GetGameAsync(Guid gameId, CancellationToken cancellationToken)
    {
        return _gameRepository.GetByIdAsync(gameId, cancellationToken);
    }

    private static Result<Game> ValidateHostPermission(Game game, string hostExternalId)
    {
        return !game.IsHost(hostExternalId)
            ? Result<Game>.Forbidden()
            : Result<Game>.Success(game);
    }

    private static Result<Game> CancelGame(Game game)
    {
        var cancelResult = game.Cancel();

        return !cancelResult.IsSuccess
            ? Result<Game>.Error(string.Join(", ", cancelResult.Errors))
            : Result<Game>.Success(game);
    }

    private async Task<Result<Game>> SaveChangesAsync(Game game, CancellationToken cancellationToken)
    {
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<Game>.Success(game);
    }
}
