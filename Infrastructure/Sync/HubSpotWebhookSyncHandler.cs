#nullable enable

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NovinCRM.Application.Common.Interfaces;
using NovinCRM.Domain.Webhooks;

namespace NovinCRM.Infrastructure.Sync;

/// <summary>
/// Routes every verified inbound HubSpot webhook event to the
/// <see cref="BidirectionalSyncService"/> for processing.
///
/// This is the bridge between the webhook pipeline and the sync layer.
/// It handles all object types and all event types defined in
/// <see cref="HubSpotEventType"/> and <see cref="HubSpotObjectType"/>.
///
/// Idempotency contract:
///   Every event is checked against <see cref="ISyncStateRepository"/>
///   BEFORE being forwarded. Duplicates and out-of-order events are
///   discarded here — the sync service never sees them.
/// </summary>
public sealed class HubSpotWebhookSyncHandler : IHubSpotWebhookHandler
{
    // null = subscribe to ALL event types
    public IReadOnlySet<string>? SupportedSubscriptionTypes => null;

    private readonly BidirectionalSyncService  _sync;
    private readonly ISyncStateRepository      _state;
    private readonly IMemoryCache              _cache;
    private readonly ILogger<HubSpotWebhookSyncHandler> _logger;

    public HubSpotWebhookSyncHandler(
        BidirectionalSyncService  sync,
        ISyncStateRepository      state,
        IMemoryCache              cache,
        ILogger<HubSpotWebhookSyncHandler> logger)
    {
        _sync   = sync;
        _state  = state;
        _cache  = cache;
        _logger = logger;
    }

    public async Task HandleAsync(HubSpotWebhookEvent ev, CancellationToken ct = default)
    {
        // ── 1. Skip synthetic integration events published by this system ────
        if (ev.PortalId == 0) return;

        // ── 2. Duplicate detection ────────────────────────────────────────────
        var eventKey = $"{ev.EventId}:{ev.PortalId}";
        if (await _state.IsProcessedAsync(eventKey, ct))
        {
            _logger.LogDebug(
                "SyncHandler: duplicate event skipped — EventId={EventId} Type={Type}",
                ev.EventId, ev.SubscriptionType);
            return;
        }

        var objectType = ev.ObjectType.ToString().ToLowerInvariant();
        var objectId   = ev.ObjectId.ToString();

        // ── 3. Out-of-order detection (property-change events carry a version) ─
        if (ev.EventType == HubSpotEventType.PropertyChange)
        {
            var storedVersion = await _state.GetVersionAsync(objectType, objectId, ct);
            if (storedVersion.HasValue && ev.OccurredAt <= storedVersion.Value)
            {
                _logger.LogDebug(
                    "SyncHandler: out-of-order event discarded — " +
                    "EventId={EventId} OccurredAt={OccurredAt} StoredVersion={Stored}",
                    ev.EventId, ev.OccurredAt, storedVersion.Value);
                return;
            }
        }

        // ── 4. Route to sync service ──────────────────────────────────────────
        try
        {
            await RouteAsync(ev, objectType, objectId, ct).ConfigureAwait(false);

            // Record that we processed this event at this version.
            await _state.SetVersionAsync(
                objectType, objectId, ev.OccurredAt, eventKey, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "SyncHandler: error processing EventId={EventId} Type={Type} ObjectId={ObjectId}",
                ev.EventId, ev.SubscriptionType, objectId);
            throw; // let the dispatcher retry
        }
    }

    public bool IsFatalException(Exception ex)
        // ArgumentException / InvalidOperationException from validation = fatal (no retry).
        => ex is ArgumentException or InvalidOperationException;

    // ── Routing ───────────────────────────────────────────────────────────────

    private Task RouteAsync(
        HubSpotWebhookEvent ev,
        string objectType, string objectId,
        CancellationToken ct)
        => ev.EventType switch
        {
            HubSpotEventType.Creation        => _sync.HandleCreationAsync(ev, ct),
            HubSpotEventType.Deletion        => _sync.HandleDeletionAsync(ev, ct),
            HubSpotEventType.Restoration     => _sync.HandleRestorationAsync(ev, ct),
            HubSpotEventType.PropertyChange  => _sync.HandlePropertyChangeAsync(ev, ct),
            HubSpotEventType.AssociationChange => _sync.HandleAssociationChangeAsync(ev, ct),
            HubSpotEventType.Merge           => _sync.HandleMergeAsync(ev, ct),
            HubSpotEventType.PrivacyDeletion => _sync.HandlePrivacyDeletionAsync(ev, ct),
            _ => HandleUnknownAsync(ev)
        };

    private Task HandleUnknownAsync(HubSpotWebhookEvent ev)
    {
        _logger.LogWarning(
            "SyncHandler: unrecognised event type — SubscriptionType={Type} EventId={EventId}",
            ev.SubscriptionType, ev.EventId);
        return Task.CompletedTask;
    }
}
