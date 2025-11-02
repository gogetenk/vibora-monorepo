using Ardalis.Result;
using MediatR;
using Microsoft.Extensions.Logging;
using Vibora.Games.Application;
using Vibora.Games.Domain;
using Vibora.Shared.Application;
using Vibora.Users.Contracts.Services;

namespace Vibora.Games.Application.Commands.JoinGameAsGuest;

/// <summary>
/// Handler for allowing a guest to join a game without creating an account
/// </summary>
internal sealed class JoinGameAsGuestCommandHandler : IRequestHandler<JoinGameAsGuestCommand, Result<JoinGameAsGuestResult>>
{
    private readonly IGameRepository _gameRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUsersServiceClient _usersServiceClient;
    private readonly ILogger<JoinGameAsGuestCommandHandler> _logger;

    public JoinGameAsGuestCommandHandler(
        IGameRepository gameRepository,
        IUnitOfWork unitOfWork,
        IUsersServiceClient usersServiceClient,
        ILogger<JoinGameAsGuestCommandHandler> logger)
    {
        _gameRepository = gameRepository;
        _unitOfWork = unitOfWork;
        _usersServiceClient = usersServiceClient;
        _logger = logger;
    }

    public async Task<Result<JoinGameAsGuestResult>> Handle(JoinGameAsGuestCommand request, CancellationToken cancellationToken)
    {
        // Railway programming: chain operations that can fail
        var result = await ValidateRequest(request)
            .BindAsync(cmd => _gameRepository.GetByIdAsync(cmd.GameId, cancellationToken))
            .BindAsync(ValidateGameStatus)
            .BindAsync(async game => await RegisterGuestUserAndAddToGame(game, request, cancellationToken))
            .TapAsync(async data => await PersistChanges(data, cancellationToken));

        return result.Map(BuildResult);
    }

    private Result<JoinGameAsGuestCommand> ValidateRequest(JoinGameAsGuestCommand request)
    {
        var errors = new List<ValidationError>();

        if (request.GameId == Guid.Empty)
            errors.Add(new ValidationError("GameId cannot be empty"));

        if (string.IsNullOrWhiteSpace(request.Name))
            errors.Add(new ValidationError("Guest name is required"));

        if (string.IsNullOrWhiteSpace(request.PhoneNumber) && string.IsNullOrWhiteSpace(request.Email))
            errors.Add(new ValidationError("Either phone number or email is required"));

        return errors.Any()
            ? Result<JoinGameAsGuestCommand>.Invalid(errors)
            : Result.Success(request);
    }

    private Result<Game> ValidateGameStatus(Game game)
    {
        if (game.Status == GameStatus.Canceled)
            return Result<Game>.Error("Cannot join a canceled game");

        if (game.Status == GameStatus.Completed)
            return Result<Game>.Error("Cannot join a completed game");

        return Result.Success(game);
    }

    private async Task<Result<(Game game, GuestParticipant guest)>> RegisterGuestUserAndAddToGame(
        Game game,
        JoinGameAsGuestCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            // PHASE 3B: Register guest in Users module BEFORE creating GuestParticipant
            var guestExternalId = await _usersServiceClient.CreateOrUpdateGuestUserAsync(
                name: request.Name,
                phoneNumber: request.PhoneNumber,
                email: request.Email,
                skillLevel: 5, // Default: 5 (Intermediate on 1-10 scale)
                cancellationToken);

            _logger.LogInformation(
                "Guest user created/updated with ExternalId {GuestExternalId} for game {GameId}",
                guestExternalId, game.Id);

            // Now add guest participant to game with the guestExternalId
            var addResult = game.AddGuestParticipant(
                request.Name,
                request.PhoneNumber,
                request.Email,
                guestExternalId); // Pass the guestExternalId

            if (!addResult.IsSuccess)
                return Result<(Game, GuestParticipant)>.Error(string.Join(", ", addResult.Errors));

            return Result.Success((game, addResult.Value));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to register guest user for game {GameId}: {Message}",
                game.Id, ex.Message);
            return Result<(Game, GuestParticipant)>.Error(
                $"Failed to process guest registration: {ex.Message}");
        }
    }

    private async Task<Result> PersistChanges((Game game, GuestParticipant guest) data, CancellationToken cancellationToken)
    {
        _gameRepository.AddGuestParticipant(data.guest);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private JoinGameAsGuestResult BuildResult((Game game, GuestParticipant guest) data)
    {
        return new JoinGameAsGuestResult(
            data.game.Id,
            data.guest.Id,
            data.guest.Name,
            data.game.CurrentPlayers,
            data.game.Status.ToString(),
            $"Successfully joined game as guest at {data.game.Location}"
        );
    }
}
