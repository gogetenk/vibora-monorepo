using System.Net.Http.Json;
using System.Text.Json;

namespace Vibora.Integration.Tests.Infrastructure;

/// <summary>
/// Enhanced extension methods for HttpClient in integration tests
/// </summary>
public static class HttpClientExtensions
{
    private static readonly JsonSerializerOptions DefaultJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Deserialize response body to strongly-typed object
    /// Throws on error with detailed message
    /// </summary>
    public static async Task<T> ReadAsAsync<T>(this HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"HTTP request failed with status {response.StatusCode}. Body: {errorBody}");
        }

        var content = await response.Content.ReadAsStringAsync();
        
        var result = JsonSerializer.Deserialize<T>(content, DefaultJsonOptions);
        
        if (result == null)
        {
            throw new InvalidOperationException($"Failed to deserialize response to {typeof(T).Name}");
        }

        return result;
    }

    /// <summary>
    /// Try to deserialize response, returns null on failure
    /// </summary>
    public static async Task<T?> TryReadAsAsync<T>(this HttpResponseMessage response) where T : class
    {
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        try
        {
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(content, DefaultJsonOptions);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Read error response body as string
    /// </summary>
    public static async Task<string> ReadErrorAsync(this HttpResponseMessage response)
    {
        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>
    /// Assert response is successful and return deserialized body
    /// Useful for one-liner assertions
    /// </summary>
    public static async Task<T> EnsureSuccessAndReadAsync<T>(this HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        return await response.ReadAsAsync<T>();
    }
}
