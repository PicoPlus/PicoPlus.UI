#nullable enable

using System.Text.Json;
using Microsoft.Extensions.Logging;
using NovinCRM.Application.Common.Interfaces;
using NovinCRM.Domain.Events;
using NovinCRM.Domain.Webhooks;

namespace NovinCRM.Infrastructure.Events;

/// <summary>
/// Publishes integration events by converting them into synthetic
/// <see cref="HubSpotWebhookEvent"/> records and enqueuing them into
/// the existing <see cref="IWebhookEventQueue"/> pipeline.
///
/// This reuses the channel-backed queue, retry logic, background dispatcher,
/// and all registered <see cref="IHubSpotWebhookHandler"/> implementations
/// without any new infrastructure.
///
/// Mapping:
///   IIntegrationEvent.EventType  → HubSpotWebhookEvent.SubscriptionType
///   IIntegrationEvent.EventId    → HubSpotWebhookEvent.EventId (truncated to long)
///   Payload is JSON-serialised into a dedicated "integrationPayload" property
///   via a custom field on the synthetic event.
///
/// Handlers that want to consume integration events subscribe to the
/// EventType string (e.g. "contact.registered") in their
/// <see cref="IHubSpotWebhookHandler.SupportedSubscriptionTypes"/>.
/// </summary>
public sealed class WebhookIntegrationEventPublisher : IIntegrationEventPublisher
{
    private readonly IWebhookEventQueue                _queue;
    private readonly ILogger<WebhookIntegrationEventPublisher> _logger;

    private static readonly JsonSerializerOptions JsonOptions =
        new() { WriteIndented = false };

    // Portal ID used to distinguish internally-published events from real HubSpot events.
    private const long InternalPortalId = 0L;

    public WebhookIntegrationEventPublisher(
        IWebhookEventQueue queue,
        ILogger<WebhookIntegrationEventPublisher> logger)
    {
        _queue  = queue;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task PublishAsync(
        IIntegrationEvent integrationEvent,
        CancellationToken ct = default)
    {
        // Synthesise a HubSpotWebhookEvent so integration events travel
        // through exactly the same pipeline as inbound webhooks.
        var synthetic = new HubSpotWebhookEvent
        {
            // Truncate Guid to long for the EventId field.
            EventId          = Math.Abs(BitConverter.ToInt64(
                                   integrationEvent.EventId.ToByteArray(), 0)),
            SubscriptionId   = 0,
            PortalId         = InternalPortalId,
            AppId            = 0,
            OccurredAt       = integrationEvent.OccurredAt.ToUnixTimeMilliseconds(),
            AttemptNumber    = 0,
            SubscriptionType = integrationEvent.EventType,
            ObjectId         = 0,
            // Store the serialised payload in ChangeSource for handler inspection.
            ChangeSource     = JsonSerializer.Serialize(
                                   integrationEvent, integrationEvent.GetType(), JsonOptions),
        };

        _logger.LogDebug(
            "IntegrationEventPublisher: enqueuing {EventType} (EventId={EventId})",
            integrationEvent.EventType, integrationEvent.EventId);

        await _queue.WriteAsync([synthetic], ct).ConfigureAwait(false);
    }
}
