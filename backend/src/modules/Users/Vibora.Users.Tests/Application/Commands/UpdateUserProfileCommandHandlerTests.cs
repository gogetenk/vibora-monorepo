using Ardalis.Result;
using FluentAssertions;
using NSubstitute;
using Vibora.Users.Application;
using Vibora.Users.Application.Commands.UpdateUserProfile;
using Vibora.Users.Domain;
using Xunit;

namespace Vibora.Users.Tests.Application.Commands;

public class UpdateUserProfileCommandHandlerTests
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly UpdateUserProfileCommandHandler _handler;

    public UpdateUserProfileCommandHandlerTests()
    {
        _userRepository = Substitute.For<IUserRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _handler = new UpdateUserProfileCommandHandler(_userRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_WithValidUser_ShouldUpdateProfileWithBio()
    {
        // Arrange
        var externalId = "auth0|123";
        var user = User.CreateFromExternalAuth(externalId, "John Doe", "Beginner");
        _userRepository.GetNonGuestByExternalIdAsync(externalId, Arg.Any<CancellationToken>())
            .Returns(user);

        var command = new UpdateUserProfileCommand(
            externalId,
            "John Updated",
            "Advanced",
            "Padel enthusiast from Paris");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("John Updated");
        result.Value.SkillLevel.Should().Be("Advanced");
        result.Value.Bio.Should().Be("Padel enthusiast from Paris");
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithNullBio_ShouldUpdateProfileWithoutBio()
    {
        // Arrange
        var externalId = "auth0|123";
        var user = User.CreateFromExternalAuth(externalId, "John Doe", "Beginner");
        user.UpdateProfile("John", "Intermediate", "Old bio");
        
        _userRepository.GetNonGuestByExternalIdAsync(externalId, Arg.Any<CancellationToken>())
            .Returns(user);

        var command = new UpdateUserProfileCommand(
            externalId,
            "John Updated",
            "Advanced",
            null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

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

        var command = new UpdateUserProfileCommand(
            externalId,
            "John",
            "Intermediate",
            "Bio");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Status.Should().Be(ResultStatus.NotFound);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithEmptyBio_ShouldSetEmptyBio()
    {
        // Arrange
        var externalId = "auth0|123";
        var user = User.CreateFromExternalAuth(externalId, "John Doe", "Beginner");
        _userRepository.GetNonGuestByExternalIdAsync(externalId, Arg.Any<CancellationToken>())
            .Returns(user);

        var command = new UpdateUserProfileCommand(
            externalId,
            "John",
            "Intermediate",
            "");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Bio.Should().Be("");
    }
}
