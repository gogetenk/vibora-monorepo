using FluentAssertions;
using Vibora.Notifications.Domain;
using Vibora.Notifications.Domain.Events;
using Xunit;

namespace Vibora.Notifications.Tests.Domain;

public class NotificationTests
{
    [Fact]
    public void Create_WithValidParameters_ShouldReturnNotification()
    {
        // Arrange
        var userId = "auth0|123456";
        var type = NotificationType.GameCreated;
        var channel = NotificationChannel.Push;
        var content = new NotificationContent("Test Title", "Test Body");

        // Act
        var result = Notification.Create(userId, type, channel, content);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var notification = result.Value;
        notification.Should().NotBeNull();
        notification.NotificationId.Should().NotBeEmpty();
        notification.UserId.Should().Be(userId);
        notification.Type.Should().Be(type);
        notification.Channel.Should().Be(channel);
        notification.Content.Should().Be(content);
        notification.Status.Should().Be(NotificationStatus.Pending);
        notification.RetryCount.Should().Be(0);
        notification.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        notification.SentAt.Should().BeNull();
        notification.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void Create_WithEmptyUserId_ShouldReturnInvalid()
    {
        // Arrange
        var content = new NotificationContent("Title", "Body");

        // Act
        var result = Notification.Create("", NotificationType.GameCreated, NotificationChannel.Push, content);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().Contain(e => e.Identifier == "userId");
    }

    [Fact]
    public void Create_WithNullContent_ShouldReturnInvalid()
    {
        // Arrange & Act
        var result = Notification.Create("auth0|123", NotificationType.GameCreated, NotificationChannel.Push, null!);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().Contain(e => e.Identifier == "content");
    }

    [Fact]
    public void MarkAsSending_WhenPending_ShouldSucceed()
    {
        // Arrange
        var notification = CreateTestNotification();

        // Act
        var result = notification.MarkAsSending();

        // Assert
        result.IsSuccess.Should().BeTrue();
        notification.Status.Should().Be(NotificationStatus.Sending);
    }

    [Fact]
    public void MarkAsSending_WhenNotPending_ShouldFail()
    {
        // Arrange
        var notification = CreateTestNotification();
        notification.MarkAsSending();
        notification.MarkAsSent();

        // Act
        var result = notification.MarkAsSending();

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainMatch("*Cannot mark as sending*");
    }

    [Fact]
    public void MarkAsSent_WhenSending_ShouldSucceedAndRaiseDomainEvent()
    {
        // Arrange
        var notification = CreateTestNotification();
        notification.MarkAsSending();

        // Act
        var result = notification.MarkAsSent();

        // Assert
        result.IsSuccess.Should().BeTrue();
        notification.Status.Should().Be(NotificationStatus.Sent);
        notification.SentAt.Should().NotBeNull();
        notification.SentAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));

        // Verify domain event raised
        notification.DomainEvents.Should().HaveCount(1);
        notification.DomainEvents.First().Should().BeOfType<NotificationSentDomainEvent>();
        var domainEvent = notification.DomainEvents.First() as NotificationSentDomainEvent;
        domainEvent!.NotificationId.Should().Be(notification.NotificationId);
        domainEvent.UserId.Should().Be(notification.UserId);
        domainEvent.Type.Should().Be(notification.Type);
        domainEvent.Channel.Should().Be(notification.Channel);
    }

    [Fact]
    public void MarkAsSent_WhenNotSending_ShouldFail()
    {
        // Arrange
        var notification = CreateTestNotification();

        // Act
        var result = notification.MarkAsSent();

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainMatch("*Cannot mark as sent*");
        notification.Status.Should().Be(NotificationStatus.Pending);
    }

    [Fact]
    public void MarkAsFailed_WhenSending_ShouldRetryAndResetToPending()
    {
        // Arrange
        var notification = CreateTestNotification();
        notification.MarkAsSending();
        var errorMessage = "Network timeout";

        // Act
        var result = notification.MarkAsFailed(errorMessage);

        // Assert
        result.IsSuccess.Should().BeTrue();
        notification.Status.Should().Be(NotificationStatus.Pending); // Reset for retry
        notification.RetryCount.Should().Be(1);
        notification.ErrorMessage.Should().Be(errorMessage);
        notification.DomainEvents.Should().BeEmpty(); // No event yet, still can retry
    }

    [Fact]
    public void MarkAsFailed_AfterMaxRetries_ShouldMarkAsFailedAndRaiseDomainEvent()
    {
        // Arrange
        var notification = CreateTestNotification();

        // Simulate 3 failures (max retries)
        for (int i = 0; i < 3; i++)
        {
            notification.MarkAsSending();
            notification.MarkAsFailed($"Attempt {i + 1} failed");
        }

        // Act - 3rd failure should mark as failed permanently
        // Assert
        notification.Status.Should().Be(NotificationStatus.Failed);
        notification.RetryCount.Should().Be(3);

        // Verify domain event raised
        notification.DomainEvents.Should().HaveCount(1);
        notification.DomainEvents.First().Should().BeOfType<NotificationFailedDomainEvent>();
        var domainEvent = notification.DomainEvents.First() as NotificationFailedDomainEvent;
        domainEvent!.NotificationId.Should().Be(notification.NotificationId);
        domainEvent.RetryCount.Should().Be(3);
    }

    [Fact]
    public void MarkAsFailed_WhenNotSending_ShouldFail()
    {
        // Arrange
        var notification = CreateTestNotification();

        // Act
        var result = notification.MarkAsFailed("Error");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainMatch("*Cannot mark as failed*");
    }

    [Fact]
    public void Cancel_WhenPending_ShouldSucceed()
    {
        // Arrange
        var notification = CreateTestNotification();

        // Act
        var result = notification.Cancel();

        // Assert
        result.IsSuccess.Should().BeTrue();
        notification.Status.Should().Be(NotificationStatus.Cancelled);
    }

    [Fact]
    public void Cancel_WhenNotPending_ShouldFail()
    {
        // Arrange
        var notification = CreateTestNotification();
        notification.MarkAsSending();

        // Act
        var result = notification.Cancel();

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainMatch("*Cannot cancel*");
    }

    [Fact]
    public void CanRetry_WhenPendingAndUnderMaxRetries_ShouldReturnTrue()
    {
        // Arrange
        var notification = CreateTestNotification();

        // Act & Assert
        notification.CanRetry().Should().BeTrue();
    }

    [Fact]
    public void CanRetry_WhenAtMaxRetries_ShouldReturnFalse()
    {
        // Arrange
        var notification = CreateTestNotification();

        // Simulate 3 failures
        for (int i = 0; i < 3; i++)
        {
            notification.MarkAsSending();
            notification.MarkAsFailed($"Attempt {i + 1}");
        }

        // Act & Assert
        notification.CanRetry().Should().BeFalse();
        notification.Status.Should().Be(NotificationStatus.Failed);
    }

    [Fact]
    public void CanRetry_WhenSent_ShouldReturnFalse()
    {
        // Arrange
        var notification = CreateTestNotification();
        notification.MarkAsSending();
        notification.MarkAsSent();

        // Act & Assert
        notification.CanRetry().Should().BeFalse();
    }

    private static Notification CreateTestNotification()
    {
        var content = new NotificationContent("Test Title", "Test Body");
        var result = Notification.Create(
            "auth0|123456",
            NotificationType.GameCreated,
            NotificationChannel.Push,
            content
        );
        return result.Value;
    }
}
