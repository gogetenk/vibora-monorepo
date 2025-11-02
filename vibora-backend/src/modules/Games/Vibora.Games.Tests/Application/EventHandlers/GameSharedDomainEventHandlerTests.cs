using FluentAssertions;
using MassTransit;
using Moq;
using Vibora.Games.Application.EventHandlers;
using Vibora.Games.Contracts.Events;
using Vibora.Games.Domain.Events;

namespace Vibora.Games.Tests.Application.EventHandlers;

public class GameSharedDomainEventHandlerTests
{
    private readonly Mock<IPublishEndpoint> _publishEndpointMock;
    private readonly GameSharedDomainEventHandler _handler;

    public GameSharedDomainEventHandlerTests()
    {
        _publishEndpointMock = new Mock<IPublishEndpoint>();
        _handler = new GameSharedDomainEventHandler(_publishEndpointMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldPublishIntegrationEvent()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var gameShareId = Guid.NewGuid();
        var sharedByUserExternalId = "auth0|user123";
        var shareToken = "ABC12345";

        var domainEvent = new GameSharedDomainEvent(
            gameId,
            gameShareId,
            sharedByUserExternalId,
            shareToken
        );

        // Act
        await _handler.Handle(domainEvent, CancellationToken.None);

        // Assert
        _publishEndpointMock.Verify(
            x => x.Publish(
                It.Is<GameSharedEvent>(e =>
                    e.GameId == gameId &&
                    e.GameShareId == gameShareId &&
                    e.SharedByUserExternalId == sharedByUserExternalId &&
                    e.ShareToken == shareToken &&
                    e.SharedAt == domainEvent.OccurredOn
                ),
                It.IsAny<CancellationToken>()
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_ShouldTransformDomainEventToIntegrationEvent()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var domainEvent = new GameSharedDomainEvent(
            gameId,
            Guid.NewGuid(),
            "auth0|user456",
            "TOKEN123"
        );

        GameSharedEvent? capturedEvent = null;
        _publishEndpointMock
            .Setup(x => x.Publish(It.IsAny<GameSharedEvent>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((evt, ct) => capturedEvent = evt as GameSharedEvent);

        // Act
        await _handler.Handle(domainEvent, CancellationToken.None);

        // Assert
        capturedEvent.Should().NotBeNull();
        capturedEvent!.GameId.Should().Be(gameId);
        capturedEvent.SharedByUserExternalId.Should().Be("auth0|user456");
        capturedEvent.ShareToken.Should().Be("TOKEN123");
        capturedEvent.SharedAt.Should().Be(domainEvent.OccurredOn);
    }
}
