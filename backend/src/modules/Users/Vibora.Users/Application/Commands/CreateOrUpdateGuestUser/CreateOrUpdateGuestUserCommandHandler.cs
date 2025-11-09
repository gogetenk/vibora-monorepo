using Ardalis.Result;
using MediatR;
using Microsoft.Extensions.Logging;
using Vibora.Users.Application.Commands.CreateGuestUser;
using Vibora.Users.Domain;

namespace Vibora.Users.Application.Commands.CreateOrUpdateGuestUser;

/// <summary>
/// Handler for creating or updating guest users
///
/// Architecture: This handler contains the business orchestration logic:
/// 1. Search for existing guest by phone/email
/// 2. Update existing or create new
/// 3. Persist to database
///
/// This is the CORRECT PLACE for this logic (Application Layer).
/// It was previously in UsersServiceInProcessClient (Infrastructure Layer - WRONG).
/// </summary>
internal sealed class CreateOrUpdateGuestUserCommandHandler
    : IRequestHandler<CreateOrUpdateGuestUserCommand, Result<CreateOrUpdateGuestUserResult>>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISender _sender;
    private readonly ILogger<CreateOrUpdateGuestUserCommandHandler> _logger;

    public CreateOrUpdateGuestUserCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ISender sender,
        ILogger<CreateOrUpdateGuestUserCommandHandler> logger)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _sender = sender;
        _logger = logger;
    }

    public async Task<Result<CreateOrUpdateGuestUserResult>> Handle(
        CreateOrUpdateGuestUserCommand request,
        CancellationToken cancellationToken)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Result<CreateOrUpdateGuestUserResult>.Invalid(
                new ValidationError("Name is required"));
        }

        // Validate SkillLevel (1-10 scale)
        if (!SkillLevelConstants.IsValid(request.SkillLevel))
        {
            return Result<CreateOrUpdateGuestUserResult>.Invalid(
                new ValidationError($"Invalid SkillLevel. Must be between {SkillLevelConstants.Min} and {SkillLevelConstants.Max}"));
        }

        // ORCHESTRATION: Try to find existing guest with matching contact
        User? existingGuest = null;

        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            existingGuest = await _userRepository.GetGuestByPhoneNumberAsync(
                request.PhoneNumber, cancellationToken);

            if (existingGuest != null)
            {
                _logger.LogDebug(
                    "Found existing guest user by phone number: {ExternalId}",
                    existingGuest.ExternalId);
            }
        }

        // If no phone match, try email match
        if (existingGuest == null && !string.IsNullOrWhiteSpace(request.Email))
        {
            existingGuest = await _userRepository.GetGuestByEmailAsync(
                request.Email, cancellationToken);

            if (existingGuest != null)
            {
                _logger.LogDebug(
                    "Found existing guest user by email: {ExternalId}",
                    existingGuest.ExternalId);
            }
        }

        // If existing guest found, update contact info and return
        if (existingGuest != null)
        {
            // Update contact info (may have changed since last join)
            existingGuest.SetContactInfo(request.PhoneNumber, request.Email);
            _userRepository.Update(existingGuest);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Updated existing guest user: {ExternalId} with phone={Phone}, email={Email}",
                existingGuest.ExternalId,
                request.PhoneNumber != null ? "***" : "null",
                request.Email != null ? "***" : "null");

            return Result<CreateOrUpdateGuestUserResult>.Success(
                new CreateOrUpdateGuestUserResult(existingGuest.ExternalId));
        }

        // Create new guest user
        var createCommand = new CreateGuestUserCommand(
            request.Name,
            request.SkillLevel,
            request.PhoneNumber,
            request.Email);

        var createResult = await _sender.Send(createCommand, cancellationToken);

        if (!createResult.IsSuccess)
        {
            _logger.LogError(
                "Failed to create guest user: {Errors}",
                string.Join(", ", createResult.Errors));

            return Result<CreateOrUpdateGuestUserResult>.Error(
                $"Failed to create guest user: {string.Join(", ", createResult.Errors)}");
        }

        _logger.LogInformation(
            "Created new guest user: {ExternalId} with name={Name}, phone={Phone}, email={Email}",
            createResult.Value.ExternalId,
            request.Name,
            request.PhoneNumber != null ? "***" : "null",
            request.Email != null ? "***" : "null");

        return Result<CreateOrUpdateGuestUserResult>.Success(
            new CreateOrUpdateGuestUserResult(createResult.Value.ExternalId));
    }
}
