#nullable enable

namespace PicoPlus.Domain.Events;

/// <summary>
/// Marker interface for integration events.
///
/// An integration event crosses bounded-context or system boundaries —
/// it carries the minimum data needed for external consumers to react
/// without querying back into this system.
///
/// Integration events are dispatched via <see cref="PicoPlus.Application.Common.Interfaces.IIntegrationEventPublisher"/>
/// which routes them through the webhook event pipeline (IWebhookEventQueue → background dispatcher).
///
/// Rules:
///   • Immutable — init-only properties only.
///   • Self-contained — carry all data the consumer needs.
///   • Named with the suffix "IntegrationEvent".
///   • No domain object references — only primitive / DTO values.
/// </summary>
public interface IIntegrationEvent
{
    /// <summary>Unique identifier for this event occurrence.</summary>
    Guid EventId { get; }

    /// <summary>UTC time when the event was published.</summary>
    DateTimeOffset OccurredAt { get; }

    /// <summary>
    /// Logical event name used for routing consumers.
    /// Convention: "{aggregate}.{verb}" e.g. "contact.registered"
    /// </summary>
    string EventType { get; }
}
