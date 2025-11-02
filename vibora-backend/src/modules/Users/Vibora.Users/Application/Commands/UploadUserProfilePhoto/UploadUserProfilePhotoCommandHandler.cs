using Ardalis.Result;
using MediatR;
using Vibora.Users.Application.Services;
using Vibora.Users.Domain;

namespace Vibora.Users.Application.Commands.UploadUserProfilePhoto;

internal sealed class UploadUserProfilePhotoCommandHandler
    : IRequestHandler<UploadUserProfilePhotoCommand, Result<UploadUserProfilePhotoResult>>
{
    private readonly IUserRepository _userRepository;
    private readonly IPhotoStorageService _photoStorageService;
    private readonly IUnitOfWork _unitOfWork;

    public UploadUserProfilePhotoCommandHandler(
        IUserRepository userRepository,
        IPhotoStorageService photoStorageService,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _photoStorageService = photoStorageService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<UploadUserProfilePhotoResult>> Handle(
        UploadUserProfilePhotoCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Get user
        var user = await _userRepository.GetNonGuestByExternalIdAsync(
            request.ExternalId,
            cancellationToken);

        if (user == null)
        {
            return Result<UploadUserProfilePhotoResult>.NotFound("User not found");
        }

        // 2. Delete old photo if exists
        if (!string.IsNullOrWhiteSpace(user.PhotoUrl))
        {
            await _photoStorageService.DeleteUserPhotoAsync(
                user.PhotoUrl,
                cancellationToken);
        }

        // 3. Upload new photo
        var uploadResult = await _photoStorageService.UploadUserPhotoAsync(
            request.ExternalId,
            request.PhotoStream,
            request.ContentType,
            cancellationToken);

        if (!uploadResult.IsSuccess)
        {
            return Result<UploadUserProfilePhotoResult>.Invalid(uploadResult.ValidationErrors);
        }

        // 4. Update user entity
        user.UpdateProfilePhoto(uploadResult.Value);

        // 5. Save changes
        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<UploadUserProfilePhotoResult>.Success(
            new UploadUserProfilePhotoResult(uploadResult.Value));
    }
}
