using Ardalis.Result;
using MediatR;
using Vibora.Users.Domain;

namespace Vibora.Users.Application.Commands.UpdateNotificationSettings;

/// <summary>
/// Handler for UpdateNotificationSettingsCommand
/// Creates or updates notification settings for a user
/// </summary>
internal sealed class UpdateNotificationSettingsCommandHandler 
    : IRequestHandler<UpdateNotificationSettingsCommand, Result>
{
    private readonly IUserNotificationSettingsRepository _settingsRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateNotificationSettingsCommandHandler(
        IUserNotificationSettingsRepository settingsRepository,
        IUnitOfWork unitOfWork)
    {
        _settingsRepository = settingsRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(
        UpdateNotificationSettingsCommand request, 
        CancellationToken cancellationToken)
    {
        // Get or create settings
        var settings = await _settingsRepository.GetByUserExternalIdAsync(
            request.UserExternalId, 
            cancellationToken);

        if (settings == null)
        {
            // Create new settings with default values
            settings = UserNotificationSettings.CreateDefault(request.UserExternalId);
            _settingsRepository.Add(settings);
        }

        // Update device token if provided
        if (!string.IsNullOrWhiteSpace(request.DeviceToken))
        {
            var tokenResult = settings.UpdateDeviceToken(request.DeviceToken);
            if (!tokenResult.IsSuccess)
            {
                return tokenResult;
            }
        }

        // Update phone number if provided
        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            var phoneResult = settings.UpdatePhoneNumber(request.PhoneNumber);
            if (!phoneResult.IsSuccess)
            {
                return phoneResult;
            }
        }

        // Update email if provided
        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var emailResult = settings.UpdateEmail(request.Email);
            if (!emailResult.IsSuccess)
            {
                return emailResult;
            }
        }

        // Update preferences if any are provided
        if (request.PushEnabled.HasValue || request.SmsEnabled.HasValue || request.EmailEnabled.HasValue)
        {
            var prefsResult = settings.UpdatePreferences(
                request.PushEnabled ?? settings.PushEnabled,
                request.SmsEnabled ?? settings.SmsEnabled,
                request.EmailEnabled ?? settings.EmailEnabled
            );

            if (!prefsResult.IsSuccess)
            {
                return prefsResult;
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
