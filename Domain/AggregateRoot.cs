#nullable enable

using NovinCRM.Domain.Events;

namespace NovinCRM.Domain;

/// <summary>
/// Base class for DDD aggregate roots.
///
/// Aggregates collect domain events during a business operation.
/// The Application layer is responsible for dispatching those events
/// via <see cref="NovinCRM.Application.Common.Interfaces.IDomainEventDispatcher"/>
/// after the aggregate's state has been persisted.
///
/// Design note — why a class, not a record:
///   The existing sealed-record entities (Contact, Deal, …) are used as
///   immutable read-model types throughout the infrastructure layer and
///   remain untouched. Aggregate roots are a parallel concept used only
///   when business operations need to raise domain events. They are
///   constructed by Application-layer services, not by the HubSpot mappers.
/// </summary>
public abstract class AggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = new();

    /// <summary>
    /// Read-only snapshot of uncommitted domain events.
    /// Cleared after dispatch by <see cref="ClearDomainEvents"/>.
    /// </summary>
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Records a domain event to be dispatched after the aggregate is saved.
    /// Call from within aggregate methods — never from outside.
    /// </summary>
    protected void RaiseDomainEvent(IDomainEvent domainEvent)
        => _domainEvents.Add(domainEvent);

    /// <summary>
    /// Clears all recorded domain events.
    /// Called by the dispatcher after successful dispatch.
    /// </summary>
    public void ClearDomainEvents() => _domainEvents.Clear();
}

/// <summary>
/// Strongly-typed aggregate root with a typed identifier.
/// </summary>
public abstract class AggregateRoot<TId> : AggregateRoot
    where TId : notnull
{
    public TId Id { get; protected init; } = default!;
}
