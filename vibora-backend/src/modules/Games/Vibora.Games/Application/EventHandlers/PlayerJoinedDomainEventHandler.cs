using MassTransit;
using MediatR;
using Vibora.Games.Contracts.Events;
using Vibora.Games.Domain.Events;

namespace Vibora.Games.Application.EventHandlers;

/// <summary>
/// Handler for PlayerJoinedDomainEvent
/// Transforms domain event into integration event and publishes it via MassTransit
/// Other modules can consume this to notify the host, update stats, etc.
/// </summary>
internal sealed class PlayerJoinedDomainEventHandler : INotificationHandler<PlayerJoinedDomainEvent>
{
    private readonly IPublishEndpoint _publishEndpoint;

    public PlayerJoinedDomainEventHandler(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    public async Task Handle(PlayerJoinedDomainEvent notification, CancellationToken cancellationToken)
    {
        // Transform domain event to integration event
        var integrationEvent = new PlayerJoinedEvent
        {
            GameId = notification.GameId,
            ParticipationId = notification.ParticipationId,
            UserExternalId = notification.UserExternalId,
            UserName = notification.UserName,
            UserSkillLevel = notification.UserSkillLevel,
            HostExternalId = notification.HostExternalId,
            GameDateTime = notification.GameDateTime,
            Location = notification.Location,
            CurrentPlayers = notification.CurrentPlayers,
            MaxPlayers = notification.MaxPlayers,
            JoinedAt = notification.OccurredOn
        };

        // Publish integration event to message bus for other modules/services
        await _publishEndpoint.Publish(integrationEvent, cancellationToken);
    }
}
