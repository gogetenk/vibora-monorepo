using Ardalis.Result;
using MediatR;
using Vibora.Games.Domain;

namespace Vibora.Games.Application.Queries.GetGameParticipantIds;

/// <summary>
/// Handler for retrieving participant user IDs for a game
/// </summary>
internal sealed class GetGameParticipantIdsQueryHandler
    : IRequestHandler<GetGameParticipantIdsQuery, Result<List<string>>>
{
    private readonly IGameRepository _gameRepository;

    public GetGameParticipantIdsQueryHandler(IGameRepository gameRepository)
    {
        _gameRepository = gameRepository;
    }

    public async Task<Result<List<string>>> Handle(
        GetGameParticipantIdsQuery request,
        CancellationToken cancellationToken)
    {
        var participantIds = await _gameRepository.GetGameParticipantUserIdsAsync(
            request.GameId,
            request.ExcludeUserId,
            cancellationToken);

        return Result<List<string>>.Success(participantIds);
    }
}
