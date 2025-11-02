using MassTransit;
using MediatR;
using Vibora.Games.Contracts.Events;
using Vibora.Games.Domain.Events;

namespace Vibora.Games.Application.EventHandlers;

/// <summary>
/// Handler for GameCanceledDomainEvent
/// Transforms domain event into integration event and publishes it via MassTransit
/// Other modules can consume this to notify participants, update stats, etc.
/// </summary>
internal sealed class GameCanceledDomainEventHandler : INotificationHandler<GameCanceledDomainEvent>
{
    private readonly IPublishEndpoint _publishEndpoint;

    public GameCanceledDomainEventHandler(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    public async Task Handle(GameCanceledDomainEvent notification, CancellationToken cancellationToken)
    {
        // Map registered participants (excluding host)
        var participants = notification.Participants
            .Where(p => !p.IsHost)
            .Select(p => new ParticipantInfo
            {
                UserExternalId = p.UserExternalId,
                UserName = p.UserName,
                UserSkillLevel = p.UserSkillLevel,
                JoinedAt = p.JoinedAt
            })
            .ToList();

        // Map guest participants
        var guestParticipants = notification.GuestParticipants
            .Select(g => new GuestParticipantInfo
            {
                GuestId = g.Id,
                GuestName = g.Name,
                PhoneNumber = g.PhoneNumber,
                Email = g.Email,
                JoinedAt = g.JoinedAt
            })
            .ToList();

        // Transform domain event to integration event
        var integrationEvent = new GameCanceledEvent
        {
            GameId = notification.GameId,
            HostExternalId = notification.HostExternalId,
            GameDateTime = notification.GameDateTime,
            Location = notification.Location,
            TotalParticipants = notification.TotalParticipants,
            CanceledAt = notification.OccurredOn,
            Participants = participants,
            GuestParticipants = guestParticipants
        };

        // Publish integration event to message bus for other modules/services
        await _publishEndpoint.Publish(integrationEvent, cancellationToken);
    }
}
