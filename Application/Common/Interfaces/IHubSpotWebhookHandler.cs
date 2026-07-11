#nullable enable

using PicoPlus.Domain.Webhooks;

namespace PicoPlus.Application.Common.Interfaces;

/// <summary>
/// Processes a single, already-verified HubSpot webhook event.
///
/// Implementations live in Application\Features or Infrastructure and contain
/// the actual business logic (e.g. sync a contact, update a deal stage).
/// The endpoint dispatches every event to <see cref="IWebhookEventQueue"/>
/// immediately, so this handler runs off the HTTP request thread.
///
/// Multiple handlers can be registered; the dispatcher calls each in order
/// for a given event.
/// </summary>
public interface IHubSpotWebhookHandler
{
    /// <summary>
    /// Subscription type strings this handler wants to receive, e.g.
    /// <c>{ "contact.creation", "contact.propertyChange" }</c>.
    /// Return <c>null</c> to receive every event (catch-all handler).
    /// </summary>
    IReadOnlySet<string>? SupportedSubscriptionTypes { get; }

    /// <summary>
    /// Handle the event. Should not throw — log and absorb exceptions internally
    /// so that other handlers in the chain are not interrupted.
    /// </summary>
    Task HandleAsync(HubSpotWebhookEvent webhookEvent, CancellationToken ct = default);

    /// <summary>
    /// Determines whether the exception raised by <see cref="HandleAsync"/> is
    /// "fatal" — meaning the event should NOT be retried (e.g. validation errors,
    /// duplicate key, permanent business-rule violations).
    ///
    /// Defaults to <c>false</c> — all exceptions are retryable unless overridden.
    /// </summary>
    bool IsFatalException(Exception ex) => false;
}
