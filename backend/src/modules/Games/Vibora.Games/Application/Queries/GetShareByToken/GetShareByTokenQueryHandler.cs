using Ardalis.Result;
using MediatR;
using Vibora.Games.Application;
using Vibora.Games.Domain;

namespace Vibora.Games.Application.Queries.GetShareByToken;

/// <summary>
/// Handler for getting a game share by token and incrementing view count
/// </summary>
internal sealed class GetShareByTokenQueryHandler : IRequestHandler<GetShareByTokenQuery, Result<GetShareByTokenResult>>
{
    private readonly IGameShareRepository _gameShareRepository;
    private readonly IGameRepository _gameRepository;
    private readonly IUnitOfWork _unitOfWork;

    public GetShareByTokenQueryHandler(
        IGameShareRepository gameShareRepository,
        IGameRepository gameRepository,
        IUnitOfWork unitOfWork)
    {
        _gameShareRepository = gameShareRepository;
        _gameRepository = gameRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<GetShareByTokenResult>> Handle(GetShareByTokenQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ShareToken))
        {
            return Result.Invalid(new ValidationError("ShareToken is required"));
        }

        // Get the share by token
        var gameShare = await _gameShareRepository.GetByTokenAsync(request.ShareToken, cancellationToken);
        if (gameShare == null)
        {
            return Result.NotFound($"Share link with token '{request.ShareToken}' not found");
        }

        // Load the associated game with participants to get host display name
        var gameResult = await _gameRepository.GetByIdWithParticipationsAsync(gameShare.GameId, cancellationToken);
        if (!gameResult.IsSuccess || gameResult.Value == null)
        {
            return Result.NotFound($"Game with ID '{gameShare.GameId}' not found");
        }

        var game = gameResult.Value;

        // Get host display name
        var hostParticipation = game.Participations.FirstOrDefault(p => p.UserExternalId == game.HostExternalId);
        var hostDisplayName = hostParticipation?.UserName ?? "Unknown";

        // Increment view count if not expired (using Result pattern)
        var incrementResult = gameShare.IncrementViewCount();
        if (incrementResult.IsSuccess)
        {
            // Save the updated view count
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        // Note: We still return the share info even if increment failed (expired)

        var gameSummary = new GameSummaryDto(
            game.Id,
            game.DateTime,
            game.Location,
            game.SkillLevel,
            game.MaxPlayers,
            game.CurrentPlayers,
            game.Status.ToString(),
            hostDisplayName
        );

        return Result.Success(new GetShareByTokenResult(
            gameShare.GameId,
            gameShare.Id,
            gameShare.ShareToken,
            gameShare.ViewCount,
            gameShare.IsExpired(),
            gameSummary
        ));
    }
}
