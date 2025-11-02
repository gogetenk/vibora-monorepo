using Ardalis.Result;
using MediatR;
using Microsoft.Extensions.Logging;
using Vibora.Users.Application.Commands.CreateOrUpdateGuestUser;
using Vibora.Users.Contracts.Queries;
using Vibora.Users.Contracts.Services;

namespace Vibora.Users.Infrastructure.Services;

/// <summary>
/// In-process client to query Users module (Monolith mode)
/// Calls Users module handlers directly via MediatR (same process)
///
/// ARCHITECTURE PRINCIPLE: This class is a DUMB PROXY
/// - Injects ONLY ISender (MediatR)
/// - Each method = ONE MediatR call
/// - Performs DTO mapping (internal → public)
/// - NO business logic, NO orchestration
///
/// Business orchestration lives in Application Layer (Command/Query Handlers).
/// PUBLIC class - can be used by other modules
/// </summary>
public sealed class UsersServiceInProcessClient : IUsersServiceClient
{
    private readonly ISender _sender;
    private readonly ILogger<UsersServiceInProcessClient> _logger;

    /// <summary>
    /// Constructor - injected via DI in UsersModuleServiceRegistrar
    /// </summary>
    public UsersServiceInProcessClient(
        ISender sender,
        ILogger<UsersServiceInProcessClient> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    public async Task<Result<UserMetadataDto>> GetUserMetadataAsync(
        string externalId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetUserByExternalIdQuery(externalId);
            var result = await _sender.Send(query, cancellationToken);

            if (!result.IsSuccess)
            {
                _logger.LogWarning(
                    "GetUserMetadataAsync failed for {ExternalId}. Status: {Status}, Errors: {Errors}, ValidationErrors: {ValidationErrors}",
                    externalId,
                    result.Status,
                    string.Join(", ", result.Errors),
                    string.Join(", ", result.ValidationErrors.Select(e => e.ErrorMessage))
                );
                return Result<UserMetadataDto>.NotFound(result.Errors.ToArray());
            }

            var userMetadata = result.Value;
            
            return Result<UserMetadataDto>.Success(new UserMetadataDto(
                userMetadata.ExternalId,
                userMetadata.Name,
                userMetadata.SkillLevel
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception in GetUserMetadataAsync for {ExternalId}", externalId);
            return Result<UserMetadataDto>.Error(ex.Message);
        }
    }

    public async Task<Result<UserNotificationSettingsDto>> GetUserNotificationSettingsAsync(
        string userExternalId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetUserNotificationSettingsQuery(userExternalId);
            var result = await _sender.Send(query, cancellationToken);

            if (!result.IsSuccess)
            {
                _logger.LogWarning(
                    "GetUserNotificationSettingsAsync failed for {UserExternalId}. Status: {Status}, Errors: {Errors}, ValidationErrors: {ValidationErrors}",
                    userExternalId,
                    result.Status,
                    string.Join(", ", result.Errors),
                    string.Join(", ", result.ValidationErrors.Select(e => e.ErrorMessage))
                );
                // Propagate the Result
                return Result<UserNotificationSettingsDto>.NotFound(result.Errors.ToArray());
            }

            var settings = result.Value;

            return Result<UserNotificationSettingsDto>.Success(new UserNotificationSettingsDto(
                settings.UserExternalId,
                settings.DeviceToken,
                settings.PhoneNumber,
                settings.Email,
                settings.PushEnabled,
                settings.SmsEnabled,
                settings.EmailEnabled
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception in GetUserNotificationSettingsAsync for {UserExternalId}", userExternalId);
            return Result<UserNotificationSettingsDto>.Error(ex.Message);
        }
    }

    public async Task<Dictionary<string, UserNotificationSettingsDto>> GetUserNotificationSettingsBatchAsync(
        IEnumerable<string> userExternalIds,
        CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<string, UserNotificationSettingsDto>();

        // TODO: Optimize with a batch query in the future
        // For now, execute multiple queries in parallel
        var tasks = userExternalIds.Select(async userId =>
        {
            var settings = await GetUserNotificationSettingsAsync(userId, cancellationToken);
            return (userId, settings);
        });

        var results = await Task.WhenAll(tasks);

        foreach (var (userId, settings) in results)
        {
            if (settings != null)
            {
                result[userId] = settings;
            }
        }

        return result;
    }

    public async Task<string> CreateOrUpdateGuestUserAsync(
        string name,
        string? phoneNumber,
        string? email,
        int skillLevel = 5, // Default: 5 (Intermediate on 1-10 scale)
        CancellationToken cancellationToken = default)
    {
        // ARCHITECTURE: Simple proxy - delegate ALL orchestration to Command Handler
        var command = new CreateOrUpdateGuestUserCommand(
            name,
            phoneNumber,
            email,
            skillLevel);

        var result = await _sender.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(
                $"Failed to create/update guest user: {string.Join(", ", result.Errors)}");
        }

        return result.Value.ExternalId;
    }
}
