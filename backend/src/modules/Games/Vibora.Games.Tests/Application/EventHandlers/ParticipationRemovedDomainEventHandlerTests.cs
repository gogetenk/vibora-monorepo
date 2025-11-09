using FluentAssertions;
using MassTransit;
using Moq;
using Vibora.Games.Application.EventHandlers;
using Vibora.Games.Contracts.Events;
using Vibora.Games.Domain;
using Vibora.Games.Domain.Events;

namespace Vibora.Games.Tests.Application.EventHandlers;

public class ParticipationRemovedDomainEventHandlerTests
{
    private readonly Mock<IPublishEndpoint> _publishEndpointMock;
    private readonly ParticipationRemovedDomainEventHandler _handler;

    public ParticipationRemovedDomainEventHandlerTests()
    {
        _publishEndpointMock = new Mock<IPublishEndpoint>();
        _handler = new ParticipationRemovedDomainEventHandler(_publishEndpointMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldPublishIntegrationEvent()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var userExternalId = "auth0|player-123";
        var userName = "John Doe";
        var remainingPlayers = 2;
        var gameStatus = GameStatus.Open;

        var domainEvent = new ParticipationRemovedDomainEvent(
            gameId,
            userExternalId,
            userName,
            remainingPlayers,
            gameStatus
        );

        // Act
        await _handler.Handle(domainEvent, CancellationToken.None);

        // Assert
        _publishEndpointMock.Verify(
            x => x.Publish(
                It.Is<ParticipationRemovedEvent>(e =>
                    e.GameId == gameId &&
                    e.UserExternalId == userExternalId &&
                    e.UserName == userName &&
                    e.RemainingPlayers == remainingPlayers &&
                    e.GameStatus == "Open" &&
                    e.RemovedAt == domainEvent.OccurredOn
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
        var domainEvent = new ParticipationRemovedDomainEvent(
            gameId,
            "auth0|user",
            "Test User",
            3,
            GameStatus.Full
        );

        ParticipationRemovedEvent? capturedEvent = null;
        _publishEndpointMock
            .Setup(x => x.Publish(It.IsAny<ParticipationRemovedEvent>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((evt, ct) => capturedEvent = evt as ParticipationRemovedEvent);

        // Act
        await _handler.Handle(domainEvent, CancellationToken.None);

        // Assert
        capturedEvent.Should().NotBeNull();
        capturedEvent!.GameId.Should().Be(gameId);
        capturedEvent.UserExternalId.Should().Be("auth0|user");
        capturedEvent.UserName.Should().Be("Test User");
        capturedEvent.RemainingPlayers.Should().Be(3);
        capturedEvent.GameStatus.Should().Be("Full");
        capturedEvent.RemovedAt.Should().Be(domainEvent.OccurredOn);
    }
}
