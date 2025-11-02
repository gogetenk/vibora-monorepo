using System.Net;
using FluentAssertions;
using Vibora.Integration.Tests.Infrastructure;
using Vibora.Notifications.Application.Queries.GetNotificationHistory;

namespace Vibora.Integration.Tests.Notifications;

/// <summary>
/// Integration tests for getting notification history
/// REFACTORED: Uses IntegrationTestBaseImproved with AuthenticateAs helper
/// </summary>
public class GetNotificationHistoryTests : IntegrationTestBaseImproved
{
    public GetNotificationHistoryTests(ViboraWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetNotificationHistory_WithoutAuth_ShouldReturn401()
    {
        // Arrange - No auth token
        ClearAuthentication();

        // Act
        var response = await Client.GetAsync("/notifications/history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetNotificationHistory_WithAuth_ShouldReturnEmptyList()
    {
        // Arrange
        var userExternalId = "auth0|test-user";
        AuthenticateAs(userExternalId);

        // Act
        var response = await Client.GetAsync("/notifications/history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.ReadAsAsync<List<NotificationHistoryDto>>();
        result.Should().NotBeNull();
        result.Should().BeEmpty(); // No notifications yet
    }

    [Fact]
    public async Task GetNotificationHistory_WithPagination_ShouldRespectParameters()
    {
        // Arrange
        var userExternalId = "auth0|test-user-paginated";
        AuthenticateAs(userExternalId);

        // Act
        var response = await Client.GetAsync("/notifications/history?pageNumber=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.ReadAsAsync<List<NotificationHistoryDto>>();
        result.Should().NotBeNull();
    }
}
