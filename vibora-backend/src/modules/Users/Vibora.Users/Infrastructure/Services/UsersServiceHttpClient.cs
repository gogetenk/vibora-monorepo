using System.Net.Http.Json;
using Ardalis.Result;
using Microsoft.Extensions.Logging;
using Vibora.Users.Contracts.Services;

namespace Vibora.Users.Infrastructure.Services;

/// <summary>
/// HTTP client to query Users module endpoints (Microservices mode)
/// Makes HTTP calls to Users module running as a separate service
/// PUBLIC class - can be used by other modules
/// </summary>
public sealed class UsersServiceHttpClient : IUsersServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UsersServiceHttpClient> _logger;

    public UsersServiceHttpClient(
        HttpClient httpClient,
        ILogger<UsersServiceHttpClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<Result<UserMetadataDto>> GetUserMetadataAsync(
        string externalId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/users/{externalId}", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "GetUserMetadataAsync HTTP call failed for {ExternalId}. Status: {StatusCode}, Error: {Error}",
                    externalId,
                    response.StatusCode,
                    errorContent
                );
                
                return response.StatusCode == System.Net.HttpStatusCode.NotFound
                    ? Result<UserMetadataDto>.NotFound($"User {externalId} not found")
                    : Result<UserMetadataDto>.Error($"HTTP {response.StatusCode}: {errorContent}");
            }

            var userDto = await response.Content.ReadFromJsonAsync<UserMetadataDto>(cancellationToken);
            return userDto != null
                ? Result<UserMetadataDto>.Success(userDto)
                : Result<UserMetadataDto>.Error("Failed to deserialize user metadata");
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
            var response = await _httpClient.GetAsync(
                $"/api/users/{userExternalId}/notification-settings", 
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "GetUserNotificationSettingsAsync HTTP call failed for {UserExternalId}. Status: {StatusCode}, Error: {Error}",
                    userExternalId,
                    response.StatusCode,
                    errorContent
                );
                
                return response.StatusCode == System.Net.HttpStatusCode.NotFound
                    ? Result<UserNotificationSettingsDto>.NotFound($"Settings for user {userExternalId} not found")
                    : Result<UserNotificationSettingsDto>.Error($"HTTP {response.StatusCode}: {errorContent}");
            }

            var settingsDto = await response.Content.ReadFromJsonAsync<UserNotificationSettingsDto>(cancellationToken);
            return settingsDto != null
                ? Result<UserNotificationSettingsDto>.Success(settingsDto)
                : Result<UserNotificationSettingsDto>.Error("Failed to deserialize notification settings");
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
        try
        {
            // Call batch endpoint
            var response = await _httpClient.PostAsJsonAsync(
                "/api/users/notification-settings/batch", 
                userExternalIds,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return new Dictionary<string, UserNotificationSettingsDto>();
            }

            var result = await response.Content.ReadFromJsonAsync<Dictionary<string, UserNotificationSettingsDto>>(cancellationToken);
            return result ?? new Dictionary<string, UserNotificationSettingsDto>();
        }
        catch
        {
            return new Dictionary<string, UserNotificationSettingsDto>();
        }
    }

    public async Task<string> CreateOrUpdateGuestUserAsync(
        string name,
        string? phoneNumber,
        string? email,
        int skillLevel = 5, // Default: 5 (Intermediate on 1-10 scale)
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                "/api/users/guests/create-or-update",
                new { name, phoneNumber, email, skillLevel },
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"Failed to create or update guest user: HTTP {response.StatusCode}");
            }

            var result = await response.Content.ReadFromJsonAsync<CreateOrUpdateGuestUserResponse>(cancellationToken);
            return result?.ExternalId
                ?? throw new InvalidOperationException("No ExternalId returned from guest creation");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to create or update guest user: {ex.Message}", ex);
        }
    }
}

/// <summary>
/// Response DTO for CreateOrUpdateGuestUser operation
/// </summary>
internal record CreateOrUpdateGuestUserResponse(
    string ExternalId
);
