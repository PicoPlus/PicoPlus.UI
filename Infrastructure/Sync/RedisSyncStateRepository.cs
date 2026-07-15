#nullable enable

using System.Text;
using Microsoft.Extensions.Caching.Distributed;
using NovinCRM.Application.Common;
using NovinCRM.Application.Common.Interfaces;

namespace NovinCRM.Infrastructure.Sync;

/// <summary>
/// Redis-backed implementation of <see cref="ISyncStateRepository"/>.
///
/// Survives container restarts and works correctly across multiple parallel
/// application instances. Uses <see cref="IDistributedCache"/> so the
/// backing store can be swapped (Redis in production, in-process in dev)
/// by changing one DI registration in <c>Program.cs</c>.
///
/// TTL policy (matches the original in-memory implementation):
///   - Event idempotency key   : 24 h
///   - Object version          : 72 h  (longer to survive weekend outages)
///   - Deletion tombstone      : 72 h
/// </summary>
public sealed class RedisSyncStateRepository : ISyncStateRepository
{
    private static readonly TimeSpan EventTtl   = TimeSpan.FromHours(24);
    private static readonly TimeSpan VersionTtl = TimeSpan.FromHours(72);
    private static readonly TimeSpan DeletedTtl = TimeSpan.FromHours(72);

    // Sentinel value stored as a UTF-8 byte — minimal storage footprint.
    private static readonly byte[] TrueBytes = "1"u8.ToArray();

    private readonly IDistributedCache _cache;

    public RedisSyncStateRepository(IDistributedCache cache) => _cache = cache;

    // ── ISyncStateRepository ─────────────────────────────────────────────────

    public async Task<long?> GetVersionAsync(
        string objectType, string objectId, CancellationToken ct = default)
    {
        var key   = CacheKeys.SyncVersion(objectType, objectId);
        var bytes = await _cache.GetAsync(key, ct);
        if (bytes is null) return null;

        var text = Encoding.UTF8.GetString(bytes);
        return long.TryParse(text, out var v) ? v : null;
    }

    public async Task SetVersionAsync(
        string objectType, string objectId, long versionMs, string eventId,
        CancellationToken ct = default)
    {
        var versionKey = CacheKeys.SyncVersion(objectType, objectId);
        var eventKey   = CacheKeys.SyncEvent(eventId);
        var versionBytes = Encoding.UTF8.GetBytes(versionMs.ToString());

        var versionOpts = new DistributedCacheEntryOptions
            { AbsoluteExpirationRelativeToNow = VersionTtl };
        var eventOpts = new DistributedCacheEntryOptions
            { AbsoluteExpirationRelativeToNow = EventTtl };

        // Fire both writes; no atomicity required — worst case is a re-process
        // of a single event, which the version check will discard anyway.
        await _cache.SetAsync(versionKey, versionBytes, versionOpts, ct);
        await _cache.SetAsync(eventKey,   TrueBytes,    eventOpts,   ct);
    }

    public async Task<bool> IsProcessedAsync(
        string eventId, CancellationToken ct = default)
    {
        var bytes = await _cache.GetAsync(CacheKeys.SyncEvent(eventId), ct);
        return bytes is not null;
    }

    public async Task MarkDeletedAsync(
        string objectType, string objectId, CancellationToken ct = default)
    {
        var opts = new DistributedCacheEntryOptions
            { AbsoluteExpirationRelativeToNow = DeletedTtl };
        await _cache.SetAsync(CacheKeys.SyncDeleted(objectType, objectId), TrueBytes, opts, ct);
    }

    public async Task<bool> IsDeletedAsync(
        string objectType, string objectId, CancellationToken ct = default)
    {
        var bytes = await _cache.GetAsync(CacheKeys.SyncDeleted(objectType, objectId), ct);
        return bytes is not null;
    }
}
