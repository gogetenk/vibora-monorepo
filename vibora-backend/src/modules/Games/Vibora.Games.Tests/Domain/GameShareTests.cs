using FluentAssertions;
using Vibora.Games.Domain;
using Vibora.Games.Domain.Events;

namespace Vibora.Games.Tests.Domain;

public class GameShareTests
{
    [Fact]
    public void Create_WithValidParameters_ShouldSucceed()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var sharedByUserExternalId = "auth0|user123";

        // Act
        var result = GameShare.Create(gameId, sharedByUserExternalId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.GameId.Should().Be(gameId);
        result.Value.SharedByUserExternalId.Should().Be(sharedByUserExternalId);
        result.Value.ShareToken.Should().NotBeNullOrEmpty();
        result.Value.ShareToken.Length.Should().Be(8);
        result.Value.ViewCount.Should().Be(0);
        result.Value.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        result.Value.ExpiresAt.Should().BeNull();
    }

    [Fact]
    public void Create_WithExpirationDate_ShouldSetExpiresAt()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var sharedByUserExternalId = "auth0|user123";
        var expiresAt = DateTime.UtcNow.AddDays(7);

        // Act
        var result = GameShare.Create(gameId, sharedByUserExternalId, expiresAt);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ExpiresAt.Should().Be(expiresAt);
    }

    [Fact]
    public void Create_WithEmptyGameId_ShouldReturnError()
    {
        // Arrange
        var gameId = Guid.Empty;
        var sharedByUserExternalId = "auth0|user123";

        // Act
        var result = GameShare.Create(gameId, sharedByUserExternalId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().Contain(e => e.ErrorMessage.Contains("GameId cannot be empty"));
    }

    [Fact]
    public void Create_WithNullSharedByUserExternalId_ShouldReturnError()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        string sharedByUserExternalId = null!;

        // Act
        var result = GameShare.Create(gameId, sharedByUserExternalId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().Contain(e => e.ErrorMessage.Contains("SharedByUserExternalId is required"));
    }

    [Fact]
    public void Create_WithEmptySharedByUserExternalId_ShouldReturnError()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var sharedByUserExternalId = "";

        // Act
        var result = GameShare.Create(gameId, sharedByUserExternalId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().Contain(e => e.ErrorMessage.Contains("SharedByUserExternalId is required"));
    }

    [Fact]
    public void Create_WithPastExpirationDate_ShouldReturnError()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var sharedByUserExternalId = "auth0|user123";
        var pastExpiresAt = DateTime.UtcNow.AddDays(-1);

        // Act
        var result = GameShare.Create(gameId, sharedByUserExternalId, pastExpiresAt);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().Contain(e => e.ErrorMessage.Contains("ExpiresAt must be in the future"));
    }

    [Fact]
    public void Create_ShouldRaiseDomainEvent()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var sharedByUserExternalId = "auth0|user123";

        // Act
        var result = GameShare.Create(gameId, sharedByUserExternalId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.DomainEvents.Should().ContainSingle();
        var domainEvent = result.Value.DomainEvents.Single() as GameSharedDomainEvent;
        domainEvent.Should().NotBeNull();
        domainEvent!.GameId.Should().Be(gameId);
        domainEvent.GameShareId.Should().Be(result.Value.Id);
        domainEvent.SharedByUserExternalId.Should().Be(sharedByUserExternalId);
        domainEvent.ShareToken.Should().Be(result.Value.ShareToken);
    }

    [Fact]
    public void IncrementViewCount_WhenNotExpired_ShouldSucceed()
    {
        // Arrange
        var gameShare = GameShare.Create(Guid.NewGuid(), "auth0|user123").Value;
        var initialViewCount = gameShare.ViewCount;

        // Act
        var result = gameShare.IncrementViewCount();

        // Assert
        result.IsSuccess.Should().BeTrue();
        gameShare.ViewCount.Should().Be(initialViewCount + 1);
    }

    [Fact]
    public void IncrementViewCount_WhenExpired_ShouldReturnError()
    {
        // Arrange - Create a valid share, then manually set expiration to past (for testing)
        var gameShare = GameShare.Create(Guid.NewGuid(), "auth0|user123", DateTime.UtcNow.AddDays(1)).Value;
        var expiresAtField = typeof(GameShare).GetField("<ExpiresAt>k__BackingField", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        expiresAtField!.SetValue(gameShare, DateTime.UtcNow.AddDays(-1));

        // Act
        var result = gameShare.IncrementViewCount();

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("expired"));
    }

    [Fact]
    public void IsExpired_WhenNoExpirationDate_ShouldReturnFalse()
    {
        // Arrange
        var gameShare = GameShare.Create(Guid.NewGuid(), "auth0|user123").Value;

        // Act
        var isExpired = gameShare.IsExpired();

        // Assert
        isExpired.Should().BeFalse();
    }

    [Fact]
    public void IsExpired_WhenExpirationDateInFuture_ShouldReturnFalse()
    {
        // Arrange
        var expiresAt = DateTime.UtcNow.AddDays(7);
        var gameShare = GameShare.Create(Guid.NewGuid(), "auth0|user123", expiresAt).Value;

        // Act
        var isExpired = gameShare.IsExpired();

        // Assert
        isExpired.Should().BeFalse();
    }

    [Fact]
    public void IsExpired_WhenExpirationDateInPast_ShouldReturnTrue()
    {
        // Arrange - Create a valid share, then manually set expiration to past (for testing)
        var gameShare = GameShare.Create(Guid.NewGuid(), "auth0|user123", DateTime.UtcNow.AddDays(1)).Value;
        var expiresAtField = typeof(GameShare).GetField("<ExpiresAt>k__BackingField", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        expiresAtField!.SetValue(gameShare, DateTime.UtcNow.AddDays(-1));

        // Act
        var isExpired = gameShare.IsExpired();

        // Assert
        isExpired.Should().BeTrue();
    }

    [Fact]
    public void Create_ShouldGenerateUniqueTokens()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var sharedByUserExternalId = "auth0|user123";

        // Act - Create multiple shares
        var share1 = GameShare.Create(gameId, sharedByUserExternalId).Value;
        var share2 = GameShare.Create(gameId, sharedByUserExternalId).Value;
        var share3 = GameShare.Create(gameId, sharedByUserExternalId).Value;

        // Assert - All tokens should be different
        share1.ShareToken.Should().NotBe(share2.ShareToken);
        share1.ShareToken.Should().NotBe(share3.ShareToken);
        share2.ShareToken.Should().NotBe(share3.ShareToken);
    }
}
