#nullable enable

using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PicoPlus.Application.Common.Interfaces;
using PicoPlus.Domain.Events.Integration;
using PicoPlus.Domain.Webhooks;
using PicoPlus.Services.SMS;

namespace PicoPlus.Application.Features.CRM.EventHandlers;

/// <summary>
/// Sends a deal-closed SMS to the associated contact when a deal is closed
/// <b>from inside this application</b> (Kanban board, API, etc.).
///
/// Trigger path:
///   DealRepository.UpdateAsync → DealClosedEvent (domain event)
///     → DealClosedHandler → DealClosedIntegrationEvent (integration event)
///       → WebhookIntegrationEventPublisher (synthetic HubSpotWebhookEvent,
///           SubscriptionType = "deal.closed")
///             → WebhookDispatcherService → THIS HANDLER → ISmsService
///
/// Only fires for <c>ClosedWon</c> — lost deals do not receive a congratulatory SMS.
/// </summary>
public sealed class DealClosedSmsHandler : IHubSpotWebhookHandler
{
    public IReadOnlySet<string>? SupportedSubscriptionTypes { get; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "deal.closed" };

    private readonly ISmsService                      _sms;
    private readonly IConfiguration                   _config;
    private readonly ILogger<DealClosedSmsHandler>    _logger;

    private static readonly JsonSerializerOptions JsonOpts =
        new() { PropertyNameCaseInsensitive = true };

    public DealClosedSmsHandler(
        ISmsService                   sms,
        IConfiguration                config,
        ILogger<DealClosedSmsHandler> logger)
    {
        _sms    = sms;
        _config = config;
        _logger = logger;
    }

    public async Task HandleAsync(HubSpotWebhookEvent ev, CancellationToken ct = default)
    {
        // ChangeSource carries the JSON-serialised DealClosedIntegrationEvent
        // (set by WebhookIntegrationEventPublisher).
        if (string.IsNullOrEmpty(ev.ChangeSource)) return;

        DealClosedIntegrationEvent? payload;
        try
        {
            payload = JsonSerializer.Deserialize<DealClosedIntegrationEvent>(
                ev.ChangeSource, JsonOpts);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex,
                "DealClosedSmsHandler: failed to deserialise payload — EventId={EventId}",
                ev.EventId);
            return;
        }

        if (payload is null) return;

        // Only send SMS for ClosedWon
        if (!string.Equals(payload.Stage, "ClosedWon", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogDebug(
                "DealClosedSmsHandler: skipping stage={Stage} for deal {DealId}",
                payload.Stage, payload.DealId);
            return;
        }

        if (string.IsNullOrEmpty(payload.ContactPhone))
        {
            _logger.LogWarning(
                "DealClosedSmsHandler: no phone for contact {ContactId} on deal {DealId} — SMS skipped",
                payload.ContactId ?? "unknown", payload.DealId);
            return;
        }

        _logger.LogInformation(
            "DealClosedSmsHandler: sending SMS to {Phone} for deal {DealId}",
            payload.ContactPhone, payload.DealId);

        var baseUrl  = _config["App:BaseUrl"]?.TrimEnd('/') ?? string.Empty;
        var dealLink = string.IsNullOrEmpty(baseUrl) ? string.Empty : $"{baseUrl}/invoice/{payload.DealId}";

        await _sms.SendDealClosedAsync(
            payload.ContactPhone,
            payload.ContactFirstName ?? string.Empty,
            payload.ContactLastName  ?? string.Empty,
            payload.DealId,
            dealLink);
    }
}
