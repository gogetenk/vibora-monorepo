using FluentAssertions;
using MassTransit;
using Moq;
using Vibora.Games.Application.EventHandlers;
using Vibora.Games.Contracts.Events;
using Vibora.Games.Domain;
using Vibora.Games.Domain.Events;

namespace Vibora.Games.Tests.Application.EventHandlers;

public class GameCanceledDomainEventHandlerTests
{
    private readonly Mock<IPublishEndpoint> _publishEndpointMock;
    private readonly GameCanceledDomainEventHandler _handler;

    public GameCanceledDomainEventHandlerTests()
    {
        _publishEndpointMock = new Mock<IPublishEndpoint>();
        _handler = new GameCanceledDomainEventHandler(_publishEndpointMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldPublishIntegrationEvent()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var hostExternalId = "auth0|host";
        var gameDateTime = DateTime.UtcNow.AddDays(1);
        var location = "Test Club";
        var totalParticipants = 3;

        var participants = new List<Participation>
        {
            Participation.Create(gameId, hostExternalId, "Host", "Advanced", true),
            Participation.Create(gameId, "auth0|player1", "Player 1", "Intermediate", false)
        };
        var guestParticipants = new List<GuestParticipant>();

        var domainEvent = new GameCanceledDomainEvent(
            gameId,
            hostExternalId,
            gameDateTime,
            location,
            totalParticipants,
            participants,
            guestParticipants
        );

        // Act
        await _handler.Handle(domainEvent, CancellationToken.None);

        // Assert
        _publishEndpointMock.Verify(
            x => x.Publish(
                It.Is<GameCanceledEvent>(e =>
                    e.GameId == gameId &&
                    e.HostExternalId == hostExternalId &&
                    e.GameDateTime == gameDateTime &&
                    e.Location == location &&
                    e.TotalParticipants == totalParticipants &&
                    e.CanceledAt == domainEvent.OccurredOn &&
                    e.Participants.Count == 1 && // Excludes host
                    e.GuestParticipants.Count == 0
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
        var participants = new List<Participation>
        {
            Participation.Create(gameId, "auth0|host", "Host", "Advanced", true),
            Participation.Create(gameId, "auth0|player1", "Player 1", "Intermediate", false),
            Participation.Create(gameId, "auth0|player2", "Player 2", "Beginner", false)
        };
        var guestParticipants = new List<GuestParticipant>();

        var domainEvent = new GameCanceledDomainEvent(
            gameId,
            "auth0|host",
            DateTime.UtcNow.AddDays(2),
            "Tennis Club",
            4,
            participants,
            guestParticipants
        );

        GameCanceledEvent? capturedEvent = null;
        _publishEndpointMock
            .Setup(x => x.Publish(It.IsAny<GameCanceledEvent>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((evt, ct) => capturedEvent = evt as GameCanceledEvent);

        // Act
        await _handler.Handle(domainEvent, CancellationToken.None);

        // Assert
        capturedEvent.Should().NotBeNull();
        capturedEvent!.GameId.Should().Be(gameId);
        capturedEvent.HostExternalId.Should().Be("auth0|host");
        capturedEvent.Location.Should().Be("Tennis Club");
        capturedEvent.TotalParticipants.Should().Be(4);
        capturedEvent.CanceledAt.Should().Be(domainEvent.OccurredOn);
        capturedEvent.Participants.Should().HaveCount(2); // Excludes host
        capturedEvent.Participants[0].UserExternalId.Should().Be("auth0|player1");
        capturedEvent.Participants[1].UserExternalId.Should().Be("auth0|player2");
        capturedEvent.GuestParticipants.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldIncludeGuestParticipantsInIntegrationEvent()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var participants = new List<Participation>
        {
            Participation.Create(gameId, "auth0|host", "Host", "Advanced", true)
        };
        
        var guest1 = GuestParticipant.Create(gameId, "Guest John", "+33612345678", null).Value;
        var guest2 = GuestParticipant.Create(gameId, "Guest Jane", null, "jane@example.com").Value;
        var guestParticipants = new List<GuestParticipant> { guest1, guest2 };

        var domainEvent = new GameCanceledDomainEvent(
            gameId,
            "auth0|host",
            DateTime.UtcNow.AddDays(1),
            "Padel Club",
            3,
            participants,
            guestParticipants
        );

        GameCanceledEvent? capturedEvent = null;
        _publishEndpointMock
            .Setup(x => x.Publish(It.IsAny<GameCanceledEvent>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((evt, ct) => capturedEvent = evt as GameCanceledEvent);

        // Act
        await _handler.Handle(domainEvent, CancellationToken.None);

        // Assert
        capturedEvent.Should().NotBeNull();
        capturedEvent!.Participants.Should().BeEmpty(); // No registered participants except host
        capturedEvent.GuestParticipants.Should().HaveCount(2);
        capturedEvent.GuestParticipants[0].GuestName.Should().Be("Guest John");
        capturedEvent.GuestParticipants[0].PhoneNumber.Should().Be("+33612345678");
        capturedEvent.GuestParticipants[1].GuestName.Should().Be("Guest Jane");
        capturedEvent.GuestParticipants[1].Email.Should().Be("jane@example.com");
    }
}
