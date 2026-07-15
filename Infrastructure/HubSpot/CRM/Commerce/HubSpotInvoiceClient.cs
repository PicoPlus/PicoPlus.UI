#nullable enable

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NovinCRM.Models.CRM.Commerce;

namespace NovinCRM.Infrastructure.HubSpot.CRM.Commerce;

/// <summary>
/// HTTP client for the HubSpot Invoices API.
/// Registered as a typed client pointing at the "HubSpot" named HttpClient.
/// </summary>
public sealed class HubSpotInvoiceClient
{
    private readonly HttpClient                    _http;
    private readonly string                        _token;
    private readonly ILogger<HubSpotInvoiceClient> _logger;

    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition      = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public HubSpotInvoiceClient(
        HttpClient                    http,
        IConfiguration                config,
        ILogger<HubSpotInvoiceClient> logger)
    {
        _http   = http;
        _token  = Environment.GetEnvironmentVariable("HUBSPOT_TOKEN")
                  ?? config["HubSpot:Token"]
                  ?? throw new InvalidOperationException("HubSpot:Token is required.");
        _logger = logger;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Creates a new HubSpot Invoice object and returns its ID, or null on failure.</summary>
    public async Task<string?> CreateAsync(Invoice.Create.Request body, CancellationToken ct = default)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, "https://api.hubapi.com/crm/v3/objects/invoices");
        AddAuth(req);
        req.Content = JsonContent(body);

        var resp = await _http.SendAsync(req, ct);
        var raw  = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogWarning("HubSpotInvoiceClient.CreateAsync failed {Status}: {Body}", (int)resp.StatusCode, raw);
            return null;
        }

        var result = JsonSerializer.Deserialize<Invoice.Create.Response>(raw, _json);
        return result?.id;
    }

    /// <summary>Associates line items to an invoice via the v4 batch association endpoint.</summary>
    public async Task AssociateLineItemsAsync(
        string           invoiceId,
        IEnumerable<string> lineItemIds,
        CancellationToken ct = default)
    {
        var ids = lineItemIds.ToList();
        if (ids.Count == 0) return;

        var body = new Invoice.BatchAssociateRequest
        {
            inputs = ids.Select(lid => new Invoice.BatchAssociateInput
            {
                from  = new Invoice.BatchAssociateId { id = invoiceId },
                to    = new Invoice.BatchAssociateId { id = lid },
                types = new List<Invoice.Create.AssocType>
                {
                    new() { associationCategory = "HUBSPOT_DEFINED", associationTypeId = 180 }
                }
            }).ToList()
        };

        var req = new HttpRequestMessage(HttpMethod.Post,
            "https://api.hubapi.com/crm/v4/associations/invoices/line_items/batch/create");
        AddAuth(req);
        req.Content = JsonContent(body);

        var resp = await _http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode)
        {
            var raw = await resp.Content.ReadAsStringAsync(ct);
            _logger.LogWarning(
                "HubSpotInvoiceClient.AssociateLineItemsAsync failed {Status}: {Body}",
                (int)resp.StatusCode, raw);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void AddAuth(HttpRequestMessage req) =>
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);

    private static StringContent JsonContent(object body) =>
        new(JsonSerializer.Serialize(body, _json), Encoding.UTF8, "application/json");
}
