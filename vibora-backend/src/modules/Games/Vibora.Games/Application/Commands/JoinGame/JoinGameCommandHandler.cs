using Ardalis.Result;
using MediatR;
using Microsoft.AspNetCore.OutputCaching;
using Vibora.Games.Application;
using Vibora.Games.Domain;
using Vibora.Shared.Application;
using Vibora.Shared.Infrastructure.Caching;

namespace Vibora.Games.Application.Commands.JoinGame;

/// <summary>
/// Handler for joining a game
/// Uses repository and unit of work (Clean Architecture)
/// </summary>
internal sealed class JoinGameCommandHandler : IRequestHandler<JoinGameCommand, Result<JoinGameResult>>
{
    private readonly IGameRepository _gameRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOutputCacheStore _cacheStore;

    public JoinGameCommandHandler(
        IGameRepository gameRepository,
        IUnitOfWork unitOfWork,
        IOutputCacheStore cacheStore)
    {
        _gameRepository = gameRepository;
        _unitOfWork = unitOfWork;
        _cacheStore = cacheStore;
    }

    public async Task<Result<JoinGameResult>> Handle(JoinGameCommand request, CancellationToken cancellationToken)
    {
        var result = await ValidateRequest(request)
            .BindAsync(cmd => _gameRepository.GetByIdWithParticipationsAsync(cmd.GameId, cancellationToken))
            .BindAsync(ValidateGameStatus)
            .BindAsync(game => AddParticipantToGame(game, request))
            .TapAsync(async data => await PersistParticipation(data, request, cancellationToken));

        return result.Map(data => new JoinGameResult(
            data.game.Id,
            data.participation.Id,
            $"Successfully joined game at {data.game.Location}"
        ));
    }

    private Result<JoinGameCommand> ValidateRequest(JoinGameCommand request)
    {
        var errors = new List<ValidationError>();

        if (request.GameId == Guid.Empty)
            errors.Add(new ValidationError("GameId cannot be empty"));

        if (string.IsNullOrWhiteSpace(request.UserExternalId))
            errors.Add(new ValidationError("UserExternalId is required"));

        if (string.IsNullOrWhiteSpace(request.UserName))
            errors.Add(new ValidationError("UserName is required"));

        if (string.IsNullOrWhiteSpace(request.UserSkillLevel))
            errors.Add(new ValidationError("UserSkillLevel is required"));

        return errors.Any()
            ? Result<JoinGameCommand>.Invalid(errors)
            : Result.Success(request);
    }

    private Result<Game> ValidateGameStatus(Game game)
    {
        if (game.Status == GameStatus.Canceled)
            return Result<Game>.Error("Cannot join a canceled game");

        return Result.Success(game);
    }

    private Result<(Game game, Participation participation)> AddParticipantToGame(Game game, JoinGameCommand request)
    {
        var addResult = game.AddParticipant(request.UserExternalId, request.UserName, request.UserSkillLevel);
        
        if (!addResult.IsSuccess)
            return Result<(Game, Participation)>.Error(string.Join(", ", addResult.Errors));

        var participation = game.Participations.First(p => p.UserExternalId == request.UserExternalId);
        return Result.Success((game, participation));
    }

    private async Task<Result> PersistParticipation(
        (Game game, Participation participation) data,
        JoinGameCommand request,
        CancellationToken cancellationToken)
    {
        _gameRepository.AddParticipation(data.participation);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Invalidate caches after successful join
        await _cacheStore.EvictByTagAsync(CacheTags.GamesAvailable, cancellationToken);
        await _cacheStore.EvictByTagAsync(CacheTags.GamesSearch, cancellationToken);
        // Note: We can't easily invalidate user-specific "my games" cache here because
        // it varies by Authorization header, not a specific tag. The cache will expire
        // naturally after 30 seconds.

        return Result.Success();
    }
}
