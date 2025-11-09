using Ardalis.Result;
using MediatR;
using Vibora.Games.Domain;

namespace Vibora.Games.Application.Queries.GetUserGamesCount;

/// <summary>
/// Handler for counting user participations
/// </summary>
internal sealed class GetUserGamesCountQueryHandler
    : IRequestHandler<GetUserGamesCountQuery, Result<int>>
{
    private readonly IGameRepository _gameRepository;

    public GetUserGamesCountQueryHandler(IGameRepository gameRepository)
    {
        _gameRepository = gameRepository;
    }

    public async Task<Result<int>> Handle(
        GetUserGamesCountQuery request,
        CancellationToken cancellationToken)
    {
        var count = await _gameRepository.CountUserParticipationsAsync(
            request.UserExternalId,
            cancellationToken);

        return Result<int>.Success(count);
    }
}
