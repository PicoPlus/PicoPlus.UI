using System.Text.Json.Serialization;

namespace PicoPlus.Domain.Webhooks;

/// <summary>
/// A single event within a HubSpot webhook batch payload.
///
/// HubSpot sends an array of these objects as the HTTP request body.
/// The array may contain events for different object types and event types
/// in the same delivery.
///
/// Reference: https://developers.hubspot.com/docs/api/webhooks#webhook-payload
/// </summary>
public sealed class HubSpotWebhookEvent
{
    // ── Identity ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Unique identifier for this specific event delivery.
    /// Used for idempotency / replay-attack detection.
    /// </summary>
    [JsonPropertyName("eventId")]
    public long EventId { get; init; }

    /// <summary>
    /// Identifier for the webhook subscription that triggered this event.
    /// </summary>
    [JsonPropertyName("subscriptionId")]
    public long SubscriptionId { get; init; }

    /// <summary>
    /// HubSpot Portal (Hub) ID — identifies which HubSpot account sent this.
    /// </summary>
    [JsonPropertyName("portalId")]
    public long PortalId { get; init; }

    /// <summary>
    /// HubSpot App ID of the private app / OAuth app that owns the subscription.
    /// </summary>
    [JsonPropertyName("appId")]
    public long AppId { get; init; }

    // ── Timing ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Unix epoch milliseconds when the event occurred in HubSpot.
    /// </summary>
    [JsonPropertyName("occurredAt")]
    public long OccurredAt { get; init; }

    /// <summary>
    /// Zero-indexed delivery attempt number.
    /// 0 = first attempt; HubSpot retries up to ~10 times with back-off.
    /// </summary>
    [JsonPropertyName("attemptNumber")]
    public int AttemptNumber { get; init; }

    // ── Routing ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Full HubSpot subscription type string, e.g. "contact.propertyChange",
    /// "deal.creation", "company.deletion". Parsed into
    /// <see cref="ObjectType"/> and <see cref="EventType"/> for routing.
    /// </summary>
    [JsonPropertyName("subscriptionType")]
    public string SubscriptionType { get; init; } = string.Empty;

    // ── Record ────────────────────────────────────────────────────────────────

    /// <summary>ID of the CRM record that triggered this event.</summary>
    [JsonPropertyName("objectId")]
    public long ObjectId { get; init; }

    // ── Property change payload (populated when EventType == PropertyChange) ─

    [JsonPropertyName("propertyName")]
    public string? PropertyName { get; init; }

    [JsonPropertyName("propertyValue")]
    public string? PropertyValue { get; init; }

    [JsonPropertyName("changeSource")]
    public string? ChangeSource { get; init; }

    // ── Association change payload ────────────────────────────────────────────

    [JsonPropertyName("associationType")]
    public string? AssociationType { get; init; }

    [JsonPropertyName("fromObjectId")]
    public long? FromObjectId { get; init; }

    [JsonPropertyName("toObjectId")]
    public long? ToObjectId { get; init; }

    // ── Parsed routing helpers (not in JSON payload) ──────────────────────────

    /// <summary>Parsed from <see cref="SubscriptionType"/> by the verifier.</summary>
    [JsonIgnore]
    public HubSpotObjectType ObjectType { get; init; }

    /// <summary>Parsed from <see cref="SubscriptionType"/> by the verifier.</summary>
    [JsonIgnore]
    public HubSpotEventType EventType { get; init; }

    /// <summary>Convenient UTC timestamp derived from <see cref="OccurredAt"/>.</summary>
    [JsonIgnore]
    public DateTimeOffset OccurredAtUtc => DateTimeOffset.FromUnixTimeMilliseconds(OccurredAt);
}
