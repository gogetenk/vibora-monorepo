using Ardalis.Result;
using MediatR;
using Vibora.Notifications.Domain;

namespace Vibora.Notifications.Application.Commands.UpdateNotificationPreferences;

/// <summary>
/// Handler for UpdateNotificationPreferencesCommand
/// Updates notification preferences for a user
/// Uses lazy creation - creates default preferences if they don't exist
/// </summary>
internal sealed class UpdateNotificationPreferencesCommandHandler
    : IRequestHandler<UpdateNotificationPreferencesCommand, Result>
{
    private readonly IUserNotificationPreferencesRepository _preferencesRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateNotificationPreferencesCommandHandler(
        IUserNotificationPreferencesRepository preferencesRepository,
        IUnitOfWork unitOfWork)
    {
        _preferencesRepository = preferencesRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(
        UpdateNotificationPreferencesCommand request,
        CancellationToken cancellationToken)
    {
        // Get or create preferences (lazy creation)
        var preferences = await _preferencesRepository.GetOrCreateAsync(
            request.UserExternalId,
            request.Email,
            cancellationToken);

        // Only update if at least one preference is provided
        if (request.PushEnabled.HasValue || request.SmsEnabled.HasValue || request.EmailEnabled.HasValue)
        {
            var result = preferences.UpdatePreferences(
                request.PushEnabled ?? preferences.PushEnabled,
                request.SmsEnabled ?? preferences.SmsEnabled,
                request.EmailEnabled ?? preferences.EmailEnabled
            );

            if (!result.IsSuccess)
            {
                return result;
            }

            _preferencesRepository.Update(preferences);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }
}
