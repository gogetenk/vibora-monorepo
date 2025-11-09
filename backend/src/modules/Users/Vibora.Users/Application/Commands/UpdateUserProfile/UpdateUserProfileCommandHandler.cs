using Ardalis.Result;
using MediatR;
using Microsoft.AspNetCore.OutputCaching;
using Vibora.Shared.Infrastructure.Caching;
using Vibora.Users.Application;
using Vibora.Users.Domain;

namespace Vibora.Users.Application.Commands.UpdateUserProfile;

internal sealed class UpdateUserProfileCommandHandler
    : IRequestHandler<UpdateUserProfileCommand, Result<UpdateUserProfileResult>>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOutputCacheStore _cacheStore;

    public UpdateUserProfileCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IOutputCacheStore cacheStore)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _cacheStore = cacheStore;
    }

    public async Task<Result<UpdateUserProfileResult>> Handle(
        UpdateUserProfileCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetNonGuestByExternalIdAsync(
            request.ExternalId,
            cancellationToken);

        if (user == null)
        {
            return Result<UpdateUserProfileResult>.NotFound("User not found");
        }

        // Validate SkillLevel (1-10 scale)
        if (!SkillLevelConstants.IsValid(request.SkillLevel))
        {
            return Result<UpdateUserProfileResult>.Invalid(
                new ValidationError($"Invalid SkillLevel. Must be between {SkillLevelConstants.Min} and {SkillLevelConstants.Max}"));
        }

        // UpdateProfile with FirstName, LastName, SkillLevel int, Bio
        var updateResult = user.UpdateProfile(request.FirstName, request.LastName, request.SkillLevel, request.Bio);

        if (!updateResult.IsSuccess)
        {
            return Result<UpdateUserProfileResult>.Invalid(updateResult.ValidationErrors);
        }

        // Explicitly mark the entity as modified to ensure EF Core tracks the changes
        _userRepository.Update(user);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Invalidate user-related caches
        // Note: User-specific authenticated caches (GET /users/me, GET /users/profile) vary by
        // Authorization header and will expire naturally (5 min TTL). We invalidate the general
        // Users tag which covers public profiles and other user-related queries.
        await _cacheStore.EvictByTagAsync(CacheTags.Users, cancellationToken);

        return Result<UpdateUserProfileResult>.Success(new UpdateUserProfileResult(
            user.ExternalId,
            user.FirstName,
            user.LastName,
            user.SkillLevel,
            user.Bio
        ));
    }
}
