#nullable enable

namespace NovinCRM.Infrastructure.Webhooks;

/// <summary>
/// Calculates the delay before a failed event should be retried.
///
/// Injected into <see cref="WebhookDispatcherService"/> so the policy can be
/// swapped or mocked in tests without changing the dispatcher.
/// </summary>
public interface IRetryPolicy
{
    /// <summary>
    /// Whether the event should be retried given the attempt history.
    /// </summary>
    bool ShouldRetry(EventEnvelope envelope);

    /// <summary>
    /// Computes the delay before the next attempt.
    /// Only called when <see cref="ShouldRetry"/> returned <c>true</c>.
    /// </summary>
    TimeSpan GetDelay(EventEnvelope envelope);
}

/// <summary>
/// Full-jitter exponential back-off retry policy.
///
/// Delay formula (per "Exponential Backoff And Jitter" — AWS Architecture Blog):
///   cap  = min(MaxRetryDelay, BaseRetryDelay × 2^attempt)
///   delay = Random(0, cap)          ← full jitter
///
/// Full jitter spreads retries across time, preventing thundering-herd
/// when many events fail simultaneously (e.g. downstream outage recovery).
/// </summary>
public sealed class ExponentialBackoffRetryPolicy : IRetryPolicy
{
    private readonly HubSpotWebhookOptions _options;
    private readonly Random _rng = new();           // instance per service — not shared

    public ExponentialBackoffRetryPolicy(HubSpotWebhookOptions options)
        => _options = options;

    /// <inheritdoc />
    public bool ShouldRetry(EventEnvelope envelope)
        => envelope.AttemptCount < _options.MaxRetryAttempts;

    /// <inheritdoc />
    public TimeSpan GetDelay(EventEnvelope envelope)
    {
        // Exponential cap: BaseDelay × 2^attempt, capped at MaxRetryDelay.
        // Use double arithmetic to avoid integer overflow on large attempt counts.
        var baseMs  = _options.BaseRetryDelay.TotalMilliseconds;
        var capMs   = _options.MaxRetryDelay.TotalMilliseconds;
        var expMs   = Math.Min(capMs, baseMs * Math.Pow(2, envelope.AttemptCount));

        // Full jitter: uniformly random in [0, cap]
        var jitteredMs = _rng.NextDouble() * expMs;

        return TimeSpan.FromMilliseconds(jitteredMs);
    }
}
