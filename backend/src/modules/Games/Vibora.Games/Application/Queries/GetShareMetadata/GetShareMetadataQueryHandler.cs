using Ardalis.Result;
using MediatR;
using Vibora.Games.Domain;

namespace Vibora.Games.Application.Queries.GetShareMetadata;

/// <summary>
/// Handler for getting Open Graph metadata for a shared game
/// </summary>
internal sealed class GetShareMetadataQueryHandler : IRequestHandler<GetShareMetadataQuery, Result<GetShareMetadataResult>>
{
    private readonly IGameShareRepository _gameShareRepository;

    public GetShareMetadataQueryHandler(IGameShareRepository gameShareRepository)
    {
        _gameShareRepository = gameShareRepository;
    }

    public async Task<Result<GetShareMetadataResult>> Handle(GetShareMetadataQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ShareToken))
        {
            return Result.Invalid(new ValidationError("ShareToken is required"));
        }

        // Get the share with game details
        var gameShare = await _gameShareRepository.GetByTokenAsync(request.ShareToken, cancellationToken);
        if (gameShare == null)
        {
            return Result.NotFound($"Share link with token '{request.ShareToken}' not found");
        }

        var game = gameShare.Game;

        // Build metadata for social media previews
        var title = $"Partie de Padel - {game.Location}";
        var description = $"Rejoignez cette partie le {game.DateTime:dd/MM/yyyy à HH:mm}. " +
                         $"Niveau: {game.SkillLevel}. " +
                         $"{game.CurrentPlayers}/{game.MaxPlayers} joueurs inscrits.";

        return Result.Success(new GetShareMetadataResult(
            title,
            description,
            game.Location,
            game.DateTime,
            game.SkillLevel,
            game.CurrentPlayers,
            game.MaxPlayers,
            game.Status.ToString()
        ));
    }
}
