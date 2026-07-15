#nullable enable

using System.ComponentModel.DataAnnotations;

namespace NovinCRM.Infrastructure.Webhooks;

/// <summary>
/// Configuration for HubSpot webhook verification and the async processing pipeline.
/// Bound from the "HubSpot" section of IConfiguration.
///
/// In Development: set via User Secrets.
/// In Production:  set via environment variables or Liara secret manager.
///
///   HubSpot__WebhookClientSecret=your-client-secret
///   HubSpot__AppId=12345
/// </summary>
public sealed class HubSpotWebhookOptions
{
    public const string SectionName = "HubSpot";

    // ── Signature verification ────────────────────────────────────────────────

    /// <summary>
    /// The "Client Secret" value found on your HubSpot Private App or OAuth App settings page.
    /// This is NOT the same as the API token (PAT). It is the shared secret used to sign
    /// webhook payloads.
    ///
    /// Required. The webhook endpoint rejects all requests if this is empty.
    /// </summary>
    [Required]
    public string WebhookClientSecret { get; init; } = string.Empty;

    /// <summary>
    /// Your HubSpot App ID — appears in the app's webhook settings URL.
    /// Used for logging and future subscription management.
    /// </summary>
    public long AppId { get; init; }

    // ── Replay-attack window ──────────────────────────────────────────────────

    /// <summary>
    /// Maximum age of an accepted webhook request, measured from
    /// the <c>X-HubSpot-Request-Timestamp</c> header value.
    ///
    /// HubSpot's own documentation requires receivers to reject requests
    /// older than 5 minutes. Default: 5 minutes.
    /// </summary>
    public TimeSpan MaxRequestAge { get; init; } = TimeSpan.FromMinutes(5);

    // ── Main queue ────────────────────────────────────────────────────────────

    /// <summary>
    /// Maximum number of events that may wait in the main queue.
    /// The writer applies back-pressure (awaits) when this is full,
    /// rather than dropping events.
    /// Default: 10,000.
    /// </summary>
    public int QueueCapacity { get; init; } = 10_000;

    // ── Concurrency ───────────────────────────────────────────────────────────

    /// <summary>
    /// Number of parallel worker tasks that consume events from the main queue.
    ///
    /// Each worker runs every registered handler sequentially for its event.
    /// Workers run concurrently with each other, so different events are
    /// processed in parallel while the same event sees handlers in order.
    ///
    /// Default: 4. Set to 1 to restore strictly serial processing.
    /// </summary>
    [Range(1, 64)]
    public int ConsumerCount { get; init; } = 4;

    // ── Retry queue ───────────────────────────────────────────────────────────

    /// <summary>
    /// Maximum number of events waiting in the retry queue.
    /// Excess entries are dropped and logged at Error level.
    /// Default: 2,000.
    /// </summary>
    public int RetryQueueCapacity { get; init; } = 2_000;

    /// <summary>
    /// Maximum number of times a failed event will be retried before
    /// it is sent to the dead-letter log and discarded.
    /// Default: 5.
    /// </summary>
    [Range(0, 20)]
    public int MaxRetryAttempts { get; init; } = 5;

    /// <summary>
    /// Base delay for exponential back-off between retry attempts.
    /// Actual delay = <c>BaseRetryDelay × 2^attempt</c> with jitter.
    /// Default: 2 seconds.
    /// </summary>
    public TimeSpan BaseRetryDelay { get; init; } = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Maximum cap on the computed back-off delay.
    /// Default: 2 minutes.
    /// </summary>
    public TimeSpan MaxRetryDelay { get; init; } = TimeSpan.FromMinutes(2);

    // ── Graceful shutdown ─────────────────────────────────────────────────────

    /// <summary>
    /// How long the dispatcher will wait to drain the main queue during
    /// application shutdown before forcibly stopping.
    /// Default: 30 seconds.
    /// </summary>
    public TimeSpan ShutdownDrainTimeout { get; init; } = TimeSpan.FromSeconds(30);
}
