using Ardalis.Result;
using MediatR;
using Microsoft.AspNetCore.OutputCaching;
using Vibora.Games.Application;
using Vibora.Games.Domain;
using Vibora.Shared.Infrastructure.Caching;
using Vibora.Users.Contracts.Services;

namespace Vibora.Games.Application.Commands.CreateGame;

internal sealed class CreateGameCommandHandler
    : IRequestHandler<CreateGameCommand, Result<CreateGameResult>>
{
    private readonly IGameRepository _gameRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUsersServiceClient _usersClient;
    private readonly IOutputCacheStore _cacheStore;

    public CreateGameCommandHandler(
        IGameRepository gameRepository,
        IUnitOfWork unitOfWork,
        IUsersServiceClient usersClient,
        IOutputCacheStore cacheStore)
    {
        _gameRepository = gameRepository;
        _unitOfWork = unitOfWork;
        _usersClient = usersClient;
        _cacheStore = cacheStore;
    }

    public async Task<Result<CreateGameResult>> Handle(
        CreateGameCommand request,
        CancellationToken cancellationToken)
    {
        // Query Users module to get host metadata
        var hostMetadataResult = await _usersClient.GetUserMetadataAsync(
            request.HostExternalId,
            cancellationToken);

        if (!hostMetadataResult.IsSuccess)
        {
            // Propagate the error from Users module with full context
            return Result<CreateGameResult>.Invalid(
                new ValidationError($"Host user not found: {string.Join(", ", hostMetadataResult.Errors)}"));
        }

        var hostMetadata = hostMetadataResult.Value;

        // Create game (host automatically joins with cached metadata)
        // All business validations are done in Domain
        var createResult = Game.Create(
            request.HostExternalId,
            hostMetadata.Name,
            hostMetadata.SkillLevel.ToString(), // Convert int (1-10) to string for now
            request.DateTime,
            request.Location,
            request.SkillLevel,
            request.MaxPlayers,
            request.Latitude,
            request.Longitude);

        if (!createResult.IsSuccess)
        {
            // Return validation errors or domain errors
            return Result<CreateGameResult>.Invalid(createResult.ValidationErrors);
        }

        var game = createResult.Value;

        _gameRepository.Add(game);

        // Save changes and publish domain events via Unit of Work
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Invalidate relevant caches after successful creation
        await _cacheStore.EvictByTagAsync(CacheTags.GamesAvailable, cancellationToken);
        await _cacheStore.EvictByTagAsync(CacheTags.GamesSearch, cancellationToken);

        // Return participants (host is already in Participations collection)
        var participants = game.Participations
            .Select(p => new ParticipantDto(p.UserExternalId, p.UserName, p.UserSkillLevel))
            .ToList();

        return Result<CreateGameResult>.Success(new CreateGameResult(
            game.Id,
            game.DateTime,
            game.Location,
            game.SkillLevel,
            game.MaxPlayers,
            game.HostExternalId,
            game.CurrentPlayers,
            participants,
            game.Latitude,
            game.Longitude
        ));
    }
}
