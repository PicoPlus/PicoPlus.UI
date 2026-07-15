using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using NovinCRM.Application.Common.Interfaces;

namespace NovinCRM.Infrastructure.Webhooks;

/// <summary>
/// Thread-safe in-memory dead-letter store. Survives only for the lifetime of
/// the process — on restart entries are lost. Replace with a Redis or SQL
/// implementation for multi-instance / durable storage.
///
/// Capped at <see cref="MaxEntries"/> to prevent unbounded memory growth.
/// When the cap is hit the oldest entry is evicted (drop-oldest semantics).
/// </summary>
public sealed class InMemoryDeadLetterStore : IDeadLetterStore
{
    public const int MaxEntries = 500;

    private readonly ConcurrentQueue<DeadLetterEntry> _queue = new();
    private readonly ILogger<InMemoryDeadLetterStore>  _logger;

    public InMemoryDeadLetterStore(ILogger<InMemoryDeadLetterStore> logger)
        => _logger = logger;

    public Task WriteAsync(DeadLetterEntry entry, CancellationToken ct = default)
    {
        _queue.Enqueue(entry);

        // Evict oldest if over cap
        while (_queue.Count > MaxEntries)
            _queue.TryDequeue(out _);

        _logger.LogWarning(
            "DeadLetter stored: EventId={EventId} Type={Type} Attempts={Attempts} Reason={Reason}",
            entry.EventId, entry.SubscriptionType, entry.AttemptCount, entry.FailureReason);

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<DeadLetterEntry>> GetAllAsync(int limit = 100, CancellationToken ct = default)
    {
        var result = (IReadOnlyList<DeadLetterEntry>)_queue
            .TakeLast(limit)
            .OrderByDescending(e => e.DeadLetteredAt)
            .ToList();
        return Task.FromResult(result);
    }

    public Task<int> CountAsync(CancellationToken ct = default)
        => Task.FromResult(_queue.Count);

    public Task DeleteAsync(string eventId, CancellationToken ct = default)
    {
        // ConcurrentQueue doesn't support removal; rebuild without the target entry
        var kept = _queue.Where(e => e.EventId != eventId).ToList();
        while (_queue.TryDequeue(out _)) { }
        foreach (var e in kept) _queue.Enqueue(e);
        return Task.CompletedTask;
    }
}
