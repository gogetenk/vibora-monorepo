using System.Net.Http.Json;

namespace Vibora.Games.Contracts.Services;

/// <summary>
/// HTTP client to query Games module endpoints (Microservices mode)
/// Makes HTTP calls to Games module running as a separate service
/// PUBLIC class - can be used by other modules
/// </summary>
public sealed class GamesServiceHttpClient : IGamesServiceClient
{
    private readonly HttpClient _httpClient;

    public GamesServiceHttpClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<int> GetUserGamesCountAsync(
        string userExternalId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"/api/games/users/{userExternalId}/count", 
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return 0;
            }

            var result = await response.Content.ReadFromJsonAsync<GameCountResponse>(cancellationToken);
            return result?.Count ?? 0;
        }
        catch
        {
            // Graceful degradation: return 0 if Games service unavailable
            return 0;
        }
    }

    public async Task<List<GuestParticipationDto>> GetGuestParticipationsByContactAsync(
        string? phoneNumber,
        string? email,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                "/api/games/guest-participations/by-contact",
                new { phoneNumber, email },
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return new List<GuestParticipationDto>();
            }

            var result = await response.Content.ReadFromJsonAsync<List<GuestParticipationDto>>(cancellationToken);
            return result ?? new List<GuestParticipationDto>();
        }
        catch
        {
            // Graceful degradation: return empty list if Games service unavailable
            return new List<GuestParticipationDto>();
        }
    }

    public async Task<int> ConvertGuestParticipationsAsync(
        List<Guid> guestParticipantIds,
        string userExternalId,
        string userName,
        string userSkillLevel,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                "/api/games/guest-participations/convert",
                new
                {
                    guestParticipantIds,
                    userExternalId,
                    userName,
                    userSkillLevel
                },
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return 0;
            }

            var result = await response.Content.ReadFromJsonAsync<ConvertGuestParticipationsResponse>(cancellationToken);
            return result?.ConvertedCount ?? 0;
        }
        catch
        {
            // Graceful degradation: return 0 if Games service unavailable
            return 0;
        }
    }

    public async Task<List<string>> GetGameParticipantIdsAsync(
        Guid gameId,
        string? excludeUserId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"/api/games/{gameId}/participants";
            if (!string.IsNullOrEmpty(excludeUserId))
            {
                url += $"?excludeUserId={Uri.EscapeDataString(excludeUserId)}";
            }

            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return new List<string>();
            }

            var result = await response.Content.ReadFromJsonAsync<List<string>>(cancellationToken);
            return result ?? new List<string>();
        }
        catch
        {
            // Graceful degradation: return empty list if Games service unavailable
            return new List<string>();
        }
    }
}

/// <summary>
/// Response DTO for GetUserGamesCount operation
/// </summary>
internal record GameCountResponse(int Count);

/// <summary>
/// Response DTO for ConvertGuestParticipations operation
/// </summary>
internal record ConvertGuestParticipationsResponse(int ConvertedCount);
