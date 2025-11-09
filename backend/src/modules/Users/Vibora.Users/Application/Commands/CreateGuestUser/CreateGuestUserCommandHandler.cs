using Ardalis.Result;
using MediatR;
using Vibora.Users.Application;
using Vibora.Users.Domain;
using Vibora.Users.Infrastructure.Authentication;

namespace Vibora.Users.Application.Commands.CreateGuestUser;

internal sealed class CreateGuestUserCommandHandler
    : IRequestHandler<CreateGuestUserCommand, Result<CreateGuestUserResult>>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public CreateGuestUserCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<Result<CreateGuestUserResult>> Handle(
        CreateGuestUserCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Result<CreateGuestUserResult>.Invalid(
                new ValidationError("Name is required"));
        }

        // Validate SkillLevel (1-10 scale)
        if (!SkillLevelConstants.IsValid(request.SkillLevel))
        {
            return Result<CreateGuestUserResult>.Invalid(
                new ValidationError($"Invalid SkillLevel. Must be between {SkillLevelConstants.Min} and {SkillLevelConstants.Max}"));
        }

        var user = User.CreateGuestUser(
            request.Name,
            request.SkillLevel,
            request.PhoneNumber,
            request.Email);

        _userRepository.Add(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Generate JWT token for guest user
        var token = _jwtTokenGenerator.GenerateGuestToken(user.ExternalId, user.Name);

        return Result<CreateGuestUserResult>.Success(new CreateGuestUserResult(
            user.ExternalId,
            user.Name,
            user.SkillLevel,
            token,
            user.PhoneNumber,
            user.Email
        ));
    }
}
