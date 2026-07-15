#nullable enable

namespace NovinCRM.Application.Common.Interfaces;

/// <summary>
/// Creates HubSpot Invoice objects for completed deals and associates line items to them.
/// </summary>
public interface IInvoiceService
{
    /// <summary>
    /// Creates a HubSpot Invoice for the given deal and associates all attached
    /// line items to it.  Returns the new invoice ID, or null on failure.
    /// Failure is intentionally non-fatal — the deal is never rolled back.
    /// </summary>
    Task<string?> CreateForDealAsync(
        string    dealId,
        string    dealName,
        DateTime? closeDate,
        CancellationToken ct = default);
}
