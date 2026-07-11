#nullable enable

using PicoPlus.Domain.Entities;

namespace PicoPlus.Application.Common.Interfaces;

/// <summary>
/// Application-layer contract for HubSpot LineItem operations.
/// </summary>
public interface ILineItemRepository
{
    Task<LineItem?> GetByIdAsync(string id);
    Task<IReadOnlyList<string>> CreateBatchAsync(IEnumerable<LineItem> lineItems, string? dealId = null);
}
