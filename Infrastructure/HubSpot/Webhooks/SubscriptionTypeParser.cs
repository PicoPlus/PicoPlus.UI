#nullable enable

using PicoPlus.Domain.Webhooks;

namespace PicoPlus.Infrastructure.Webhooks;

/// <summary>
/// Parses a HubSpot <c>subscriptionType</c> string such as
/// <c>"contact.propertyChange"</c> into typed
/// <see cref="HubSpotObjectType"/> and <see cref="HubSpotEventType"/> values,
/// and stamps the parsed results onto the event's mutable companion fields.
///
/// subscriptionType format:  {objectPrefix}.{eventVerb}
/// Examples:
///   contact.creation        → Contact + Creation
///   deal.propertyChange     → Deal    + PropertyChange
///   company.deletion        → Company + Deletion
///   object.propertyChange   → CustomObject + PropertyChange   (custom objects)
/// </summary>
public static class SubscriptionTypeParser
{
    // ── Object prefix → enum ──────────────────────────────────────────────────

    private static readonly Dictionary<string, HubSpotObjectType> ObjectPrefixes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["contact"]     = HubSpotObjectType.Contact,
            ["company"]     = HubSpotObjectType.Company,
            ["deal"]        = HubSpotObjectType.Deal,
            ["ticket"]      = HubSpotObjectType.Ticket,
            ["product"]     = HubSpotObjectType.Product,
            ["line_item"]   = HubSpotObjectType.LineItem,
            ["quote"]       = HubSpotObjectType.Quote,
            ["call"]        = HubSpotObjectType.Call,
            ["note"]        = HubSpotObjectType.Note,
            ["meeting"]     = HubSpotObjectType.Meeting,
            ["task"]        = HubSpotObjectType.Task,
            ["email"]       = HubSpotObjectType.Email,
            ["order"]       = HubSpotObjectType.Order,
            ["cart"]        = HubSpotObjectType.Cart,
            ["invoice"]     = HubSpotObjectType.Invoice,
            ["payment_link"]= HubSpotObjectType.PaymentLink,
            ["object"]      = HubSpotObjectType.CustomObject,   // custom objects
        };

    // ── Event verb → enum ─────────────────────────────────────────────────────

    private static readonly Dictionary<string, HubSpotEventType> EventVerbs =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["creation"]        = HubSpotEventType.Creation,
            ["deletion"]        = HubSpotEventType.Deletion,
            ["restore"]         = HubSpotEventType.Restoration,
            ["restoration"]     = HubSpotEventType.Restoration,
            ["propertyChange"]  = HubSpotEventType.PropertyChange,
            ["associationChange"]= HubSpotEventType.AssociationChange,
            ["privacyDeletion"] = HubSpotEventType.PrivacyDeletion,
            ["merge"]           = HubSpotEventType.Merge,
        };

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Parses <paramref name="ev.SubscriptionType"/> and returns a new instance
    /// with <see cref="HubSpotWebhookEvent.ObjectType"/> and
    /// <see cref="HubSpotWebhookEvent.EventType"/> populated.
    /// Unrecognised parts map to the <c>Unknown</c> enum values.
    /// </summary>
    public static HubSpotWebhookEvent WithParsedType(HubSpotWebhookEvent ev)
    {
        var (objectType, eventType) = Parse(ev.SubscriptionType);
        return new HubSpotWebhookEvent
        {
            EventId          = ev.EventId,
            SubscriptionId   = ev.SubscriptionId,
            PortalId         = ev.PortalId,
            AppId            = ev.AppId,
            OccurredAt       = ev.OccurredAt,
            AttemptNumber    = ev.AttemptNumber,
            SubscriptionType = ev.SubscriptionType,
            ObjectId         = ev.ObjectId,
            PropertyName     = ev.PropertyName,
            PropertyValue    = ev.PropertyValue,
            ChangeSource     = ev.ChangeSource,
            AssociationType  = ev.AssociationType,
            FromObjectId     = ev.FromObjectId,
            ToObjectId       = ev.ToObjectId,
            ObjectType       = objectType,
            EventType        = eventType,
        };
    }

    /// <summary>
    /// Parses a raw subscriptionType string into its component enum values.
    /// </summary>
    public static (HubSpotObjectType objectType, HubSpotEventType eventType)
        Parse(string subscriptionType)
    {
        if (string.IsNullOrWhiteSpace(subscriptionType))
            return (HubSpotObjectType.Unknown, HubSpotEventType.Unknown);

        var dotIndex = subscriptionType.IndexOf('.');
        if (dotIndex <= 0 || dotIndex == subscriptionType.Length - 1)
            return (HubSpotObjectType.Unknown, HubSpotEventType.Unknown);

        var prefix = subscriptionType[..dotIndex];
        var verb   = subscriptionType[(dotIndex + 1)..];

        var objectType = ObjectPrefixes.TryGetValue(prefix, out var ot)
            ? ot : HubSpotObjectType.Unknown;

        var eventType = EventVerbs.TryGetValue(verb, out var et)
            ? et : HubSpotEventType.Unknown;

        return (objectType, eventType);
    }
}
