using MassTransit;
using MediatR;
using Vibora.Games.Contracts.Events;
using Vibora.Games.Domain.Events;

namespace Vibora.Games.Application.EventHandlers;

/// <summary>
/// Handler for GameCreatedDomainEvent
/// Transforms domain event into integration event and publishes it via MassTransit
/// </summary>
internal sealed class GameCreatedDomainEventHandler : INotificationHandler<GameCreatedDomainEvent>
{
    private readonly IPublishEndpoint _publishEndpoint;

    public GameCreatedDomainEventHandler(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    public async Task Handle(GameCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        // Transform domain event to integration event
        var integrationEvent = new GameCreatedEvent
        {
            GameId = notification.GameId,
            HostExternalId = notification.HostExternalId,
            DateTime = notification.GameDateTime,
            Location = notification.Location,
            SkillLevel = notification.SkillLevel,
            MaxPlayers = notification.MaxPlayers,
            CreatedAt = notification.OccurredOn
        };

        // Publish integration event to message bus for other modules/services
        await _publishEndpoint.Publish(integrationEvent, cancellationToken);
    }
}
