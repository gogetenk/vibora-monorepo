using Ardalis.Result;
using MediatR;
using Microsoft.Extensions.Logging;
using Vibora.Games.Contracts.Services;
using Vibora.Users.Application;
using Vibora.Users.Domain;

namespace Vibora.Users.Application.Commands.SyncUserFromAuth;

internal sealed class SyncUserFromAuthCommandHandler
    : IRequestHandler<SyncUserFromAuthCommand, Result<SyncUserFromAuthResult>>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IGamesServiceClient _gamesServiceClient;
    private readonly ILogger<SyncUserFromAuthCommandHandler> _logger;

    public SyncUserFromAuthCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IGamesServiceClient gamesServiceClient,
        ILogger<SyncUserFromAuthCommandHandler> logger)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _gamesServiceClient = gamesServiceClient;
        _logger = logger;
    }

    public async Task<Result<SyncUserFromAuthResult>> Handle(
        SyncUserFromAuthCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Validate request (Railway step 1)
        var validationResult = ValidateRequest(request);
        if (!validationResult.IsSuccess)
            return Result<SyncUserFromAuthResult>.Invalid(validationResult.ValidationErrors);

        // Validate SkillLevel (1-10 scale)
        if (!SkillLevelConstants.IsValid(request.SkillLevel))
        {
            return Result<SyncUserFromAuthResult>.Invalid(
                new ValidationError($"Invalid SkillLevel. Must be between {SkillLevelConstants.Min} and {SkillLevelConstants.Max}"));
        }

        // 2. Check if user already exists (Railway step 2)
        var existingUser = await _userRepository.GetNonGuestByExternalIdAsync(
            request.ExternalId, cancellationToken);

        if (existingUser != null)
        {
            return await HandleExistingUser(existingUser, request, cancellationToken);
        }

        // 3. Create new user (Railway step 3)
        return await HandleNewUser(request, request.SkillLevel, cancellationToken);
    }

    private static Result ValidateRequest(SyncUserFromAuthCommand request)
    {
        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(request.ExternalId))
            errors.Add(new ValidationError("ExternalId is required"));

        if (string.IsNullOrWhiteSpace(request.Name))
            errors.Add(new ValidationError("Name is required"));

        return errors.Any() ? Result.Invalid(errors) : Result.Success();
    }

    private async Task<Result<SyncUserFromAuthResult>> HandleExistingUser(
        User existingUser,
        SyncUserFromAuthCommand request,
        CancellationToken cancellationToken)
    {
        existingUser.SyncFromExternalProvider(request.Name);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<SyncUserFromAuthResult>.Success(new SyncUserFromAuthResult(
            existingUser.ExternalId,
            existingUser.Name,
            existingUser.SkillLevel,
            IsNewUser: false
        ));
    }

    private async Task<Result<SyncUserFromAuthResult>> HandleNewUser(
        SyncUserFromAuthCommand request,
        int skillLevel,
        CancellationToken cancellationToken)
    {
        // Create and persist new user
        // Use FirstName/LastName if available, otherwise fall back to Name
        var firstName = request.FirstName ?? request.Name;
        var user = User.CreateFromExternalAuth(
            request.ExternalId, 
            firstName, 
            skillLevel,
            request.LastName,
            request.Email);
        _userRepository.Add(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // TODO: Publish UserCreatedEvent via MassTransit

        // PHASE 3B: Auto-claim guest participations (non-blocking)
        await TryAutoClaimGuestParticipationsAsync(user, request, cancellationToken);

        return Result<SyncUserFromAuthResult>.Success(new SyncUserFromAuthResult(
            user.ExternalId,
            user.Name,
            user.SkillLevel,
            IsNewUser: true
        ));
    }

    private async Task TryAutoClaimGuestParticipationsAsync(
        User user,
        SyncUserFromAuthCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.PhoneNumber) &&
            string.IsNullOrWhiteSpace(request.Email))
            return;

        try
        {
            // Step 1: Find guest participations via cross-module service client
            var guestParticipations = await _gamesServiceClient.GetGuestParticipationsByContactAsync(
                request.PhoneNumber,
                request.Email,
                cancellationToken);

            if (guestParticipations.Count == 0)
            {
                _logger.LogDebug(
                    "User {ExternalId} signed up with contact info but no matching guest participations found",
                    user.ExternalId);
                return;
            }

            // Step 2: Convert guest participations to user participations
            var guestIds = guestParticipations.Select(gp => gp.GuestParticipantId).ToList();
            var convertedCount = await _gamesServiceClient.ConvertGuestParticipationsAsync(
                guestIds,
                user.ExternalId,
                user.Name,
                user.SkillLevel.ToString(),
                cancellationToken);

            if (convertedCount > 0)
            {
                _logger.LogInformation(
                    "User {ExternalId} automatically claimed {Count} guest participations on signup",
                    user.ExternalId,
                    convertedCount);
            }
        }
        catch (Exception ex)
        {
            // CRITICAL: Signup must succeed even if claim fails
            _logger.LogError(ex,
                "Failed to auto-claim guest participations for user {ExternalId}. User can claim manually later.",
                user.ExternalId);
        }
    }
}
