using Vibora.Shared.Domain;

namespace Vibora.Games.Domain.Events;

/// <summary>
/// Domain event raised when a game is canceled by the host
/// This is the internal domain event (not the integration event published via MassTransit)
/// </summary>
internal sealed class GameCanceledDomainEvent : IDomainEvent
{
    public Guid GameId { get; }
    public string HostExternalId { get; }
    public DateTime GameDateTime { get; }
    public string Location { get; }
    public int TotalParticipants { get; }
    public DateTime OccurredOn { get; }
    public IReadOnlyCollection<Participation> Participants { get; }
    public IReadOnlyCollection<GuestParticipant> GuestParticipants { get; }

    public GameCanceledDomainEvent(
        Guid gameId,
        string hostExternalId,
        DateTime gameDateTime,
        string location,
        int totalParticipants,
        IReadOnlyCollection<Participation> participants,
        IReadOnlyCollection<GuestParticipant> guestParticipants)
    {
        GameId = gameId;
        HostExternalId = hostExternalId;
        GameDateTime = gameDateTime;
        Location = location;
        TotalParticipants = totalParticipants;
        Participants = participants;
        GuestParticipants = guestParticipants;
        OccurredOn = DateTime.UtcNow;
    }
}
