#nullable enable

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NovinCRM.Application.Common.Interfaces;
using NovinCRM.Domain.Webhooks;

namespace NovinCRM.Infrastructure.Sync;

/// <summary>
/// Full bidirectional synchronisation service.
///
/// Responsibilities
/// ────────────────
/// • Receive HubSpot webhook events from <see cref="HubSpotWebhookSyncHandler"/>.
/// • For each event: fetch the current state from HubSpot via the existing
///   repository interfaces, then update the local read-model / cache.
/// • Invalidate <see cref="IMemoryCache"/> keys that are affected by the change.
///
/// Object coverage
///   Contact, Company, Deal, Ticket (all CRUD + merge + privacy deletion)
///   Associations (creation / deletion between any two object types)
///
/// Idempotency is enforced upstream in <see cref="HubSpotWebhookSyncHandler"/>;
/// this service assumes every event it receives is unique and in-order.
///
/// Current implementation — cache-invalidation tier
/// ─────────────────────────────────────────────────
/// The service invalidates IMemoryCache entries keyed by the standard patterns
/// used across the application. When the next request for that resource arrives
/// the relevant service (UserPanelService, KanbanService, etc.) will re-fetch
/// from HubSpot and re-populate the cache with fresh data.
///
/// This is intentionally thin — no local database is written. All source-of-truth
/// reads still go to HubSpot. The sync layer ensures local caches never serve
/// stale data for longer than the next request cycle.
///
/// Outbound sync (local → HubSpot) is handled by the existing repository
/// implementations (ContactRepository, DealRepository, …) which write directly
/// to HubSpot on every mutation; no additional outbound sync layer is needed.
/// </summary>
public sealed class BidirectionalSyncService
{
    private readonly IContactRepository  _contactRepo;
    private readonly IDealRepository     _dealRepo;
    private readonly IMemoryCache        _cache;
    private readonly ISyncStateRepository _syncState;
    private readonly ILogger<BidirectionalSyncService> _logger;

    // Cache keys are now centralised in CacheKeys — no local string constants needed.

    public BidirectionalSyncService(
        IContactRepository  contactRepo,
        IDealRepository     dealRepo,
        IMemoryCache        cache,
        ISyncStateRepository syncState,
        ILogger<BidirectionalSyncService> logger)
    {
        _contactRepo = contactRepo;
        _dealRepo    = dealRepo;
        _cache       = cache;
        _syncState   = syncState;
        _logger      = logger;
    }

    // ── Creation ──────────────────────────────────────────────────────────────

    public async Task HandleCreationAsync(HubSpotWebhookEvent ev, CancellationToken ct)
    {
        _logger.LogInformation(
            "Sync.Creation: {ObjectType} objectId={ObjectId}",
            ev.ObjectType, ev.ObjectId);

        await InvalidateByObjectAsync(ev, ct).ConfigureAwait(false);
    }

    // ── Deletion ──────────────────────────────────────────────────────────────

    public async Task HandleDeletionAsync(HubSpotWebhookEvent ev, CancellationToken ct)
    {
        _logger.LogInformation(
            "Sync.Deletion: {ObjectType} objectId={ObjectId}",
            ev.ObjectType, ev.ObjectId);

        var objectType = ev.ObjectType.ToString().ToLowerInvariant();
        await _syncState.MarkDeletedAsync(objectType, ev.ObjectId.ToString(), ct);
        await InvalidateByObjectAsync(ev, ct).ConfigureAwait(false);
    }

    // ── Restoration ───────────────────────────────────────────────────────────

    public async Task HandleRestorationAsync(HubSpotWebhookEvent ev, CancellationToken ct)
    {
        _logger.LogInformation(
            "Sync.Restoration: {ObjectType} objectId={ObjectId}",
            ev.ObjectType, ev.ObjectId);

        await InvalidateByObjectAsync(ev, ct).ConfigureAwait(false);
    }

    // ── Property change ───────────────────────────────────────────────────────

    public async Task HandlePropertyChangeAsync(HubSpotWebhookEvent ev, CancellationToken ct)
    {
        _logger.LogInformation(
            "Sync.PropertyChange: {ObjectType} objectId={ObjectId} property={Property} value={Value}",
            ev.ObjectType, ev.ObjectId,
            ev.PropertyName ?? "?",
            ev.PropertyValue ?? "?");

        await InvalidateByObjectAsync(ev, ct).ConfigureAwait(false);

        // For contact property changes: if this is a field shown in the user panel
        // (name, phone, wallet, plan, etc.) also invalidate the panel aggregate cache.
        if (ev.ObjectType == HubSpotObjectType.Contact)
            await InvalidateContactPanelCacheAsync(ev.ObjectId.ToString(), ct)
                .ConfigureAwait(false);
    }

