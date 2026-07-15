#nullable enable

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NovinCRM.Application.Common.Interfaces;
using NovinCRM.Domain.Webhooks;

namespace NovinCRM.Infrastructure.Webhooks;

/// <summary>
/// Enterprise-grade webhook event dispatcher.
///
/// Architecture
/// ────────────
///   ┌─────────────────────────────────────────────────────────────────────┐
///   │  InMemoryWebhookEventQueue                                          │
///   │   mainChannel ──► [N ConsumerWorker tasks]  ──► handler chain      │
///   │   retryChannel ──► [N RetryWorker tasks]    ──► handler chain      │
///   │                         │failure                                   │
///   │                         └──► retryChannel (if retryable + budget)  │
///   │                              OR dead-letter log (budget exhausted)  │
///   └─────────────────────────────────────────────────────────────────────┘
///
/// Design decisions
/// ────────────────
/// 1. CONCURRENCY  — <see cref="HubSpotWebhookOptions.ConsumerCount"/> parallel tasks
///    consume both the main and retry channels simultaneously. Each task owns an
///    independent DI scope so scoped services (e.g. DbContext) are safe.
///
/// 2. RETRY QUEUE  — failed events are wrapped in an <see cref="EventEnvelope"/>
///    that tracks attempt count and the <c>NotBefore</c> time. Before re-running
///    the handler, the worker sleeps until <c>NotBefore</c>. Full-jitter exponential
///    back-off is applied via <see cref="IRetryPolicy"/>.
///
/// 3. FATAL VS RETRYABLE — handlers can declare a permanent failure by implementing
///    <see cref="IHubSpotWebhookHandler.IsFatalException"/>. Fatal events are
///    dead-lettered immediately without consuming retry budget.
///
/// 4. GRACEFUL SHUTDOWN — on cancellation the service:
///      a. Stops accepting new main-queue items (channel reader is drained, not closed).
///      b. Waits up to <see cref="HubSpotWebhookOptions.ShutdownDrainTimeout"/> for all
///         in-flight worker tasks to complete.
///      c. Logs a warning with the remaining queue depth if the timeout expires.
///    The retry channel is NOT drained on shutdown — in-flight retries with pending
///    delays would block shutdown. Instead, events still in the retry channel are
///    logged as "retry events abandoned" so operators know to re-deliver.
///
/// 5. THREAD SAFETY — all shared state passes through Channel<T> which is
///    intrinsically thread-safe. No locks, no volatile fields.
///
/// 6. HANDLER ISOLATION — each event dispatch creates a fresh DI scope.
///    Scoped services (e.g. IContactRepository) are safe to inject into handlers.
/// </summary>
public sealed class WebhookDispatcherService : BackgroundService
{
    private readonly IRetryableEventQueue        _queue;
    private readonly IServiceScopeFactory        _scopeFactory;
    private readonly IRetryPolicy                _retryPolicy;
    private readonly HubSpotWebhookOptions       _options;
    private readonly ILogger<WebhookDispatcherService> _logger;
    private readonly IDeadLetterStore            _deadLetterStore;

    public WebhookDispatcherService(
        IRetryableEventQueue            queue,
        IServiceScopeFactory            scopeFactory,
        IRetryPolicy                    retryPolicy,
        IOptions<HubSpotWebhookOptions> options,
        IDeadLetterStore                deadLetterStore,
        ILogger<WebhookDispatcherService> logger)
    {
        _queue           = queue;
        _scopeFactory    = scopeFactory;
        _retryPolicy     = retryPolicy;
        _options         = options.Value;
        _deadLetterStore = deadLetterStore;
        _logger          = logger;
    }

