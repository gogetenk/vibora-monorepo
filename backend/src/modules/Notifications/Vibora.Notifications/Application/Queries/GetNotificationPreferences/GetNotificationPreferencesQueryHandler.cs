using Ardalis.Result;
using MediatR;
using Vibora.Notifications.Domain;

namespace Vibora.Notifications.Application.Queries.GetNotificationPreferences;

/// <summary>
/// Handler for GetNotificationPreferencesQuery
/// Returns notification preferences for the current user
/// Creates default preferences if they don't exist (lazy creation)
/// </summary>
internal sealed class GetNotificationPreferencesQueryHandler
    : IRequestHandler<GetNotificationPreferencesQuery, Result<GetNotificationPreferencesResult>>
{
    private readonly IUserNotificationPreferencesRepository _preferencesRepository;

    public GetNotificationPreferencesQueryHandler(IUserNotificationPreferencesRepository preferencesRepository)
    {
        _preferencesRepository = preferencesRepository;
    }

    public async Task<Result<GetNotificationPreferencesResult>> Handle(
        GetNotificationPreferencesQuery request,
        CancellationToken cancellationToken)
    {
        // Use GetOrCreateAsync for lazy creation pattern
        var preferences = await _preferencesRepository.GetOrCreateAsync(
            request.UserExternalId,
            request.Email,
            cancellationToken);

        var result = new GetNotificationPreferencesResult(
            preferences.DeviceToken,
            preferences.PhoneNumber,
            preferences.Email,
            preferences.PushEnabled,
            preferences.SmsEnabled,
            preferences.EmailEnabled
        );

        return Result<GetNotificationPreferencesResult>.Success(result);
    }
}