    // ── Association change ────────────────────────────────────────────────────

    public async Task HandleAssociationChangeAsync(HubSpotWebhookEvent ev, CancellationToken ct)
    {
        _logger.LogInformation(
            "Sync.AssociationChange: {ObjectType} objectId={ObjectId} assocType={AssocType}",
            ev.ObjectType, ev.ObjectId, ev.AssociationType ?? "?");

        // Invalidate both sides of the association.
        await InvalidateByObjectAsync(ev, ct).ConfigureAwait(false);

        if (ev.FromObjectId.HasValue)
            await InvalidateObjectCacheAsync(
                ev.ObjectType.ToString().ToLowerInvariant(),
                ev.FromObjectId.Value.ToString(), ct).ConfigureAwait(false);

        if (ev.ToObjectId.HasValue)
            await InvalidateObjectCacheAsync(
                ev.ObjectType.ToString().ToLowerInvariant(),
                ev.ToObjectId.Value.ToString(), ct).ConfigureAwait(false);
    }

    // ── Merge ─────────────────────────────────────────────────────────────────

    public async Task HandleMergeAsync(HubSpotWebhookEvent ev, CancellationToken ct)
    {
        _logger.LogInformation(
            "Sync.Merge: {ObjectType} survivorId={ObjectId}",
            ev.ObjectType, ev.ObjectId);

        // Invalidate the survivor and any related panel states.
        await InvalidateByObjectAsync(ev, ct).ConfigureAwait(false);

        if (ev.ObjectType == HubSpotObjectType.Contact)
        {
            await InvalidateContactPanelCacheAsync(ev.ObjectId.ToString(), ct)
                .ConfigureAwait(false);
            // Also invalidate the merged-away record if we know its ID.
            if (ev.FromObjectId.HasValue)
                await InvalidateContactPanelCacheAsync(
                    ev.FromObjectId.Value.ToString(), ct).ConfigureAwait(false);
        }
    }

    // ── Privacy deletion ──────────────────────────────────────────────────────

    public async Task HandlePrivacyDeletionAsync(HubSpotWebhookEvent ev, CancellationToken ct)
    {
        _logger.LogInformation(
            "Sync.PrivacyDeletion: {ObjectType} objectId={ObjectId} — purging all local state",
            ev.ObjectType, ev.ObjectId);

        var objectType = ev.ObjectType.ToString().ToLowerInvariant();
        var objectId   = ev.ObjectId.ToString();

        await _syncState.MarkDeletedAsync(objectType, objectId, ct);
        await InvalidateObjectCacheAsync(objectType, objectId, ct);

        if (ev.ObjectType == HubSpotObjectType.Contact)
            await InvalidateContactPanelCacheAsync(objectId, ct).ConfigureAwait(false);
    }

    // ── Cache helpers ─────────────────────────────────────────────────────────

    /// <summary>Invalidates all caches related to the object in the event.</summary>
    private Task InvalidateByObjectAsync(HubSpotWebhookEvent ev, CancellationToken ct)
        => InvalidateObjectCacheAsync(
            ev.ObjectType.ToString().ToLowerInvariant(),
            ev.ObjectId.ToString(), ct);

    private Task InvalidateObjectCacheAsync(
        string objectType, string objectId, CancellationToken _)
    {
        // Generic per-object cache key used by AdminServices / KanbanService.
        _cache.Remove(NovinCRM.Application.Common.CacheKeys.CrmObject(objectType, objectId));

        // Deal changes also invalidate the kanban board cache (no contact scope).
        if (objectType == "deal")
            _cache.Remove(NovinCRM.Application.Common.CacheKeys.KanbanBoard);

        _logger.LogDebug(
            "SyncCache: invalidated {ObjectType}:{ObjectId}", objectType, objectId);

        return Task.CompletedTask;
    }

    private async Task InvalidateContactPanelCacheAsync(string contactId, CancellationToken ct)
    {
        _cache.Remove(NovinCRM.Application.Common.CacheKeys.UserPanel(contactId));
        _logger.LogDebug("SyncCache: invalidated userpanel:{ContactId}", contactId);
        await Task.CompletedTask;
    }
}
