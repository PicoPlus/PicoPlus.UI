#nullable enable

using NovinCRM.Domain.Entities;

namespace NovinCRM.Application.Common.Interfaces;

/// <summary>
/// Application-layer contract for HubSpot Deal operations.
/// </summary>
public interface IDealRepository
{
    Task<Deal?> GetByIdAsync(string id);
    Task<IReadOnlyList<Deal>> GetBatchAsync(IEnumerable<string> ids);
    Task<Deal> CreateAsync(Deal deal, string? contactId = null, IEnumerable<string>? lineItemIds = null, string? stageName = null);
    Task<Deal> UpdateAsync(string dealId, Dictionary<string, string> properties);
    Task<bool> DeleteAsync(string dealId);
}
