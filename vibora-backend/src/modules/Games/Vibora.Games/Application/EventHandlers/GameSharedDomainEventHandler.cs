using MassTransit;
using MediatR;
using Vibora.Games.Contracts.Events;
using Vibora.Games.Domain.Events;

namespace Vibora.Games.Application.EventHandlers;

/// <summary>
/// Handler for GameSharedDomainEvent
/// Transforms domain event into integration event and publishes it via MassTransit
/// Other modules can consume this for analytics, tracking, etc.
/// </summary>
internal sealed class GameSharedDomainEventHandler : INotificationHandler<GameSharedDomainEvent>
{
    private readonly IPublishEndpoint _publishEndpoint;

    public GameSharedDomainEventHandler(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    public async Task Handle(GameSharedDomainEvent notification, CancellationToken cancellationToken)
    {
        // Transform domain event to integration event
        var integrationEvent = new GameSharedEvent
        {
            GameId = notification.GameId,
            GameShareId = notification.GameShareId,
            SharedByUserExternalId = notification.SharedByUserExternalId,
            ShareToken = notification.ShareToken,
            SharedAt = notification.OccurredOn
        };

        // Publish integration event to message bus for other modules/services
        await _publishEndpoint.Publish(integrationEvent, cancellationToken);
    }
}
