using Ardalis.Result;
using AutoFixture;
using AutoFixture.AutoMoq;
using FluentAssertions;
using Microsoft.AspNetCore.OutputCaching;
using Moq;
using Vibora.Games.Application;
using Vibora.Games.Application.Commands.CreateGame;
using Vibora.Games.Domain;
using Vibora.Users.Contracts.Services;

namespace Vibora.Games.Tests.Application.Commands;

public class CreateGameCommandHandlerTests
{
    private readonly IFixture _fixture;
    private readonly Mock<IGameRepository> _mockGameRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IUsersServiceClient> _mockUsersClient;
    private readonly Mock<IOutputCacheStore> _mockCacheStore;
    private readonly CreateGameCommandHandler _handler;

    public CreateGameCommandHandlerTests()
    {
        _fixture = new Fixture();
        _fixture.Customize(new AutoMoqCustomization { ConfigureMembers = true });

        _mockGameRepository = new Mock<IGameRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockUsersClient = new Mock<IUsersServiceClient>();
        _mockCacheStore = new Mock<IOutputCacheStore>();

        _handler = new CreateGameCommandHandler(
            _mockGameRepository.Object,
            _mockUnitOfWork.Object,
            _mockUsersClient.Object,
            _mockCacheStore.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldReturnSuccessResult()
    {
        // Arrange
        var command = new CreateGameCommand(
            HostExternalId: "auth0|123456",
            DateTime: DateTime.UtcNow.AddDays(1),
            Location: "Club de Padel",
            SkillLevel: "Intermediate",
            MaxPlayers: 4);

        var hostMetadata = new UserMetadataDto(
            "auth0|123456",
            "John Doe",
            9);

        _mockUsersClient
            .Setup(x => x.GetUserMetadataAsync(
                command.HostExternalId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserMetadataDto>.Success(hostMetadata));

        _mockUnitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.HostExternalId.Should().Be(command.HostExternalId);
        result.Value.Location.Should().Be(command.Location);
        result.Value.CurrentPlayers.Should().Be(1); // Host auto-joined
        result.Value.Participants.Should().HaveCount(1);
        
        var hostParticipant = result.Value.Participants.First();
        hostParticipant.ExternalId.Should().Be(hostMetadata.ExternalId);
        hostParticipant.Name.Should().Be(hostMetadata.Name);
        hostParticipant.SkillLevel.Should().Be(hostMetadata.SkillLevel.ToString());
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldQueryUsersService()
    {
        // Arrange
        var command = new CreateGameCommand(
            HostExternalId: "auth0|123456",
            DateTime: DateTime.UtcNow.AddDays(1),
            Location: "Club",
            SkillLevel: "Intermediate",
            MaxPlayers: 4);

        var hostMetadata = new UserMetadataDto("auth0|123456", "John", 9);

        _mockUsersClient
            .Setup(x => x.GetUserMetadataAsync(
                command.HostExternalId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserMetadataDto>.Success(hostMetadata));

        _mockUnitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockUsersClient.Verify(
            x => x.GetUserMetadataAsync(command.HostExternalId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenHostNotFound_ShouldReturnValidationError()
    {
        // Arrange
        var command = new CreateGameCommand(
            HostExternalId: "auth0|nonexistent",
            DateTime: DateTime.UtcNow.AddDays(1),
            Location: "Club",
            SkillLevel: "Intermediate",
            MaxPlayers: 4);

        _mockUsersClient
            .Setup(x => x.GetUserMetadataAsync(
                command.HostExternalId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserMetadataDto>.NotFound("User not found"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().Contain(e => 
            e.ErrorMessage.Contains("not found", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldCallUnitOfWorkSaveChanges()
    {
        // Arrange
        var command = new CreateGameCommand(
            HostExternalId: "auth0|123456",
            DateTime: DateTime.UtcNow.AddDays(1),
            Location: "Club",
            SkillLevel: "Intermediate",
            MaxPlayers: 4);

        var hostMetadata = new UserMetadataDto("auth0|123456", "John", 9);

        _mockUsersClient
            .Setup(x => x.GetUserMetadataAsync(
                command.HostExternalId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserMetadataDto>.Success(hostMetadata));

        _mockUnitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockUnitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithInvalidGameData_ShouldReturnDomainValidationErrors()
    {
        // Arrange
        var command = new CreateGameCommand(
            HostExternalId: "auth0|123456",
            DateTime: DateTime.UtcNow.AddHours(-1), // Past date - invalid
            Location: "", // Empty location - invalid
            SkillLevel: "Intermediate",
            MaxPlayers: 20); // Too many - invalid

        var hostMetadata = new UserMetadataDto("auth0|123456", "John", 9);

        _mockUsersClient
            .Setup(x => x.GetUserMetadataAsync(
                command.HostExternalId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserMetadataDto>.Success(hostMetadata));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().HaveCountGreaterThanOrEqualTo(3);
        result.ValidationErrors.Should().Contain(e => e.ErrorMessage.Contains("future"));
        result.ValidationErrors.Should().Contain(e => e.ErrorMessage.Contains("Location"));
        result.ValidationErrors.Should().Contain(e => e.ErrorMessage.Contains("between 2 and 10"));
    }

    [Fact]
    public async Task Handle_WithInvalidGameData_ShouldNotCallUnitOfWork()
    {
        // Arrange
        var command = new CreateGameCommand(
            HostExternalId: "auth0|123456",
            DateTime: DateTime.UtcNow.AddHours(-1), // Invalid
            Location: "Club",
            SkillLevel: "Intermediate",
            MaxPlayers: 4);

        var hostMetadata = new UserMetadataDto("auth0|123456", "John", 9);

        _mockUsersClient
            .Setup(x => x.GetUserMetadataAsync(
                command.HostExternalId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserMetadataDto>.Success(hostMetadata));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockUnitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldReturnGameWithCorrectParticipants()
    {
        // Arrange
        var command = new CreateGameCommand(
            HostExternalId: "auth0|123456",
            DateTime: DateTime.UtcNow.AddDays(1),
            Location: "Club",
            SkillLevel: "Intermediate",
            MaxPlayers: 4);

        var hostMetadata = new UserMetadataDto(
            "auth0|123456",
            "John Doe",
            9);

        _mockUsersClient
            .Setup(x => x.GetUserMetadataAsync(
                command.HostExternalId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserMetadataDto>.Success(hostMetadata));

        _mockUnitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        
        var participants = result.Value.Participants;
        participants.Should().HaveCount(1);
        
        var hostParticipant = participants.First();
        hostParticipant.ExternalId.Should().Be(hostMetadata.ExternalId);
        hostParticipant.Name.Should().Be(hostMetadata.Name);
        hostParticipant.SkillLevel.Should().Be(hostMetadata.SkillLevel.ToString());
    }

    [Fact]
    public async Task Handle_WhenCancellationRequested_ShouldPassCancellationToken()
    {
        // Arrange
        var command = new CreateGameCommand(
            HostExternalId: "auth0|123456",
            DateTime: DateTime.UtcNow.AddDays(1),
            Location: "Club",
            SkillLevel: "Intermediate",
            MaxPlayers: 4);

        var cancellationToken = new CancellationToken(canceled: true);

        _mockUsersClient
            .Setup(x => x.GetUserMetadataAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _handler.Handle(command, cancellationToken));
    }
}
