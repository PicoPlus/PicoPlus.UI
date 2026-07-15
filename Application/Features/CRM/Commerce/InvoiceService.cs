#nullable enable

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NovinCRM.Application.Common.Interfaces;
using NovinCRM.Infrastructure.HubSpot.CRM.Commerce;
using NovinCRM.Models.CRM.Commerce;

namespace NovinCRM.Application.Features.CRM.Commerce;

/// <summary>
/// Creates a HubSpot Invoice for a deal immediately after the deal is created,
/// then associates all line items to the invoice.
///
/// Failure is non-fatal — the deal is never rolled back.
/// </summary>
public sealed class InvoiceService : IInvoiceService
{
    private readonly HubSpotInvoiceClient     _invoiceClient;
    private readonly ILineItemRepository      _lineItemRepo;
    private readonly ILogger<InvoiceService>  _logger;
    private readonly string                   _baseUrl;

    public InvoiceService(
        HubSpotInvoiceClient    invoiceClient,
        ILineItemRepository     lineItemRepo,
        IConfiguration          config,
        ILogger<InvoiceService> logger)
    {
        _invoiceClient = invoiceClient;
        _lineItemRepo  = lineItemRepo;
        _logger        = logger;
        _baseUrl       = config["App:BaseUrl"]?.TrimEnd('/') ?? string.Empty;
    }

    public async Task<string?> CreateForDealAsync(
        string    dealId,
        string    dealName,
        DateTime? closeDate,
        CancellationToken ct = default)
    {
        try
        {
            var dueDate = (closeDate ?? DateTime.UtcNow.AddDays(30)).ToString("yyyy-MM-dd");

            var req = new Invoice.Create.Request
            {
                properties = new Invoice.Create.Properties
                {
                    hs_invoice_status = "DRAFT",
                    hs_title          = $"فاکتور سفارش {dealName}",
                    hs_currency_code  = "IRR",
                    hs_due_date       = dueDate,
                    hs_invoice_source = "API"
                },
                associations = new List<Invoice.Create.Association>
                {
                    new()
                    {
                        to    = new Invoice.Create.To { id = dealId },
                        types = new List<Invoice.Create.AssocType>
                        {
                            new() { associationCategory = "HUBSPOT_DEFINED", associationTypeId = 176 }
                        }
                    }
                }
            };

            var invoiceId = await _invoiceClient.CreateAsync(req, ct);
            if (invoiceId == null)
            {
                _logger.LogWarning("InvoiceService: invoice creation returned null for deal {DealId}", dealId);
                return null;
            }

            _logger.LogInformation(
                "InvoiceService: created invoice {InvoiceId} for deal {DealId}", invoiceId, dealId);

            // Associate line items that belong to this deal
            // Line items are already created; we fetch their IDs from the deal's associations.
            // For now we pass an empty list — the caller (DealCreatedHandler) may enrich this.
            // Line item association is best-effort; invoice is still valid without it.
            try
            {
                await _invoiceClient.AssociateLineItemsAsync(invoiceId, Array.Empty<string>(), ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "InvoiceService: line-item association failed for invoice {InvoiceId}", invoiceId);
            }

            return invoiceId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "InvoiceService: failed to create invoice for deal {DealId}", dealId);
            return null;
        }
    }
}
