#nullable enable

using PicoPlus.Domain.Events;

namespace PicoPlus.Application.Common.Interfaces;

/// <summary>
/// Publishes integration events to external consumers.
///
/// Integration events are published AFTER domain event handlers have run
/// and any side effects (e.g. SMS, cache invalidation) have completed.
///
/// The default implementation routes integration events through the
/// existing webhook event pipeline (<see cref="IWebhookEventQueue"/>)
/// so they are delivered off the request thread with the same retry
/// guarantees as inbound HubSpot webhook events.
///
/// Usage in a domain event handler:
/// <code>
///   await _publisher.PublishAsync(new DealCreatedIntegrationEvent { … }, ct);
/// </code>
/// </summary>
public interface IIntegrationEventPublisher
{
    Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken ct = default);
}
