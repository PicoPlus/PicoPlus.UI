#nullable enable

using Microsoft.Extensions.Caching.Memory;
using NovinCRM.Application.Common.Interfaces;

namespace NovinCRM.Infrastructure.Sync;

/// <summary>
/// In-memory implementation of <see cref="ISyncStateRepository"/>.
///
/// Uses <see cref="IMemoryCache"/> with a sliding expiration of 24 hours.
/// Sufficient for single-instance deployments.
///
/// For multi-instance (scale-out) deployments replace this with a Redis or
/// SQL-backed implementation — swap the DI registration in
/// <see cref="SyncServiceExtensions"/> without changing any sync logic.
/// </summary>
public sealed class InMemorySyncStateRepository : ISyncStateRepository
{
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan Ttl = TimeSpan.FromHours(24);

    // All key patterns are centralised in CacheKeys — no local constants needed.

    public InMemorySyncStateRepository(IMemoryCache cache) => _cache = cache;

    public Task<long?> GetVersionAsync(string objectType, string objectId, CancellationToken ct = default)
    {
        var key = NovinCRM.Application.Common.CacheKeys.SyncVersion(objectType, objectId);
        return Task.FromResult(_cache.TryGetValue<long>(key, out var v) ? (long?)v : null);
    }

    public Task SetVersionAsync(string objectType, string objectId, long versionMs, string eventId, CancellationToken ct = default)
    {
        _cache.Set(NovinCRM.Application.Common.CacheKeys.SyncVersion(objectType, objectId), versionMs, Ttl);
        _cache.Set(NovinCRM.Application.Common.CacheKeys.SyncEvent(eventId), true, Ttl);
        return Task.CompletedTask;
    }

    public Task<bool> IsProcessedAsync(string eventId, CancellationToken ct = default)
        => Task.FromResult(_cache.TryGetValue(NovinCRM.Application.Common.CacheKeys.SyncEvent(eventId), out _));

    public Task MarkDeletedAsync(string objectType, string objectId, CancellationToken ct = default)
    {
        _cache.Set(NovinCRM.Application.Common.CacheKeys.SyncDeleted(objectType, objectId), true, Ttl);
        return Task.CompletedTask;
    }

    public Task<bool> IsDeletedAsync(string objectType, string objectId, CancellationToken ct = default)
        => Task.FromResult(_cache.TryGetValue(NovinCRM.Application.Common.CacheKeys.SyncDeleted(objectType, objectId), out _));
}
