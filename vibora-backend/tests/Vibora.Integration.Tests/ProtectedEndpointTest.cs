using System.Net.Http.Headers;
using Vibora.Integration.Tests.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace Vibora.Integration.Tests;

public class ProtectedEndpointTest : IntegrationTestBase
{
    private readonly ITestOutputHelper _output;

    public ProtectedEndpointTest(ViboraWebApplicationFactory factory, ITestOutputHelper output) : base(factory)
    {
        _output = output;
    }

    [Fact]
    public async Task TestProtectedEndpoint_WithManualToken()
    {
        // Generate token manually
        var userId = "auth0|test-123";
        var token = TestJwtGenerator.GenerateToken(userId);
        _output.WriteLine($"Token: {token.Substring(0, 50)}...");
        
        // Set it directly
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        
        _output.WriteLine($"Header set: {Client.DefaultRequestHeaders.Authorization}");
        
        // Try a protected endpoint - /games/me requires auth
        var response = await Client.GetAsync("/games/me");
        
        _output.WriteLine($"Status: {response.StatusCode}");
        var body = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"Body: {body}");
    }
    
    [Fact]
    public async Task TestProtectedEndpoint_WithManualTokenSetup()
    {
        // Generate token and set header manually
        var userId = "auth0|test-123";
        var token = TestJwtGenerator.GenerateToken(userId);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        
        _output.WriteLine($"Header set: {Client.DefaultRequestHeaders.Authorization}");
        
        // Try a protected endpoint
        var response = await Client.GetAsync("/games/me");
        
        _output.WriteLine($"Status: {response.StatusCode}");
        var body = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"Body: {body}");
    }
}
