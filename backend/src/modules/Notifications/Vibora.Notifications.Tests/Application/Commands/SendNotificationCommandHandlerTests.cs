using Ardalis.Result;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Vibora.Notifications.Application;
using Vibora.Notifications.Application.Commands.SendNotification;
using Vibora.Notifications.Domain;
using Vibora.Notifications.Infrastructure.Channels;
using Vibora.Notifications.Infrastructure.Services;

namespace Vibora.Notifications.Tests.Application.Commands;

/// <summary>
/// Unit tests for SendNotificationCommandHandler
/// Tests the complete notification sending flow: Create -> Persist -> Dispatch -> Update Status
/// </summary>
public class SendNotificationCommandHandlerTests
{
    private readonly INotificationRepository _mockRepository;
    private readonly INotificationDispatcher _mockDispatcher;
    private readonly IUnitOfWork _mockUnitOfWork;
    private readonly ILogger<SendNotificationCommandHandler> _logger;
    private readonly SendNotificationCommandHandler _handler;

    public SendNotificationCommandHandlerTests()
    {
        _mockRepository = Substitute.For<INotificationRepository>();
        _mockDispatcher = Substitute.For<INotificationDispatcher>();
        _mockUnitOfWork = Substitute.For<IUnitOfWork>();
        _logger = Substitute.For<ILogger<SendNotificationCommandHandler>>();

        _handler = new SendNotificationCommandHandler(
            _mockRepository,
            _mockDispatcher,
            _mockUnitOfWork,
            _logger);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldCreateAndSendNotification()
    {
        // Arrange
        var content = new NotificationContent("Test Title", "Test Body");
        var command = new SendNotificationCommand(
            "user123",
            NotificationType.GameCancelled,
            NotificationChannel.Push,
            "device-token-xyz",
            content);

        _mockDispatcher.DispatchAsync(
                Arg.Any<Notification>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        await _mockRepository.Received(1).AddAsync(
            Arg.Any<Notification>(),
            Arg.Any<CancellationToken>());

        await _mockUnitOfWork.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>());

        await _mockDispatcher.Received(1).DispatchAsync(
            Arg.Is<Notification>(n =>
                n.UserId == "user123" &&
                n.Type == NotificationType.GameCancelled &&
                n.Channel == NotificationChannel.Push &&
                n.Content.Title == "Test Title"),
            "device-token-xyz",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenDispatchFails_ShouldMarkNotificationAsFailed()
    {
        // Arrange
        var content = new NotificationContent("Test Title", "Test Body");
        var command = new SendNotificationCommand(
            "user123",
            NotificationType.PlayerJoined,
            NotificationChannel.Push,
            "device-token-xyz",
            content);

        _mockDispatcher.DispatchAsync(
                Arg.Any<Notification>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Error("FCM service unavailable"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue(); // Handler should return success even if dispatch fails
        result.Value.Should().NotBeEmpty();

        await _mockRepository.Received(1).AddAsync(
            Arg.Any<Notification>(),
            Arg.Any<CancellationToken>());

        // Should save twice: once after creation, once after marking as failed
        await _mockUnitOfWork.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithInvalidUserId_ShouldReturnInvalidResult()
    {
        // Arrange
        var content = new NotificationContent("Test Title", "Test Body");
        var command = new SendNotificationCommand(
            "", // Invalid: empty userId
            NotificationType.GameCancelled,
            NotificationChannel.Push,
            "device-token-xyz",
            content);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().NotBeEmpty();
        result.ValidationErrors.Should().Contain(e => e.Identifier == "userId");

        await _mockRepository.DidNotReceive().AddAsync(
            Arg.Any<Notification>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithDifferentChannels_ShouldUseCorrectChannel()
    {
        // Arrange
        var content = new NotificationContent("Test", "Body");
        var emailCommand = new SendNotificationCommand(
            "user123",
            NotificationType.GameCancelled,
            NotificationChannel.Email,
            "user@example.com",
            content);

        _mockDispatcher.DispatchAsync(
                Arg.Any<Notification>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        var result = await _handler.Handle(emailCommand, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        await _mockDispatcher.Received(1).DispatchAsync(
            Arg.Is<Notification>(n => n.Channel == NotificationChannel.Email),
            "user@example.com",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithMultipleNotifications_ShouldHandleEachIndependently()
    {
        // Arrange
        var content1 = new NotificationContent("Title 1", "Body 1");
        var command1 = new SendNotificationCommand(
            "user1",
            NotificationType.GameCancelled,
            NotificationChannel.Push,
            "token1",
            content1);

        var content2 = new NotificationContent("Title 2", "Body 2");
        var command2 = new SendNotificationCommand(
            "user2",
            NotificationType.PlayerJoined,
            NotificationChannel.Email,
            "user2@example.com",
            content2);

        _mockDispatcher.DispatchAsync(
                Arg.Any<Notification>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        var result1 = await _handler.Handle(command1, CancellationToken.None);
        var result2 = await _handler.Handle(command2, CancellationToken.None);

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();
        result1.Value.Should().NotBe(result2.Value); // Different notification IDs

        await _mockRepository.Received(2).AddAsync(
            Arg.Any<Notification>(),
            Arg.Any<CancellationToken>());

        await _mockDispatcher.Received(2).DispatchAsync(
            Arg.Any<Notification>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }
}
