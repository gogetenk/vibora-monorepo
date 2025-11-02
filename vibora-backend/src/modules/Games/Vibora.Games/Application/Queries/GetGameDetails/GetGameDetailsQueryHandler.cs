using Ardalis.Result;
using MediatR;
using Vibora.Games.Domain;

namespace Vibora.Games.Application.Queries.GetGameDetails;

/// <summary>
/// Handler for retrieving detailed game information
/// Uses repository for data access (Clean Architecture)
/// </summary>
internal sealed class GetGameDetailsQueryHandler
    : IRequestHandler<GetGameDetailsQuery, Result<GameDetailsResult>>
{
    private readonly IGameRepository _gameRepository;

    public GetGameDetailsQueryHandler(IGameRepository gameRepository)
    {
        _gameRepository = gameRepository;
    }

    public async Task<Result<GameDetailsResult>> Handle(
        GetGameDetailsQuery request,
        CancellationToken cancellationToken)
    {
        if (request.GameId == Guid.Empty)
            return Result.Invalid(new ValidationError("GameId cannot be empty"));

        var gameResult = await _gameRepository.GetByIdWithParticipationsAsync(
            request.GameId,
            cancellationToken
        );

        return gameResult.Map(game =>
        {
            // Map registered users
            var registeredParticipants = game.Participations
                .Select(p => new ParticipantInfoDto(
                    Type: "User",
                    ParticipationId: p.Id,
                    Identifier: p.UserExternalId,
                    DisplayName: p.UserName,
                    SkillLevel: p.UserSkillLevel,
                    ContactInfo: null,
                    IsHost: p.IsHost,
                    JoinedAt: p.JoinedAt
                ));

            // Map guest participants
            var guestParticipants = game.GuestParticipants
                .Select(g => new ParticipantInfoDto(
                    Type: "Guest",
                    ParticipationId: null,
                    Identifier: $"Guest: {g.Name}",
                    DisplayName: g.Name,
                    SkillLevel: null,
                    ContactInfo: g.GetContactIdentifier(),
                    IsHost: false,
                    JoinedAt: g.JoinedAt
                ));

            // Merge and sort: Host first, then chronological
            var allParticipants = registeredParticipants
                .Concat(guestParticipants)
                .OrderByDescending(p => p.IsHost)
                .ThenBy(p => p.JoinedAt)
                .ToList();

            return new GameDetailsResult(
                game.Id,
                game.DateTime,
                game.Location,
                game.SkillLevel,
                game.MaxPlayers,
                game.CurrentPlayers,
                game.HostExternalId,
                game.Status.ToString(),
                game.CreatedAt,
                allParticipants
            );
        });
    }
}
