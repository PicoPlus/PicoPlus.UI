namespace NovinCRM.Application.Common;

/// <summary>
/// Single source of truth for all <see cref="Microsoft.Extensions.Caching.Memory.IMemoryCache"/>
/// and <see cref="Microsoft.Extensions.Caching.Distributed.IDistributedCache"/> key patterns.
///
/// Using compile-time constants eliminates the silent cache-miss bugs that occur
/// when a string literal is mistyped in one call site but correct in another.
/// </summary>
public static class CacheKeys
{
    /// <summary>Key for the entire Kanban board snapshot (all columns + cards).</summary>
    public static string KanbanBoard => "kanban:board";

    /// <summary>Key for a single contact's user-panel aggregate (deals, stats, notes).</summary>
    public static string UserPanel(string contactId) => $"userpanel:{contactId}";

    /// <summary>Key for a generic CRM object snapshot (contact, deal, company, …).</summary>
    public static string CrmObject(string objectType, string objectId) =>
        $"crm:{objectType}:{objectId}";

    // ── Sync-state keys (used by ISyncStateRepository) ───────────────────────

    /// <summary>Idempotency key for a processed webhook event (dedup guard).</summary>
    public static string SyncEvent(string eventId) => $"sync:evt:{eventId}";

    /// <summary>Version counter for an object (out-of-order protection).</summary>
    public static string SyncVersion(string objectType, string objectId) =>
        $"sync:ver:{objectType}:{objectId}";

    /// <summary>Deletion tombstone for an object.</summary>
    public static string SyncDeleted(string objectType, string objectId) =>
        $"sync:del:{objectType}:{objectId}";

    // ── OTP keys (used by OtpService) ────────────────────────────────────────

    /// <summary>Distributed-cache key for a pending OTP code.</summary>
    public static string Otp(string normalizedPhone) => $"otp:{normalizedPhone}";
}
