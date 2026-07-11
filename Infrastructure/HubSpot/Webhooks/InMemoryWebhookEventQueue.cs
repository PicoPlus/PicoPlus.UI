#nullable enable

using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PicoPlus.Application.Common.Interfaces;
using PicoPlus.Domain.Webhooks;

namespace PicoPlus.Infrastructure.Webhooks;

/// <summary>
/// Dual-channel, thread-safe, in-process webhook event queue.
///
/// Architecture:
///   Main channel  — receives events from the HTTP endpoint (bounded, back-pressure).
///   Retry channel — receives events that failed handling and are eligible for retry
///                   (bounded, drop-oldest to prefer fresh retries over stale ones).
///
/// Main channel uses <see cref="BoundedChannelFullMode.Wait"/> so that
/// <see cref="WriteAsync"/> applies true back-pressure: the HTTP endpoint awaits
/// capacity instead of silently dropping events. The endpoint has a per-request
/// timeout that bounds how long it will wait, so the total request time is bounded.
///
/// The retry channel uses <see cref="BoundedChannelFullMode.DropOldest"/> to keep
/// retry depth bounded without blocking the dispatcher — if the retry queue overflows,
/// the oldest (most stale) pending retries are evicted first.
///
/// Both channels are multi-writer / single-reader, guarded by the Channel API
/// which provides all necessary synchronisation internally.
///
/// Lifetime: Singleton.
/// </summary>
public sealed class InMemoryWebhookEventQueue : IWebhookEventQueue
{
    private readonly Channel<HubSpotWebhookEvent>  _mainChannel;
    private readonly Channel<EventEnvelope>        _retryChannel;
    private readonly ILogger<InMemoryWebhookEventQueue> _logger;

    // Expose the retry channel reader/writer to the dispatcher without
    // leaking it through the public IWebhookEventQueue interface.
    internal ChannelReader<EventEnvelope> RetryReader  => _retryChannel.Reader;
    internal ChannelWriter<EventEnvelope> RetryWriter  => _retryChannel.Writer;

    public InMemoryWebhookEventQueue(
        IOptions<HubSpotWebhookOptions>        options,
        ILogger<InMemoryWebhookEventQueue>     logger)
    {
        _logger = logger;
        var opts = options.Value;

        // ── Main channel — back-pressure on write ─────────────────────────────
        var mainOpts = new BoundedChannelOptions(opts.QueueCapacity)
        {
            FullMode                      = BoundedChannelFullMode.Wait,
            SingleReader                  = false,   // N concurrent workers read
            SingleWriter                  = false,   // multiple HTTP threads write
            AllowSynchronousContinuations = false
        };
        _mainChannel = Channel.CreateBounded<HubSpotWebhookEvent>(mainOpts);

        // ── Retry channel — drop-oldest when full ─────────────────────────────
        var retryOpts = new BoundedChannelOptions(opts.RetryQueueCapacity)
        {
            FullMode                      = BoundedChannelFullMode.DropOldest,
            SingleReader                  = false,   // N retry workers read
            SingleWriter                  = false,   // dispatcher threads write retries
            AllowSynchronousContinuations = false
        };
        _retryChannel = Channel.CreateBounded<EventEnvelope>(retryOpts);
    }

    // ── IWebhookEventQueue ────────────────────────────────────────────────────

    /// <inheritdoc />
    /// Non-blocking fallback — use only from synchronous callers.
    public bool TryEnqueue(IReadOnlyList<HubSpotWebhookEvent> events)
    {
        int written = 0, dropped = 0;
        foreach (var ev in events)
        {
            if (_mainChannel.Writer.TryWrite(ev)) written++;
            else                                  dropped++;
        }
        if (dropped > 0)
            _logger.LogWarning(
                "WebhookQueue: dropped {Dropped}/{Total} events — main queue full",
                dropped, events.Count);
        return dropped == 0;
    }

    /// <inheritdoc />
    /// Back-pressure write — awaits capacity. Preferred for all async callers.
    public async ValueTask WriteAsync(
        IReadOnlyList<HubSpotWebhookEvent> events,
        CancellationToken ct = default)
    {
        foreach (var ev in events)
            await _mainChannel.Writer.WriteAsync(ev, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<HubSpotWebhookEvent> ReadAllAsync(
        [EnumeratorCancellation] CancellationToken ct)
    {
        await foreach (var ev in _mainChannel.Reader.ReadAllAsync(ct).ConfigureAwait(false))
            yield return ev;
    }

    /// <inheritdoc />
    public int Count => _mainChannel.Reader.Count;
}
