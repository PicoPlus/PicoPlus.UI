using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using PicoPlus.Models.CRM;
using PicoPlus.Models.CRM.Commerce;
using PicoPlus.Models.CRM.Objects;

namespace PicoPlus.Features.UserHome.Infrastructure;

public sealed class HubSpotClient(HttpClient httpClient) : IHubSpotClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<Associate.ListAssoc.Response?> ListAssociationsAsync(string objectId, string objectType, string toObjectType, CancellationToken cancellationToken = default)
    {
        return await httpClient.GetFromJsonAsync<Associate.ListAssoc.Response>($"/crm/v4/objects/{objectType}/{objectId}/associations/{toObjectType}?limit=500", cancellationToken);
    }

    public async Task<Deal.GetBatch.Response?> GetDealsBatchAsync(IEnumerable<string> dealIds, CancellationToken cancellationToken = default)
    {
        var request = new Deal.GetBatch.Request
        {
            inputs = dealIds.Select(id => new Deal.GetBatch.Request.Input { id = id }).ToList(),
            properties =
            [
                "dealname",
                "amount",
                "dealstage",
                "createdate",
                "hs_lastmodifieddate",
                "closedate",
                "pipeline"
            ]
        };

        using var response = await httpClient.PostAsJsonAsync("/crm/v3/objects/deals/batch/read", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Deal.GetBatch.Response>(JsonOptions, cancellationToken);
    }

    public async Task<LineItem.Read.Response?> GetLineItemAsync(string lineItemId, CancellationToken cancellationToken = default)
    {
        return await httpClient.GetFromJsonAsync<LineItem.Read.Response>($"/crm/v3/objects/line_items/{lineItemId}", cancellationToken);
    }

    public async Task<Contact.Read.Response?> ReadContactAsync(string contactId, IEnumerable<string> properties, CancellationToken cancellationToken = default)
    {
        var query = string.Join("&", properties.Select(p => $"properties={Uri.EscapeDataString(p)}"));
        return await httpClient.GetFromJsonAsync<Contact.Read.Response>($"/crm/v3/objects/contacts/{contactId}?{query}", cancellationToken);
    }

    public async Task UpdateContactPropertiesAsync(string contactId, Dictionary<string, string> properties, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Serialize(new { properties });
        using var content = new StringContent(payload, Encoding.UTF8, "application/json");
        using var response = await httpClient.PatchAsync($"/crm/v3/objects/contacts/{contactId}", content, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
