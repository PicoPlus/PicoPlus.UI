#nullable enable

using PicoPlus.Domain;
using PicoPlus.Domain.Events;

namespace PicoPlus.Application.Common.Interfaces;

/// <summary>
/// Dispatches all domain events collected on an aggregate root.
///
/// Called by Application-layer services immediately after an aggregate
/// operation succeeds. Implementations use MediatR to publish each event
/// in-process to all registered <see cref="MediatR.INotificationHandler{TNotification}"/>s.
///
/// Usage:
/// <code>
///   var deal = await _dealRepo.CreateAsync(…);
///   aggregate.RaiseDomainEvent(new DealCreatedEvent { … });
///   await _dispatcher.DispatchAsync(aggregate, ct);
/// </code>
/// </summary>
public interface IDomainEventDispatcher
{
    /// <summary>
    /// Dispatches all uncommitted domain events on the aggregate,
    /// then clears them.
    /// </summary>
    Task DispatchAsync(AggregateRoot aggregate, CancellationToken ct = default);

    /// <summary>
    /// Dispatches a single domain event directly without an aggregate.
    /// Use when the event is constructed ad-hoc (e.g. from a service that
    /// does not yet have a full aggregate model).
    /// </summary>
    Task DispatchAsync(IDomainEvent domainEvent, CancellationToken ct = default);
}
