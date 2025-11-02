using Ardalis.Result;
using MediatR;
using Vibora.Users.Contracts.Queries;
using Vibora.Users.Domain;

namespace Vibora.Users.Application.Queries.GetUserNotificationSettings;

/// <summary>
/// Handler for GetUserNotificationSettingsQuery from Contracts
/// Returns notification settings for a specific user
/// </summary>
internal sealed class GetUserNotificationSettingsQueryHandler 
    : IRequestHandler<GetUserNotificationSettingsQuery, Result<UserNotificationSettingsResult>>
{
    private readonly IUserNotificationSettingsRepository _settingsRepository;

    public GetUserNotificationSettingsQueryHandler(IUserNotificationSettingsRepository settingsRepository)
    {
        _settingsRepository = settingsRepository;
    }

    public async Task<Result<UserNotificationSettingsResult>> Handle(
        GetUserNotificationSettingsQuery request, 
        CancellationToken cancellationToken)
    {
        var settings = await _settingsRepository.GetByUserExternalIdAsync(
            request.UserExternalId, 
            cancellationToken);

        if (settings == null)
        {
            return Result<UserNotificationSettingsResult>.NotFound(
                $"Notification settings not found for user {request.UserExternalId}");
        }

        var result = new UserNotificationSettingsResult(
            settings.UserExternalId,
            settings.DeviceToken,
            settings.PhoneNumber,
            settings.Email,
            settings.PushEnabled,
            settings.SmsEnabled,
            settings.EmailEnabled
        );

        return Result<UserNotificationSettingsResult>.Success(result);
    }
}
