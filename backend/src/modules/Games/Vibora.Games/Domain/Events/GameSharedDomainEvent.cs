using Vibora.Shared.Domain;

namespace Vibora.Games.Domain.Events;

/// <summary>
/// Domain event raised when a game share link is created
/// This is the internal domain event (not the integration event published via MassTransit)
/// </summary>
internal sealed class GameSharedDomainEvent : IDomainEvent
{
    public Guid GameId { get; }
    public Guid GameShareId { get; }
    public string SharedByUserExternalId { get; }
    public string ShareToken { get; }
    public DateTime OccurredOn { get; }

    public GameSharedDomainEvent(
        Guid gameId,
        Guid gameShareId,
        string sharedByUserExternalId,
        string shareToken)
    {
        GameId = gameId;
        GameShareId = gameShareId;
        SharedByUserExternalId = sharedByUserExternalId;
        ShareToken = shareToken;
        OccurredOn = DateTime.UtcNow;
    }
}
