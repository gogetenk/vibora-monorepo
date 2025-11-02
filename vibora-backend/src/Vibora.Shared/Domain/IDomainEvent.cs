using MediatR;

namespace Vibora.Shared.Domain;

/// <summary>
/// Marker interface for all domain events across modules
/// Domain events represent something that happened in the domain that domain experts care about
/// Implements INotification so they can be published via MediatR
/// </summary>
public interface IDomainEvent : INotification
{
    /// <summary>
    /// When the domain event occurred
    /// </summary>
    DateTime OccurredOn { get; }
}