    // ── BackgroundService entrypoint ──────────────────────────────────────────

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "WebhookDispatcher: started — {Workers} consumer(s), " +
            "MaxRetry={MaxRetry}, BaseDelay={Base}, MaxDelay={Max}",
            _options.ConsumerCount,
            _options.MaxRetryAttempts,
            _options.BaseRetryDelay,
            _options.MaxRetryDelay);

        // Spawn N consumer workers for the main queue.
        // Spawn N retry workers for the retry queue.
        // All workers run concurrently; each iteration creates its own DI scope.
        var workerTasks = Enumerable
            .Range(0, _options.ConsumerCount)
            .SelectMany(_ => new[]
            {
                RunMainWorkerAsync(stoppingToken),
                RunRetryWorkerAsync(stoppingToken)
            })
            .ToArray();

        await WaitWithGracefulDrainAsync(workerTasks, stoppingToken).ConfigureAwait(false);

        _logger.LogInformation(
            "WebhookDispatcher: stopped — mainQueue={Main}, retryQueue={Retry} events remaining",
            _queue.Count, _queue.RetryReader.Count);
    }

    // ── Main queue worker ─────────────────────────────────────────────────────

    private async Task RunMainWorkerAsync(CancellationToken ct)
    {
        await foreach (var ev in _queue.ReadAllAsync(ct).ConfigureAwait(false))
        {
            var envelope = new EventEnvelope(ev);
            await ProcessEnvelopeAsync(envelope, ct).ConfigureAwait(false);
        }
    }

    // ── Retry queue worker ────────────────────────────────────────────────────

    private async Task RunRetryWorkerAsync(CancellationToken ct)
    {
        await foreach (var envelope in _queue.RetryReader.ReadAllAsync(ct).ConfigureAwait(false))
        {
            // Honour the NotBefore time — sleep until the back-off window expires.
            var delay = envelope.NotBefore - DateTimeOffset.UtcNow;
            if (delay > TimeSpan.Zero)
            {
                try
                {
                    await Task.Delay(delay, ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    // Shutdown during back-off — abandon this retry.
                    _logger.LogWarning(
                        "WebhookDispatcher: retry abandoned during shutdown — " +
                        "EventId={EventId} SubscriptionType={Type} Attempt={Attempt}",
                        envelope.Event.EventId, envelope.Event.SubscriptionType,
                        envelope.AttemptCount);
                    return;
                }
            }

            await ProcessEnvelopeAsync(envelope, ct).ConfigureAwait(false);
        }
    }

    // ── Core dispatch logic ───────────────────────────────────────────────────

    /// <summary>
    /// Runs the handler chain for one envelope inside a fresh DI scope.
    /// On handler failure: schedules a retry or dead-letters, depending on
    /// retry budget and handler's <see cref="IHubSpotWebhookHandler.IsFatalException"/>.
    /// </summary>
    private async Task ProcessEnvelopeAsync(EventEnvelope envelope, CancellationToken ct)
    {
        using var scope    = _scopeFactory.CreateScope();

        // Push a structured log scope so every log call inside this envelope
        // processing automatically carries { CorrelationId, HubSpotEventId, EventType }.
        var correlationId = envelope.CorrelationId
            ??= Guid.NewGuid().ToString("N");

        using var logScope = _logger.BeginScope(new Dictionary<string, object?>
        {
            ["CorrelationId"]   = correlationId,
            ["HubSpotEventId"]  = envelope.Event.EventId,
            ["EventType"]       = envelope.Event.SubscriptionType?.ToString(),
            ["ObjectId"]        = envelope.Event.ObjectId,
        });

        var handlers = scope.ServiceProvider
                            .GetServices<IHubSpotWebhookHandler>();

        foreach (var handler in handlers)
        {
            if (!IsHandlerInterestedIn(handler, envelope.Event))
                continue;

            try
            {
                await handler.HandleAsync(envelope.Event, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // Shutdown cancellation — do not retry, just stop.
                _logger.LogWarning(
                    "WebhookDispatcher: handler {Handler} cancelled during shutdown — " +
                    "EventId={EventId}", handler.GetType().Name, envelope.Event.EventId);
                return;
            }
            catch (Exception ex)
            {
                await HandleFailureAsync(envelope, handler, ex, ct).ConfigureAwait(false);
                // Stop the handler chain for this event on failure —
                // the event goes to retry and all handlers re-run on the next attempt.
                return;
            }
        }
    }

    // ── Failure routing ───────────────────────────────────────────────────────

    private async Task HandleFailureAsync(
        EventEnvelope             envelope,
        IHubSpotWebhookHandler    handler,
        Exception                 ex,
        CancellationToken         ct)
    {
        bool isFatal = false;
        try { isFatal = handler.IsFatalException(ex); }
        catch { /* guard against IsFatalException itself throwing */ }

        if (isFatal)
        {
            DeadLetter(envelope, handler.GetType().Name, ex, reason: "fatal exception");
            return;
        }

        var delay = _retryPolicy.GetDelay(envelope);
        envelope.RecordFailure(handler.GetType().Name, ex.Message, delay);

        if (!_retryPolicy.ShouldRetry(envelope))
        {
            DeadLetter(envelope, handler.GetType().Name, ex, reason: "retry budget exhausted");
            return;
        }

        _logger.LogWarning(ex,
            "WebhookDispatcher: handler {Handler} failed — " +
            "EventId={EventId} Type={Type} Attempt={Attempt}/{Max} " +
            "RetryAfter={Delay:g}",
            handler.GetType().Name,
            envelope.Event.EventId,
            envelope.Event.SubscriptionType,
            envelope.AttemptCount,
            _options.MaxRetryAttempts,
            delay);

        // Enqueue to retry channel. TryWrite is used (non-blocking) because
        // the retry channel is BoundedChannelFullMode.DropOldest — it never blocks.
        if (!_queue.RetryWriter.TryWrite(envelope))
        {
            // DropOldest mode guarantees TryWrite succeeds or drops a different item,
            // so this branch is only reached if the writer is completed (shutdown).
            _logger.LogError(
                "WebhookDispatcher: could not enqueue retry — channel closed. " +
                "EventId={EventId} will be lost.", envelope.Event.EventId);
        }
    }

    private void DeadLetter(
        EventEnvelope envelope,
        string        handlerName,
        Exception     ex,
        string        reason)
    {
        _logger.LogError(ex,
            "WebhookDispatcher: DEAD-LETTER — {Reason}. " +
            "Handler={Handler} EventId={EventId} PortalId={Portal} " +
            "Type={Type} Attempts={Attempts} FirstEnqueued={First:O}",
            reason,
            handlerName,
            envelope.Event.EventId,
            envelope.Event.PortalId,
            envelope.Event.SubscriptionType,
            envelope.AttemptCount,
            envelope.FirstEnqueuedAt);

        // Persist to dead-letter store for later inspection / re-drive
        var entry = new NovinCRM.Application.Common.Interfaces.DeadLetterEntry
        {
            EventId          = envelope.Event.EventId.ToString(),
            SubscriptionType = envelope.Event.SubscriptionType?.ToString() ?? string.Empty,
            PortalId         = envelope.Event.PortalId,
            HandlerName      = handlerName,
            FailureReason    = reason,
            LastErrorMessage = ex.Message,
            AttemptCount     = envelope.AttemptCount,
            FirstEnqueuedAt  = envelope.FirstEnqueuedAt,
            DeadLetteredAt   = DateTimeOffset.UtcNow,
            EventPayloadJson = System.Text.Json.JsonSerializer.Serialize(envelope.Event)
        };
        // Fire-and-forget — DeadLetter is called on a hot path; don't block
        _ = _deadLetterStore.WriteAsync(entry);
    }

    // ── Graceful shutdown drain ───────────────────────────────────────────────

    /// <summary>
    /// Waits for all worker tasks to complete, with a bounded drain timeout.
    /// If the timeout expires, logs remaining queue depth and returns — the host
    /// will then forcibly cancel the workers via the stopping token.
    /// </summary>
    private async Task WaitWithGracefulDrainAsync(
        Task[]            workerTasks,
        CancellationToken stoppingToken)
    {
        // stoppingToken fires when the host calls StopAsync.
        // We want to give workers time to finish their current event, so we
        // use a separate drain CTS that extends beyond the stopping signal.
        using var drainCts = new CancellationTokenSource(_options.ShutdownDrainTimeout);

        _logger.LogInformation(
            "WebhookDispatcher: shutdown requested — draining up to {Timeout}",
            _options.ShutdownDrainTimeout);

        try
        {
            // WhenAll with the drain timeout as the guard.
            await Task.WhenAll(workerTasks)
                      .WaitAsync(drainCts.Token)
                      .ConfigureAwait(false);

            _logger.LogInformation("WebhookDispatcher: all workers drained cleanly");
        }
        catch (OperationCanceledException) when (drainCts.IsCancellationRequested)
        {
            _logger.LogWarning(
                "WebhookDispatcher: drain timeout expired after {Timeout} — " +
                "{Main} main-queue and {Retry} retry-queue events remain unprocessed",
                _options.ShutdownDrainTimeout,
                _queue.Count,
                _queue.RetryReader.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WebhookDispatcher: unexpected error during shutdown drain");
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static bool IsHandlerInterestedIn(
        IHubSpotWebhookHandler handler,
        HubSpotWebhookEvent    ev)
        => handler.SupportedSubscriptionTypes is null ||
           handler.SupportedSubscriptionTypes.Contains(ev.SubscriptionType);
}
