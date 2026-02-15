using PicoPlus.Models.CRM.Objects;

namespace PicoPlus.Features.UserHome.Domain;

public sealed record UserDeal(Deal.GetBatch.Response.Result Model)
{
    public string Id => Model.id;
    public string Name => Model.properties?.dealname ?? "-";
    public string Stage => Model.properties?.dealstage ?? string.Empty;
    public decimal Amount => decimal.TryParse(Model.properties?.amount, out var amount) ? amount : 0m;
}
