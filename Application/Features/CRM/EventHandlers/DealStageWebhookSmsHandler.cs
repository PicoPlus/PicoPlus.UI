#nullable enable

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PicoPlus.Application.Common.Interfaces;
using PicoPlus.Domain.Webhooks;
using PicoPlus.Services.SMS;

namespace PicoPlus.Application.Features.CRM.EventHandlers;

/// <summary>
/// Sends a deal-closed SMS to the associated contact when a deal is moved to
/// <c>closedwon</c> <b>directly inside HubSpot</b> (by a sales rep, workflow, etc.).
///
/// Trigger path:
///   HubSpot → POST /webhooks/hubspot
///     (deal.propertyChange, propertyName=dealstage, propertyValue=closedwon)
///     → InMemoryWebhookEventQueue
///       → WebhookDispatcherService → THIS HANDLER
///         → IAssociateService (deal → contact IDs)
///           → IContactRepository (fetch phone + name)
///             → ISmsService.SendDealClosedAsync
///
/// The <see cref="HubSpotWebhookSyncHandler"/> (catch-all, PortalId≠0) also
/// processes the same event for cache invalidation; both handlers run independently.
/// </summary>
public sealed class DealStageWebhookSmsHandler : IHubSpotWebhookHandler
{
    // Only interested in deal property-change events
    public IReadOnlySet<string>? SupportedSubscriptionTypes { get; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "deal.propertyChange" };

    private readonly IAssociateService                       _associate;
    private readonly IContactRepository                      _contacts;
    private readonly ISmsService                             _sms;
    private readonly IConfiguration                          _config;
    private readonly ILogger<DealStageWebhookSmsHandler>     _logger;

    public DealStageWebhookSmsHandler(
        IAssociateService                   associate,
        IContactRepository                  contacts,
        ISmsService                         sms,
        IConfiguration                      config,
        ILogger<DealStageWebhookSmsHandler> logger)
    {
        _associate = associate;
        _contacts  = contacts;
        _sms       = sms;
        _config    = config;
        _logger    = logger;
    }

    public async Task HandleAsync(HubSpotWebhookEvent ev, CancellationToken ct = default)
    {
        // Only react to the dealstage property becoming closedwon
        if (!string.Equals(ev.PropertyName, "dealstage", StringComparison.OrdinalIgnoreCase))
            return;
        if (!string.Equals(ev.PropertyValue, "closedwon", StringComparison.OrdinalIgnoreCase))
            return;

        var dealId = ev.ObjectId.ToString();

        _logger.LogInformation(
            "DealStageWebhookSmsHandler: deal {DealId} moved to closedwon — looking up contact",
            dealId);

        // ── 1. Resolve associated contact IDs ─────────────────────────────────
        IReadOnlyList<string> contactIds;
        try
        {
            contactIds = await _associate.GetAssociatedIdsAsync(dealId, "deal", "contact");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "DealStageWebhookSmsHandler: failed to fetch associations for deal {DealId}",
                dealId);
            throw; // let the dispatcher retry
        }

        if (contactIds.Count == 0)
        {
            _logger.LogWarning(
                "DealStageWebhookSmsHandler: deal {DealId} has no associated contact — SMS skipped",
                dealId);
            return;
        }

        // ── 2. Fetch the first (primary) contact ──────────────────────────────
        var contact = await _contacts.GetByIdAsync(contactIds[0]);
        if (contact is null)
        {
            _logger.LogWarning(
                "DealStageWebhookSmsHandler: contact {ContactId} not found — SMS skipped",
                contactIds[0]);
            return;
        }

        if (string.IsNullOrEmpty(contact.Phone))
        {
            _logger.LogWarning(
                "DealStageWebhookSmsHandler: contact {ContactId} has no phone — SMS skipped",
                contact.Id);
            return;
        }

        // ── 3. Send SMS ────────────────────────────────────────────────────────
        _logger.LogInformation(
            "DealStageWebhookSmsHandler: sending SMS to {Phone} (contact {ContactId}) for deal {DealId}",
            contact.Phone, contact.Id, dealId);

        var baseUrl  = _config["App:BaseUrl"]?.TrimEnd('/') ?? string.Empty;
        var dealLink = string.IsNullOrEmpty(baseUrl) ? string.Empty : $"{baseUrl}/invoice/{dealId}";

        await _sms.SendDealClosedAsync(
            contact.Phone,
            contact.FirstName,
            contact.LastName,
            dealId,
            dealLink);
    }
}
