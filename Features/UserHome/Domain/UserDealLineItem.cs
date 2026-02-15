using PicoPlus.Models.CRM.Commerce;

namespace PicoPlus.Features.UserHome.Domain;

public sealed record UserDealLineItem(LineItem.Read.Response Model)
{
    public decimal UnitPrice => decimal.TryParse(Model.properties?.price, out var value) ? value : 0m;
    public decimal Quantity => decimal.TryParse(Model.properties?.quantity, out var value) ? value : 1m;
    public decimal Discount => decimal.TryParse(Model.properties?.hs_discount_percentage, out var value) ? value : 0m;
    public decimal Total => UnitPrice * Quantity * (1 - Discount / 100m);
}
