#nullable enable

using MediatR;
using Microsoft.Extensions.Logging;
using PicoPlus.Application.Common.Interfaces;
using PicoPlus.Domain;
using PicoPlus.Domain.Events;

namespace PicoPlus.Infrastructure.Events;

/// <summary>
/// Dispatches domain events in-process via MediatR.
///
/// Each domain event is published as an <see cref="INotification"/>.
/// MediatR fans the notification out to every registered
/// <see cref="INotificationHandler{TNotification}"/> synchronously
/// (sequential by default; configure with a custom publisher for parallel).
///
/// The aggregate's event list is cleared after all events are dispatched
/// so the same events cannot be dispatched twice even if the service is
/// called again.
/// </summary>
public sealed class MediatRDomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IPublisher _publisher;
    private readonly ILogger<MediatRDomainEventDispatcher> _logger;

    public MediatRDomainEventDispatcher(
        IPublisher publisher,
        ILogger<MediatRDomainEventDispatcher> logger)
    {
        _publisher = publisher;
        _logger    = logger;
    }

    /// <inheritdoc />
    public async Task DispatchAsync(AggregateRoot aggregate, CancellationToken ct = default)
    {
        var events = aggregate.DomainEvents.ToList(); // snapshot before clear
        aggregate.ClearDomainEvents();

        foreach (var ev in events)
            await PublishOneAsync(ev, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DispatchAsync(IDomainEvent domainEvent, CancellationToken ct = default)
        => await PublishOneAsync(domainEvent, ct).ConfigureAwait(false);

    private async Task PublishOneAsync(IDomainEvent ev, CancellationToken ct)
    {
        _logger.LogDebug(
            "DomainEventDispatcher: publishing {EventType} (EventId={EventId})",
            ev.GetType().Name, ev.EventId);
        try
        {
            await _publisher.Publish(ev, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Swallow + log — domain event dispatch must never roll back the
            // triggering operation. Handlers are responsible for their own
            // error handling and retries.
            _logger.LogError(ex,
                "DomainEventDispatcher: handler threw for {EventType} (EventId={EventId})",
                ev.GetType().Name, ev.EventId);
        }
    }
}
