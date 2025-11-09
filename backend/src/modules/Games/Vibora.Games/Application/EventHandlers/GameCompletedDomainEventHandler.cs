using MassTransit;
using MediatR;
using Vibora.Games.Contracts.Events;
using Vibora.Games.Domain.Events;

namespace Vibora.Games.Application.EventHandlers;

/// <summary>
/// Handler for GameCompletedDomainEvent
/// Transforms domain event into integration event and publishes it via MassTransit
/// Other modules can consume this to notify the host
/// </summary>
internal sealed class GameCompletedDomainEventHandler : INotificationHandler<GameCompletedDomainEvent>
{
    private readonly IPublishEndpoint _publishEndpoint;

    public GameCompletedDomainEventHandler(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    public async Task Handle(GameCompletedDomainEvent notification, CancellationToken cancellationToken)
    {
        // Transform domain event to integration event
        var integrationEvent = new GameCompletedEvent
        {
            GameId = notification.GameId,
            HostExternalId = notification.HostExternalId,
            GameDateTime = notification.GameDateTime,
            Location = notification.Location,
            MaxPlayers = notification.MaxPlayers,
            CompletedAt = notification.OccurredOn
        };

        // Publish integration event to message bus for other modules/services
        await _publishEndpoint.Publish(integrationEvent, cancellationToken);
    }
}
