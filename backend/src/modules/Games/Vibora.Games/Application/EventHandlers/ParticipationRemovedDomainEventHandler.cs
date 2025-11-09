using MassTransit;
using MediatR;
using Vibora.Games.Contracts.Events;
using Vibora.Games.Domain.Events;

namespace Vibora.Games.Application.EventHandlers;

/// <summary>
/// Handler for ParticipationRemovedDomainEvent
/// Transforms domain event into integration event and publishes it via MassTransit
/// Other modules can consume this to notify remaining players, update stats, etc.
/// </summary>
internal sealed class ParticipationRemovedDomainEventHandler : INotificationHandler<ParticipationRemovedDomainEvent>
{
    private readonly IPublishEndpoint _publishEndpoint;

    public ParticipationRemovedDomainEventHandler(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    public async Task Handle(ParticipationRemovedDomainEvent notification, CancellationToken cancellationToken)
    {
        // Transform domain event to integration event
        var integrationEvent = new ParticipationRemovedEvent
        {
            GameId = notification.GameId,
            UserExternalId = notification.UserExternalId,
            UserName = notification.UserName,
            RemainingPlayers = notification.RemainingPlayers,
            GameStatus = notification.GameStatus.ToString(),
            RemovedAt = notification.OccurredOn
        };

        // Publish integration event to message bus for other modules/services
        await _publishEndpoint.Publish(integrationEvent, cancellationToken);
    }
}
