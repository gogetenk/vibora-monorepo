using Ardalis.Result;
using FluentAssertions;
using NSubstitute;
using Vibora.Users.Application.Queries.GetUserById;
using Vibora.Users.Domain;
using Xunit;

namespace Vibora.Users.Tests.Application.Queries;

public class GetUserByIdQueryHandlerTests
{
    private readonly IUserRepository _userRepository;
    private readonly GetUserByIdQueryHandler _handler;

    public GetUserByIdQueryHandlerTests()
    {
        _userRepository = Substitute.For<IUserRepository>();
        _handler = new GetUserByIdQueryHandler(_userRepository);
    }

    [Fact]
    public async Task Handle_WithExistingUser_ShouldReturnUserWithBio()
    {
        // Arrange
        var externalId = "auth0|123";
        var user = User.CreateFromExternalAuth(externalId, "John Doe", "Intermediate");
        user.UpdateProfile("John Doe", "Intermediate", "Padel lover from Barcelona");
        
        _userRepository.GetByExternalIdAsync(externalId, Arg.Any<CancellationToken>())
            .Returns(user);

        var query = new GetUserByIdQuery(externalId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ExternalId.Should().Be(externalId);
        result.Value.Name.Should().Be("John Doe");
        result.Value.SkillLevel.Should().Be("Intermediate");
        result.Value.Bio.Should().Be("Padel lover from Barcelona");
    }

    [Fact]
    public async Task Handle_WithUserWithoutBio_ShouldReturnNullBio()
    {
        // Arrange
        var externalId = "auth0|456";
        var user = User.CreateFromExternalAuth(externalId, "Jane Smith", "Beginner");
        
        _userRepository.GetByExternalIdAsync(externalId, Arg.Any<CancellationToken>())
            .Returns(user);

        var query = new GetUserByIdQuery(externalId);

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
        _userRepository.GetByExternalIdAsync(externalId, Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var query = new GetUserByIdQuery(externalId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Status.Should().Be(ResultStatus.NotFound);
    }

    [Fact]
    public async Task Handle_WithEmptyBio_ShouldReturnEmptyBio()
    {
        // Arrange
        var externalId = "auth0|789";
        var user = User.CreateFromExternalAuth(externalId, "Bob Wilson", "Advanced");
        user.UpdateProfile("Bob Wilson", "Advanced", "");
        
        _userRepository.GetByExternalIdAsync(externalId, Arg.Any<CancellationToken>())
            .Returns(user);

        var query = new GetUserByIdQuery(externalId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Bio.Should().Be("");
    }
}
