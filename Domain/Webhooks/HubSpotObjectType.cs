namespace PicoPlus.Domain.Webhooks;

/// <summary>
/// Every CRM object type that HubSpot can send webhook events for.
/// String values match the <c>subscriptionType</c> prefix sent by HubSpot.
/// See: https://developers.hubspot.com/docs/api/webhooks
/// </summary>
public enum HubSpotObjectType
{
    Unknown = 0,

    // Standard objects
    Contact,
    Company,
    Deal,
    Ticket,
    Product,
    LineItem,
    Quote,

    // Engagement objects
    Call,
    Note,
    Meeting,
    Task,
    Email,

    // Commerce
    Order,
    Cart,
    Invoice,
    PaymentLink,

    // Custom object — subscriptionType prefix is "object"
    CustomObject
}
