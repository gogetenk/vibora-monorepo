using Ardalis.Result;
using MediatR;
using Vibora.Games.Domain;
using Vibora.Shared.Application;

namespace Vibora.Games.Application.Commands.CreateGameShare;

/// <summary>
/// Handler for creating a shareable link for a game
/// </summary>
internal sealed class CreateGameShareCommandHandler : IRequestHandler<CreateGameShareCommand, Result<CreateGameShareResult>>
{
    private readonly IGameRepository _gameRepository;
    private readonly IGameShareRepository _gameShareRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateGameShareCommandHandler(
        IGameRepository gameRepository,
        IGameShareRepository gameShareRepository,
        IUnitOfWork unitOfWork)
    {
        _gameRepository = gameRepository;
        _gameShareRepository = gameShareRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CreateGameShareResult>> Handle(CreateGameShareCommand request, CancellationToken cancellationToken)
    {
        var result = await ValidateRequest(request)
            .TapAsync(cmd => _gameRepository.GetByIdAsync(cmd.GameId, cancellationToken))
            .BindAsync(cmd => GameShare.Create(
                cmd.GameId,
                cmd.SharedByUserExternalId,
                cmd.ExpiresAt
            ))
            .BindAsync(gameShare => PersistGameShare(gameShare, cancellationToken));

        return result.Map(BuildResult);
    }

    private Result<CreateGameShareCommand> ValidateRequest(CreateGameShareCommand request)
    {
        var errors = new List<ValidationError>();
        if (request.GameId == Guid.Empty)
            errors.Add(new ValidationError("GameId cannot be empty"));

        if (string.IsNullOrWhiteSpace(request.SharedByUserExternalId))
            errors.Add(new ValidationError("SharedByUserExternalId is required"));

        return errors.Any()
            ? Result<CreateGameShareCommand>.Invalid(errors)
            : Result.Success(request);
    }

    private async Task<Result<GameShare>> PersistGameShare(GameShare gameShare, CancellationToken cancellationToken)
    {
        await _gameShareRepository.AddAsync(gameShare, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(gameShare);
    }

    private CreateGameShareResult BuildResult(GameShare gameShare)
    {
        var shareUrl = $"/shares/{gameShare.ShareToken}";
        return new CreateGameShareResult(
            gameShare.Id,
            gameShare.ShareToken,
            shareUrl
        );
    }
}
