using Ardalis.Result;
using MediatR;
using Vibora.Notifications.Domain;

namespace Vibora.Notifications.Application.Commands.RegisterDeviceToken;

/// <summary>
/// Handler for RegisterDeviceTokenCommand
/// Registers or updates device token for push notifications
/// Uses lazy creation - creates default preferences if they don't exist
/// </summary>
internal sealed class RegisterDeviceTokenCommandHandler
    : IRequestHandler<RegisterDeviceTokenCommand, Result>
{
    private readonly IUserNotificationPreferencesRepository _preferencesRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterDeviceTokenCommandHandler(
        IUserNotificationPreferencesRepository preferencesRepository,
        IUnitOfWork unitOfWork)
    {
        _preferencesRepository = preferencesRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(
        RegisterDeviceTokenCommand request,
        CancellationToken cancellationToken)
    {
        // Validate device token
        if (string.IsNullOrWhiteSpace(request.DeviceToken))
        {
            return Result.Invalid(new ValidationError(nameof(request.DeviceToken), "Device token cannot be empty"));
        }

        // Get or create notification preferences (lazy creation)
        var preferences = await _preferencesRepository.GetOrCreateAsync(
            request.UserExternalId,
            request.Email,
            cancellationToken);

        // Update device token
        var result = preferences.UpdateDeviceToken(request.DeviceToken);
        if (!result.IsSuccess)
        {
            return result;
        }

        _preferencesRepository.Update(preferences);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
