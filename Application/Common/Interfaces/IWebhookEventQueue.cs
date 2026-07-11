#nullable enable

using PicoPlus.Domain.Webhooks;

namespace PicoPlus.Application.Common.Interfaces;

/// <summary>
/// Decouples the HTTP endpoint from event processing.
///
/// The webhook endpoint enqueues events here and returns HTTP 200 immediately.
/// A background service dequeues and dispatches them off the HTTP request thread.
///
/// Two write paths exist:
///   • <see cref="TryEnqueue"/> — non-blocking, drops if full. Use only when the
///     caller absolutely cannot await (e.g. inside a synchronous callback).
///   • <see cref="WriteAsync"/> — awaits when the queue is full (back-pressure).
///     Preferred path for all async call sites.
/// </summary>
public interface IWebhookEventQueue
{
    // ── Write ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Attempt to enqueue a batch without blocking.
    /// Returns <c>false</c> if one or more events were dropped because the queue
    /// was full. Callers should log a warning in that case.
    /// </summary>
    bool TryEnqueue(IReadOnlyList<HubSpotWebhookEvent> events);

    /// <summary>
    /// Write a batch to the queue, awaiting capacity if it is full (back-pressure).
    /// Prefer this over <see cref="TryEnqueue"/> in all async contexts.
    /// Completes when all events in the batch have been accepted.
    /// </summary>
    ValueTask WriteAsync(IReadOnlyList<HubSpotWebhookEvent> events, CancellationToken ct = default);

    // ── Read ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Asynchronously streams events from the queue.
    /// Awaits indefinitely until an event is available or <paramref name="ct"/> is cancelled.
    /// Used exclusively by the background dispatcher.
    /// </summary>
    IAsyncEnumerable<HubSpotWebhookEvent> ReadAllAsync(CancellationToken ct);

    // ── Diagnostics ───────────────────────────────────────────────────────────

    /// <summary>Approximate number of events currently waiting in the main queue.</summary>
    int Count { get; }
}
