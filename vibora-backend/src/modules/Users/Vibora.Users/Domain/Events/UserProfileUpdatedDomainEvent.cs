using Vibora.Shared.Domain;

namespace Vibora.Users.Domain.Events;

/// <summary>
/// Domain event raised when a user profile is updated
/// </summary>
public sealed record UserProfileUpdatedDomainEvent(
    Guid UserId,
    string UserExternalId
) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
