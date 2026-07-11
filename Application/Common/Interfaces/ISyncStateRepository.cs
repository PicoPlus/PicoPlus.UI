#nullable enable

namespace PicoPlus.Application.Common.Interfaces;

/// <summary>
/// Persistent store for idempotency / duplicate-detection records.
///
/// Each inbound HubSpot event (or outbound sync operation) is recorded here
/// with a composite key of (portalId, objectType, objectId, eventId).
/// Before processing an event the sync layer checks for an existing record;
/// if one exists with a <em>newer</em> version the event is silently discarded
/// (out-of-order protection). If one exists with the <em>same</em> eventId
/// the event is a duplicate and is skipped.
///
/// The default implementation is in-memory (IMemoryCache). Replace with a
/// database-backed implementation for production multi-instance deployments.
/// </summary>
public interface ISyncStateRepository
{
    /// <summary>
    /// Returns the last-known version (HubSpot <c>updatedAt</c> epoch-ms) for the object,
    /// or null if the object has not been synced yet.
    /// </summary>
    Task<long?> GetVersionAsync(string objectType, string objectId, CancellationToken ct = default);

    /// <summary>
    /// Records that a given HubSpot event has been processed.
    /// Stores the event's <paramref name="versionMs"/> (updatedAt) so future
    /// out-of-order events with a lower version can be discarded.
    /// </summary>
    Task SetVersionAsync(string objectType, string objectId, long versionMs, string eventId, CancellationToken ct = default);

    /// <summary>
    /// Returns true if the exact <paramref name="eventId"/> has already been processed
    /// (duplicate-event detection).
    /// </summary>
    Task<bool> IsProcessedAsync(string eventId, CancellationToken ct = default);

    /// <summary>
    /// Marks an object as deleted so restore events can distinguish a new create
    /// from a genuine restore.
    /// </summary>
    Task MarkDeletedAsync(string objectType, string objectId, CancellationToken ct = default);

    /// <summary>Returns true if the object is currently in the deleted state.</summary>
    Task<bool> IsDeletedAsync(string objectType, string objectId, CancellationToken ct = default);
}
