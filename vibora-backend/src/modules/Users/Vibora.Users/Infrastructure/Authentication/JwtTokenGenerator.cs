using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Vibora.Users.Infrastructure.Authentication;

public interface IJwtTokenGenerator
{
    string GenerateGuestToken(string externalId, string name);
}

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly IConfiguration _configuration;

    public JwtTokenGenerator(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateGuestToken(string externalId, string name)
    {
        var jwtSecret = _configuration["Jwt:Secret"] 
            ?? "test-secret-key-for-integration-tests-only-minimum-256-bits";

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, externalId),
            new Claim(ClaimTypes.Name, name),
            new Claim("user_type", "guest")
        };

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddDays(30), // Guest tokens valides 30 jours
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
