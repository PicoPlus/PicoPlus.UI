#nullable enable

using NovinCRM.Domain.Webhooks;

namespace NovinCRM.Infrastructure.Webhooks;

/// <summary>
/// Internal wrapper that travels through both the main queue and the retry queue.
///
/// Carries the original <see cref="HubSpotWebhookEvent"/> alongside delivery-attempt
/// metadata so the dispatcher can apply per-attempt back-off delays and dead-letter
/// events that have exhausted their retry budget.
///
/// This type is internal to the Infrastructure layer — it is never exposed through
/// any Application-layer interface or domain model.
/// </summary>
public sealed class EventEnvelope
{
    // ── Identity ──────────────────────────────────────────────────────────────

    /// <summary>The verified, parsed domain event.</summary>
    public HubSpotWebhookEvent Event { get; }

    /// <summary>
    /// Correlation ID that ties together all log entries for this delivery attempt.
    /// Assigned lazily by the dispatcher on first processing — propagated to retry
    /// attempts so a single event's full journey can be found in logs.
    /// </summary>
    public string? CorrelationId { get; set; }

    // ── Retry tracking ────────────────────────────────────────────────────────

    /// <summary>
    /// Zero-based count of how many times this event has been attempted.
    /// 0 = first attempt (from the main queue).
    /// 1..n = retry attempts (from the retry queue).
    /// </summary>
    public int AttemptCount { get; private set; }

    /// <summary>UTC time when this envelope was first created (first delivery).</summary>
    public DateTimeOffset FirstEnqueuedAt { get; }

    /// <summary>
    /// UTC time before which the dispatcher must NOT attempt this envelope.
    /// Set by the retry policy after each failed attempt.
    /// </summary>
    public DateTimeOffset NotBefore { get; private set; }

    /// <summary>
    /// The handler type that last failed, for structured log context.
    /// Null on first attempt.
    /// </summary>
    public string? LastFailedHandler { get; private set; }

    /// <summary>
    /// The exception message from the last failure, for structured logging.
    /// Null on first attempt.
    /// </summary>
    public string? LastErrorMessage { get; private set; }

    // ── Construction ──────────────────────────────────────────────────────────

    public EventEnvelope(HubSpotWebhookEvent ev)
    {
        Event             = ev;
        AttemptCount      = 0;
        FirstEnqueuedAt   = DateTimeOffset.UtcNow;
        NotBefore         = DateTimeOffset.MinValue;
    }

    // ── Mutation — called only by the dispatcher ──────────────────────────────

    /// <summary>
    /// Record a failed attempt and set the next <see cref="NotBefore"/> time.
    /// Returns the same instance (mutates in place — safe because only one
    /// worker owns an envelope at a time).
    /// </summary>
    public EventEnvelope RecordFailure(
        string   handlerName,
        string   errorMessage,
        TimeSpan delay)
    {
        AttemptCount++;
        LastFailedHandler = handlerName;
        LastErrorMessage  = errorMessage;
        NotBefore         = DateTimeOffset.UtcNow + delay;
        return this;
    }
}
