using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Vibora.Integration.Tests.Infrastructure;

/// <summary>
/// Helper to generate JWT tokens for integration tests
/// Simulates Supabase JWT structure with real signatures
/// </summary>
public static class TestJwtGenerator
{
    // Test-only signing key (not used in production)
    private static readonly SymmetricSecurityKey SigningKey = 
        new SymmetricSecurityKey(Encoding.UTF8.GetBytes("test-secret-key-for-integration-tests-only-minimum-256-bits"));

    /// <summary>
    /// Generate a test JWT token with the given user external ID
    /// Mimics Supabase JWT structure with common claims
    /// In Development mode, the signature is not validated, but the JWT must be well-formed
    /// </summary>
    /// <param name="userExternalId">User external ID (e.g., "auth0|user-123" or Supabase UUID)</param>
    /// <param name="email">Optional email claim</param>
    /// <param name="role">Optional role claim (default: "authenticated")</param>
    /// <returns>JWT token string with valid structure and signature</returns>
    public static string GenerateToken(
        string userExternalId, 
        string? email = null,
        string role = "authenticated")
    {
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, userExternalId), // Only one sub claim
            new Claim("role", role)
        };

        if (!string.IsNullOrEmpty(email))
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.Email, email)); // Standard email claim
        }

        var tokenHandler = new JwtSecurityTokenHandler();
        var now = DateTime.UtcNow;
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = "https://test.supabase.co/auth/v1",
            Audience = "authenticated",
            NotBefore = now,
            Expires = now.AddHours(1),
            IssuedAt = now,
            SigningCredentials = new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    /// <summary>
    /// Generate a JWT for a default test user (auth0|test-user-123)
    /// </summary>
    public static string GenerateDefaultUserToken()
    {
        return GenerateToken("auth0|test-user-123", "testuser@example.com");
    }

    /// <summary>
    /// Generate a JWT with a random user ID (useful for isolated tests)
    /// </summary>
    public static string GenerateRandomUserToken()
    {
        var randomId = $"auth0|test-{Guid.NewGuid()}";
        return GenerateToken(randomId);
    }
}
