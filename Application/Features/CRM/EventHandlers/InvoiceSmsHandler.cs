#nullable enable

using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NovinCRM.Application.Common.Interfaces;
using NovinCRM.Domain.Events.Deal;
using NovinCRM.Services.SMS;

namespace NovinCRM.Application.Features.CRM.EventHandlers;

/// <summary>
/// Sends an invoice-review SMS when a deal moves to a configured trigger stage.
/// Issues a single-use token (72h TTL) and embeds the link in the SMS.
///
/// Trigger: <see cref="DealClosedEvent"/> (or extend to DealStageChangedEvent).
/// Config key: InvoiceReview:TriggerStages (comma-separated, default "ClosedWon").
/// </summary>
public sealed class InvoiceSmsHandler : INotificationHandler<DealClosedEvent>
{
    private readonly IContactRepository              _contacts;
    private readonly ISmsService                     _sms;
    private readonly IInvoiceAccessTokenRepository   _tokenRepo;
    private readonly IConfiguration                  _config;
    private readonly ILogger<InvoiceSmsHandler>      _logger;

    public InvoiceSmsHandler(
        IContactRepository            contacts,
        ISmsService                   sms,
        IInvoiceAccessTokenRepository tokenRepo,
        IConfiguration                config,
        ILogger<InvoiceSmsHandler>    logger)
    {
        _contacts  = contacts;
        _sms       = sms;
        _tokenRepo = tokenRepo;
        _config    = config;
        _logger    = logger;
    }

    public async Task Handle(DealClosedEvent notification, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(notification.ContactId)) return;

        // ── 1. Check trigger stages ───────────────────────────────────────────
        var triggerStages = (_config["InvoiceReview:TriggerStages"] ?? "ClosedWon")
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        var stage = notification.Stage.ToString();
        if (!triggerStages.Any(s => string.Equals(s, stage, StringComparison.OrdinalIgnoreCase)))
        {
            _logger.LogDebug(
                "InvoiceSmsHandler: stage {Stage} is not in trigger list — skipping deal {DealId}",
                stage, notification.DealId);
            return;
        }

        // ── 2. Load contact ───────────────────────────────────────────────────
        var contact = await _contacts.GetByIdAsync(notification.ContactId);
        if (contact is null || string.IsNullOrEmpty(contact.Phone))
        {
            _logger.LogWarning(
                "InvoiceSmsHandler: contact {ContactId} not found or has no phone — SMS skipped for deal {DealId}",
                notification.ContactId, notification.DealId);
            return;
        }

        // ── 3. Issue token ────────────────────────────────────────────────────
        var ttlHours = int.TryParse(_config["InvoiceReview:TokenTtlHours"], out var h) ? h : 72;
        var token    = await _tokenRepo.IssueAsync(
            notification.DealId, notification.ContactId, TimeSpan.FromHours(ttlHours));

        var baseUrl     = _config["InvoiceReview:BaseUrl"]?.TrimEnd('/')
                          ?? _config["App:BaseUrl"]?.TrimEnd('/')
                          ?? string.Empty;
        var invoiceLink = $"{baseUrl}/invoice/{notification.DealId}?token={token}";

        // ── 4. Send SMS ───────────────────────────────────────────────────────
        _logger.LogInformation(
            "InvoiceSmsHandler: sending order-review SMS to {Phone} for deal {DealId}",
            contact.Phone, notification.DealId);

        try
        {
            await _sms.SendOrderReviewAsync(contact.Phone, contact.FirstName ?? string.Empty, invoiceLink);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "InvoiceSmsHandler: SMS failed for deal {DealId} contact {ContactId}",
                notification.DealId, notification.ContactId);
            // Non-fatal — token is issued but link won't reach the customer.
            // Operator can resend manually from HubSpot.
        }
    }
}
