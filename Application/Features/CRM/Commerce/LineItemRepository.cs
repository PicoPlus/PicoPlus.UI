#nullable enable

using PicoPlus.Application.Common.Interfaces;

namespace PicoPlus.Services.CRM.Commerce;

/// <summary>
/// Implements ILineItemRepository by delegating to the existing LineItem HTTP service.
/// </summary>
public class LineItemRepository : ILineItemRepository
{
    private readonly LineItem _lineItem;
    public LineItemRepository(LineItem lineItem) => _lineItem = lineItem;

    public async Task<Domain.Entities.LineItem?> GetByIdAsync(string id)
    {
        var r = await _lineItem.GetLineItem(id);
        if (r?.properties == null) return null;

        return new Domain.Entities.LineItem
        {
            Id                  = r.id ?? id,
            Name                = r.properties.name ?? string.Empty,
            Price               = decimal.TryParse(r.properties.price,    out var p) ? p : 0m,
            Quantity            = long.TryParse(r.properties.quantity,    out var q) ? q : 1L,
            DiscountPercentage  = decimal.TryParse(r.properties.hs_discount_percentage, out var d) ? d : 0m,
            ProductId           = r.properties.hs_product_id,
            CreatedAt           = r.createdAt,
            UpdatedAt           = r.updatedAt
        };
    }

    public async Task<IReadOnlyList<string>> CreateBatchAsync(
        IEnumerable<Domain.Entities.LineItem> lineItems, string? dealId = null)
    {
        var inputs = lineItems.Select(li => new Models.CRM.Commerce.LineItem.Create.Request.Input
        {
            properties = new Models.CRM.Commerce.LineItem.Create.Request.Properties
            {
                name                   = li.Name,
                price                  = li.Price,
                quantity               = li.Quantity,
                hs_product_id          = li.ProductId,
                hs_sku                 = li.Sku,
                hs_discount_percentage = li.DiscountPercentage > 0
                                            ? li.DiscountPercentage.ToString("0.##")
                                            : null,
                amount                 = li.DiscountPercentage > 0
                                            ? (li.Price * li.Quantity * (1 - li.DiscountPercentage / 100m)).ToString("0.##")
                                            : (li.Price * li.Quantity).ToString("0.##")
            }
        }).ToList();

        var req  = new Models.CRM.Commerce.LineItem.Create.Request { inputs = inputs };
        var resp = await _lineItem.CreateLineAsync(req);

        return resp?.results?
                   .Select(r => r.id ?? string.Empty)
                   .Where(id => !string.IsNullOrEmpty(id))
                   .ToList()
               ?? (IReadOnlyList<string>)Array.Empty<string>();
    }
}
