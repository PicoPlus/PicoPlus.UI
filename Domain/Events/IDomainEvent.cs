#nullable enable

namespace NovinCRM.Domain.Events;

/// <summary>
/// Marker interface for domain events.
///
/// A domain event is something that happened within the domain boundary that
/// other parts of the same bounded context may react to.
/// Domain events are dispatched in-process via MediatR immediately after
/// the aggregate operation that raised them.
///
/// Rules:
///   • Immutable — use init-only properties only.
///   • Named in past tense — something that already happened.
///   • No infrastructure dependencies — pure data bags.
///   • Handled by <see cref="MediatR.INotificationHandler{TNotification}"/> implementations
///     registered in the Application layer.
/// </summary>
public interface IDomainEvent : MediatR.INotification
{
    /// <summary>Unique identifier for this event occurrence.</summary>
    Guid EventId { get; }

    /// <summary>UTC time when the event occurred.</summary>
    DateTimeOffset OccurredAt { get; }
}
