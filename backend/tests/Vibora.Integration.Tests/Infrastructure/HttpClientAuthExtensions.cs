using System.Net.Http.Headers;

namespace Vibora.Integration.Tests.Infrastructure;

/// <summary>
/// Extension methods for HttpClient to simplify authentication in tests
/// </summary>
public static class HttpClientAuthExtensions
{
    /// <summary>
    /// Set the authentication token for the HttpClient
    /// </summary>
    public static HttpClient WithAuth(this HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>
    /// Set the authentication for a specific user external ID
    /// </summary>
    public static HttpClient WithUser(this HttpClient client, string userExternalId, string? email = null)
    {
        var token = TestJwtGenerator.GenerateToken(userExternalId, email);
        return client.WithAuth(token);
    }

    /// <summary>
    /// Remove authentication from the HttpClient
    /// </summary>
    public static HttpClient WithoutAuth(this HttpClient client)
    {
        client.DefaultRequestHeaders.Authorization = null;
        return client;
    }
}
