#nullable enable

using PicoPlus.Application.Common.Interfaces;
using PicoPlus.Domain.Entities;
using PicoPlus.Domain.Enums;
using PicoPlus.Domain.Extensions;

namespace PicoPlus.Services.CRM.Objects;

/// <summary>
/// Implements IDealRepository by delegating to the existing Deal service.
/// </summary>
public class DealRepository : IDealRepository
{
    private readonly Deal _deal;

    public DealRepository(Deal deal) => _deal = deal;

    public async Task<Domain.Entities.Deal?> GetByIdAsync(string id)
    {
        var r = await _deal.GetDeal(id, ["dealname", "amount", "dealstage", "pipeline",
                                          "closedate", "createdate", "hs_lastmodifieddate"]);
        if (r == null) return null;
        // Map from Get.Response.Properties (different type from GetBatch.Response.Properties)
        return new Domain.Entities.Deal
        {
            Id        = r.id,
            DealName  = r.properties?.dealname  ?? string.Empty,
            Amount    = decimal.TryParse(r.properties?.amount, out var a) ? a : 0,
            Stage     = (r.properties?.dealstage ?? string.Empty).ParseDealStage(),
            Pipeline  = r.properties?.pipeline,
            CreatedAt = r.properties?.createdate ?? default,
            CloseDate = r.properties?.closedate
        };
    }

    public async Task<IReadOnlyList<Domain.Entities.Deal>> GetBatchAsync(IEnumerable<string> ids)
    {
        var req = new Models.CRM.Objects.Deal.GetBatch.Request
        {
            inputs     = ids.Select(i => new Models.CRM.Objects.Deal.GetBatch.Request.Input { id = i }).ToList(),
            properties = ["dealname", "amount", "dealstage", "pipeline",
                          "closedate", "createdate", "hs_lastmodifieddate"]
        };
        var resp = await _deal.GetDeals(req);
        return resp?.results?.Select(r => Map(r.id, r.properties)).ToList()
               ?? (IReadOnlyList<Domain.Entities.Deal>)Array.Empty<Domain.Entities.Deal>();
    }

    public async Task<Domain.Entities.Deal> CreateAsync(Domain.Entities.Deal deal,
        string? contactId = null, IEnumerable<string>? lineItemIds = null, string? stageName = null)
    {
        var associations = new List<Models.CRM.Objects.Deal.Create.Request.Association>();
        if (contactId != null)
            associations.Add(new() { to = new() { id = long.Parse(contactId) },
                                     types = [new() { associationCategory = "HUBSPOT_DEFINED", associationTypeId = 3 }] });

        // Use provided stageName (from UI) if available, otherwise convert from enum
        var dealStage = !string.IsNullOrEmpty(stageName) ? stageName : deal.Stage.ToString().ToLowerInvariant();

        var req = new Models.CRM.Objects.Deal.Create.Request
        {
            properties = new()
            {
                dealname = deal.DealName,
                amount   = deal.Amount.ToString(),
                dealstage = dealStage,
                pipeline  = deal.Pipeline,
                closedate = deal.CloseDate.HasValue
                    ? new DateTimeOffset(deal.CloseDate.Value, TimeZoneInfo.Local.GetUtcOffset(deal.CloseDate.Value)).ToUnixTimeMilliseconds()
                    : DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            },
            associations = associations
        };
        var resp = await _deal.Create(req);
        return deal with { Id = resp.id };
    }

    public async Task<Domain.Entities.Deal> UpdateAsync(string dealId, Dictionary<string, string> properties)
    {
        var resp = await _deal.Update(dealId, properties);
        return new Domain.Entities.Deal
        {
            Id       = dealId,
            DealName = properties.GetValueOrDefault("dealname") ?? string.Empty,
            Stage    = (properties.GetValueOrDefault("dealstage") ?? string.Empty).ParseDealStage()
        };
    }

    public async Task<bool> DeleteAsync(string dealId) => await _deal.Delete(dealId);

    private static Domain.Entities.Deal Map(string id, Models.CRM.Objects.Deal.GetBatch.Response.Properties? p) => new()
    {
        Id        = id,
        DealName  = p?.dealname  ?? string.Empty,
        Amount    = decimal.TryParse(p?.amount, out var a) ? a : 0,
        Stage     = (p?.dealstage ?? string.Empty).ParseDealStage(),
        Pipeline  = null,
        CreatedAt = DateTime.TryParse(p?.createdate, out var c) ? c : default,
        UpdatedAt = DateTime.TryParse(p?.hs_lastmodifieddate, out var u) ? u : null
    };
}
