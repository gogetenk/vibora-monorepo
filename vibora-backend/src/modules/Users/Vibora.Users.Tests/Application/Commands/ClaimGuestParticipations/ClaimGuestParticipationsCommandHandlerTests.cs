using Ardalis.Result;
using FluentAssertions;
using Moq;
using Vibora.Games.Contracts.Services;
using Vibora.Users.Application.Commands.ClaimGuestParticipations;
using Vibora.Users.Domain;

namespace Vibora.Users.Tests.Application.Commands.ClaimGuestParticipations;

public class ClaimGuestParticipationsCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IGamesServiceClient> _gamesServiceClientMock;
    private readonly ClaimGuestParticipationsCommandHandler _handler;

    public ClaimGuestParticipationsCommandHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _gamesServiceClientMock = new Mock<IGamesServiceClient>();
        _handler = new ClaimGuestParticipationsCommandHandler(
            _userRepositoryMock.Object,
            _gamesServiceClientMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidPhoneNumber_ShouldClaimParticipations()
    {
        // Arrange
        var externalId = "auth0|123";
        var phoneNumber = "+33612345678";
        var user = User.CreateFromExternalAuth(externalId, "John Doe", SkillLevel.Intermediate);

        _userRepositoryMock
            .Setup(x => x.GetNonGuestByExternalIdAsync(externalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var guestParticipations = new List<GuestParticipationDto>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), "Guest John", phoneNumber, null, DateTime.UtcNow),
            new(Guid.NewGuid(), Guid.NewGuid(), "Guest John", phoneNumber, null, DateTime.UtcNow)
        };

        _gamesServiceClientMock
            .Setup(x => x.GetGuestParticipationsByContactAsync(phoneNumber, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(guestParticipations);

        _gamesServiceClientMock
            .Setup(x => x.ConvertGuestParticipationsAsync(
                It.IsAny<List<Guid>>(),
                externalId,
                user.Name,
                user.SkillLevel.ToString(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        var command = new ClaimGuestParticipationsCommand(externalId, phoneNumber, null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ClaimedParticipations.Should().Be(2);
        result.Value.ClaimedGames.Should().HaveCount(2);

        _gamesServiceClientMock.Verify(
            x => x.ConvertGuestParticipationsAsync(
                It.Is<List<Guid>>(ids => ids.Count == 2),
                externalId,
                user.Name,
                user.SkillLevel.ToString(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidEmail_ShouldClaimParticipations()
    {
        // Arrange
        var externalId = "auth0|456";
        var email = "guest@example.com";
        var user = User.CreateFromExternalAuth(externalId, "Jane Smith", SkillLevel.Advanced);

        _userRepositoryMock
            .Setup(x => x.GetNonGuestByExternalIdAsync(externalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var guestParticipations = new List<GuestParticipationDto>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), "Guest Jane", null, email, DateTime.UtcNow)
        };

        _gamesServiceClientMock
            .Setup(x => x.GetGuestParticipationsByContactAsync(null, email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(guestParticipations);

        _gamesServiceClientMock
            .Setup(x => x.ConvertGuestParticipationsAsync(
                It.IsAny<List<Guid>>(),
                externalId,
                user.Name,
                user.SkillLevel.ToString(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new ClaimGuestParticipationsCommand(externalId, null, email);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ClaimedParticipations.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WithNoGuestParticipations_ShouldReturnZero()
    {
        // Arrange
        var externalId = "auth0|789";
        var user = User.CreateFromExternalAuth(externalId, "Bob", SkillLevel.Beginner);

        _userRepositoryMock
            .Setup(x => x.GetNonGuestByExternalIdAsync(externalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _gamesServiceClientMock
            .Setup(x => x.GetGuestParticipationsByContactAsync(
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GuestParticipationDto>());

        var command = new ClaimGuestParticipationsCommand(externalId, "+33600000000", null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ClaimedParticipations.Should().Be(0);
        result.Value.ClaimedGames.Should().BeEmpty();

        _gamesServiceClientMock.Verify(
            x => x.ConvertGuestParticipationsAsync(
                It.IsAny<List<Guid>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithoutExternalId_ShouldReturnInvalid()
    {
        // Arrange
        var command = new ClaimGuestParticipationsCommand("", "+33612345678", null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Status.Should().Be(ResultStatus.Invalid);
        result.ValidationErrors.Should().Contain(e => e.ErrorMessage.Contains("ExternalId"));
    }

    [Fact]
    public async Task Handle_WithoutContactInfo_ShouldReturnInvalid()
    {
        // Arrange
        var command = new ClaimGuestParticipationsCommand("auth0|123", null, null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Status.Should().Be(ResultStatus.Invalid);
        result.ValidationErrors.Should().Contain(e => 
            e.ErrorMessage.Contains("phone") || e.ErrorMessage.Contains("email"));
    }

    [Fact]
    public async Task Handle_WithNonExistentUser_ShouldReturnNotFound()
    {
        // Arrange
        var externalId = "auth0|notfound";

        _userRepositoryMock
            .Setup(x => x.GetNonGuestByExternalIdAsync(externalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var command = new ClaimGuestParticipationsCommand(externalId, "+33612345678", null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Status.Should().Be(ResultStatus.NotFound);
        result.Errors.Should().Contain(e => e.Contains("User not found"));
    }

    [Fact]
    public async Task Handle_WithBothPhoneAndEmail_ShouldPassBothToGamesService()
    {
        // Arrange
        var externalId = "auth0|multi";
        var phoneNumber = "+33612345678";
        var email = "test@example.com";
        var user = User.CreateFromExternalAuth(externalId, "Test User", SkillLevel.Intermediate);

        _userRepositoryMock
            .Setup(x => x.GetNonGuestByExternalIdAsync(externalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _gamesServiceClientMock
            .Setup(x => x.GetGuestParticipationsByContactAsync(phoneNumber, email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GuestParticipationDto>());

        var command = new ClaimGuestParticipationsCommand(externalId, phoneNumber, email);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        _gamesServiceClientMock.Verify(
            x => x.GetGuestParticipationsByContactAsync(phoneNumber, email, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
