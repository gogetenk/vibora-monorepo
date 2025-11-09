using Ardalis.Result;
using FluentAssertions;
using NSubstitute;
using Vibora.Users.Application.Queries.GetCurrentUser;
using Vibora.Users.Domain;
using Xunit;

namespace Vibora.Users.Tests.Application.Queries;

public class GetCurrentUserQueryHandlerTests
{
    private readonly IUserRepository _userRepository;
    private readonly GetCurrentUserQueryHandler _handler;

    public GetCurrentUserQueryHandlerTests()
    {
        _userRepository = Substitute.For<IUserRepository>();
        _handler = new GetCurrentUserQueryHandler(_userRepository);
    }

    [Fact]
    public async Task Handle_WithExistingUser_ShouldReturnCurrentUserWithBio()
    {
        // Arrange
        var externalId = "auth0|current-user";
        var user = User.CreateFromExternalAuth(externalId, "Current User", "Intermediate");
        user.UpdateProfile("Current User", "Intermediate", "Love playing padel every weekend");
        
        _userRepository.GetNonGuestByExternalIdAsync(externalId, Arg.Any<CancellationToken>())
            .Returns(user);

        var query = new GetCurrentUserQuery(externalId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ExternalId.Should().Be(externalId);
        result.Value.Name.Should().Be("Current User");
        result.Value.SkillLevel.Should().Be("Intermediate");
        result.Value.Bio.Should().Be("Love playing padel every weekend");
    }

    [Fact]
    public async Task Handle_WithUserWithoutBio_ShouldReturnNullBio()
    {
        // Arrange
        var externalId = "auth0|no-bio-user";
        var user = User.CreateFromExternalAuth(externalId, "No Bio User", "Beginner");
        
        _userRepository.GetNonGuestByExternalIdAsync(externalId, Arg.Any<CancellationToken>())
            .Returns(user);

        var query = new GetCurrentUserQuery(externalId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Bio.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithNonExistentUser_ShouldReturnNotFound()
    {
        // Arrange
        var externalId = "auth0|nonexistent";
        _userRepository.GetNonGuestByExternalIdAsync(externalId, Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var query = new GetCurrentUserQuery(externalId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Status.Should().Be(ResultStatus.NotFound);
        result.Errors.Should().Contain(e => e.Contains("Please sync from auth provider first"));
    }
}
