#nullable enable

using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NovinCRM.Application.Common.Interfaces;
using NovinCRM.Infrastructure.Persistence;
using NovinCRM.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace NovinCRM.Infrastructure.Backup;

/// <summary>
/// Pulls records modified today from HubSpot and upserts them into SQL Server.
/// Only contacts/deals with hs_lastmodifieddate >= today 00:00 UTC are fetched.
/// </summary>
public sealed class HubSpotBackupService : IHubSpotBackupService
{
    private readonly IDbContextFactory<NovinBackupDbContext> _dbFactory;
    private readonly HttpClient                              _http;
    private readonly string                                  _token;
    private readonly int                                     _pageSize;
    private readonly ILogger<HubSpotBackupService>           _logger;

    private static readonly JsonSerializerOptions _json =
        new() { PropertyNameCaseInsensitive = true };

    public HubSpotBackupService(
        IDbContextFactory<NovinBackupDbContext> dbFactory,
        IHttpClientFactory                      httpFactory,
        IConfiguration                          config,
        ILogger<HubSpotBackupService>           logger)
    {
        _dbFactory = dbFactory;
        _http      = httpFactory.CreateClient("HubSpot");
        _token     = Environment.GetEnvironmentVariable("HUBSPOT_TOKEN")
                     ?? config["HubSpot:Token"]
                     ?? throw new InvalidOperationException("HubSpot:Token required.");
        _pageSize  = int.TryParse(config["Backup:PageSize"], out var ps) ? ps : 100;
        _logger    = logger;
    }

    public async Task<BackupResult> RunDailyBackupAsync(DateOnly date, CancellationToken ct = default)
    {
        var todayUtc = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var snapshot = DateTime.UtcNow;

        int contacts = 0, deals = 0, lineItems = 0, associations = 0, notes = 0;

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        // ── 1. Contacts ───────────────────────────────────────────────────────
        contacts = await UpsertContactsAsync(db, todayUtc, snapshot, ct);
        _logger.LogInformation("Backup: {Count} contacts upserted", contacts);

        // ── 2. Deals ──────────────────────────────────────────────────────────
        var dealIds = new List<string>();
        deals = await UpsertDealsAsync(db, todayUtc, snapshot, dealIds, ct);
        _logger.LogInformation("Backup: {Count} deals upserted", deals);

        // ── 3. Line items (for today's deals) ────────────────────────────────
        foreach (var dealId in dealIds)
        {
            lineItems += await UpsertLineItemsForDealAsync(db, dealId, snapshot, ct);
        }
        _logger.LogInformation("Backup: {Count} line items upserted", lineItems);

        // ── 4. Associations ───────────────────────────────────────────────────
        foreach (var dealId in dealIds)
        {
            associations += await UpsertAssociationsAsync(db, dealId, snapshot, ct);
        }
        _logger.LogInformation("Backup: {Count} associations upserted", associations);

        await db.SaveChangesAsync(ct);

        return new BackupResult(contacts, deals, lineItems, associations, notes,
            Success: true);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task<int> UpsertContactsAsync(
        NovinBackupDbContext db,
        DateTime             since,
        DateTime             snapshot,
        CancellationToken    ct)
    {
        int count = 0;
        string? after = null;

        do
        {
            var url = $"https://api.hubapi.com/crm/v3/objects/contacts/search";
            var body = BuildSearchPayload(since, _pageSize, after,
                "firstname", "lastname", "phone", "email",
                "hs_national_code", "gender",
                "createdate", "hs_lastmodifieddate");

            var resp = await PostSearchAsync(url, body, ct);
            if (resp == null) break;

            foreach (var r in resp.Results ?? Enumerable.Empty<SearchResult>())
            {
                var id = r.Id ?? string.Empty;
                var p  = r.Properties;
                var existing = await db.Contacts.FindAsync(new object[] { id }, ct);
                var row = new BackupContact
                {
                    HubSpotId    = id,
                    FirstName    = p?.GetValueOrDefault("firstname"),
                    LastName     = p?.GetValueOrDefault("lastname"),
                    Phone        = p?.GetValueOrDefault("phone"),
                    Email        = p?.GetValueOrDefault("email"),
                    NationalCode = p?.GetValueOrDefault("hs_national_code"),
                    Gender       = p?.GetValueOrDefault("gender"),
                    HsCreatedAt  = ParseDate(p?.GetValueOrDefault("createdate")),
                    HsUpdatedAt  = ParseDate(p?.GetValueOrDefault("hs_lastmodifieddate")),
                    SnapshotAt   = snapshot
                };

                if (existing is null) db.Contacts.Add(row);
                else db.Entry(existing).CurrentValues.SetValues(row);
                count++;
            }

            after = resp.Paging?.Next?.After;
        } while (after != null);

        return count;
    }

    private async Task<int> UpsertDealsAsync(
        NovinBackupDbContext db,
        DateTime             since,
        DateTime             snapshot,
        List<string>         dealIds,
        CancellationToken    ct)
    {
        int count = 0;
        string? after = null;

        do
        {
            var url  = "https://api.hubapi.com/crm/v3/objects/deals/search";
            var body = BuildSearchPayload(since, _pageSize, after,
                "dealname", "amount", "dealstage", "pipeline",
                "createdate", "closedate", "hs_lastmodifieddate");

            var resp = await PostSearchAsync(url, body, ct);
            if (resp == null) break;

            foreach (var r in resp.Results ?? Enumerable.Empty<SearchResult>())
            {
                var id = r.Id ?? string.Empty;
                dealIds.Add(id);
                var p = r.Properties;

                var row = new BackupDeal
                {
                    HubSpotId   = id,
                    DealName    = p?.GetValueOrDefault("dealname"),
                    Amount      = decimal.TryParse(p?.GetValueOrDefault("amount"), out var amt) ? amt : 0m,
                    Stage       = p?.GetValueOrDefault("dealstage"),
                    Pipeline    = p?.GetValueOrDefault("pipeline"),
                    HsCreatedAt = ParseDate(p?.GetValueOrDefault("createdate")),
                    HsCloseDate = TryParseDate(p?.GetValueOrDefault("closedate")),
                    SnapshotAt  = snapshot
                };

                var existing = await db.Deals.FindAsync(new object[] { id }, ct);
                if (existing is null) db.Deals.Add(row);
                else db.Entry(existing).CurrentValues.SetValues(row);
                count++;
            }

            after = resp.Paging?.Next?.After;
        } while (after != null);

        return count;
    }

    private async Task<int> UpsertLineItemsForDealAsync(
        NovinBackupDbContext db,
        string              dealId,
        DateTime            snapshot,
        CancellationToken   ct)
    {
        int count = 0;
        var url = $"https://api.hubapi.com/crm/v3/objects/line_items?" +
                  $"properties=name&properties=price&properties=quantity" +
                  $"&properties=hs_discount_percentage&properties=hs_sku&archived=false";

        // Fetch line items via deal associations
        var assocUrl = $"https://api.hubapi.com/crm/v4/objects/deals/{dealId}/associations/line_items";
        var assocResp = await GetAsync<AssocResponse>(assocUrl, ct);

        foreach (var item in assocResp?.Results ?? Enumerable.Empty<AssocIdResult>())
        {
            var liId  = item.ToObjectId?.ToString() ?? item.Id?.ToString() ?? string.Empty;
            if (string.IsNullOrEmpty(liId)) continue;

            var liResp = await GetAsync<SingleObjectResponse>(
                $"https://api.hubapi.com/crm/v3/objects/line_items/{liId}?properties=name&properties=price&properties=quantity&properties=hs_discount_percentage&properties=hs_sku", ct);

            if (liResp == null) continue;
            var p = liResp.Properties;

            var row = new BackupLineItem
            {
                HubSpotId          = liId,
                DealId             = dealId,
                Name               = p?.GetValueOrDefault("name"),
                Price              = decimal.TryParse(p?.GetValueOrDefault("price"), out var pr) ? pr : 0m,
                Quantity           = long.TryParse(p?.GetValueOrDefault("quantity"), out var q) ? q : 1L,
                DiscountPercentage = decimal.TryParse(p?.GetValueOrDefault("hs_discount_percentage"), out var d) ? d : 0m,
                Sku                = p?.GetValueOrDefault("hs_sku"),
                SnapshotAt         = snapshot
            };

            var existing = await db.LineItems.FindAsync(new object[] { liId }, ct);
            if (existing is null) db.LineItems.Add(row);
            else db.Entry(existing).CurrentValues.SetValues(row);
            count++;
        }

        return count;
    }

    private async Task<int> UpsertAssociationsAsync(
        NovinBackupDbContext db,
        string              dealId,
        DateTime            snapshot,
        CancellationToken   ct)
    {
        int count = 0;
        var url = $"https://api.hubapi.com/crm/v4/objects/deals/{dealId}/associations/contacts";
        var resp = await GetAsync<AssocResponse>(url, ct);

        foreach (var item in resp?.Results ?? Enumerable.Empty<AssocIdResult>())
        {
            var contactId = item.ToObjectId?.ToString() ?? item.Id?.ToString() ?? string.Empty;
            if (string.IsNullOrEmpty(contactId)) continue;

            // Upsert via composite key — ignore if duplicate
            var exists = await db.Associations.AnyAsync(
                a => a.FromObjectType == "deal" && a.FromId == dealId
                  && a.ToObjectType   == "contact" && a.ToId == contactId, ct);

            if (!exists)
            {
                db.Associations.Add(new BackupAssociation
                {
                    FromObjectType = "deal",
                    FromId         = dealId,
                    ToObjectType   = "contact",
                    ToId           = contactId,
                    SnapshotAt     = snapshot
                });
                count++;
            }
        }

        return count;
    }

    // ── HTTP helpers ──────────────────────────────────────────────────────────

    private async Task<SearchResponse?> PostSearchAsync(string url, object body, CancellationToken ct)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        req.Content = new StringContent(JsonSerializer.Serialize(body), System.Text.Encoding.UTF8, "application/json");
        var resp = await _http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode) { _logger.LogWarning("Search {Url} → {Status}", url, (int)resp.StatusCode); return null; }
        return JsonSerializer.Deserialize<SearchResponse>(await resp.Content.ReadAsStringAsync(ct), _json);
    }

    private async Task<T?> GetAsync<T>(string url, CancellationToken ct)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        var resp = await _http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode) return default;
        return JsonSerializer.Deserialize<T>(await resp.Content.ReadAsStringAsync(ct), _json);
    }

    private static object BuildSearchPayload(DateTime since, int limit, string? after, params string[] props)
    {
        var payload = new Dictionary<string, object>
        {
            ["filterGroups"] = new[]
            {
                new
                {
                    filters = new[]
                    {
                        new
                        {
                            propertyName = "hs_lastmodifieddate",
                            @operator    = "GTE",
                            value        = since.ToString("o")
                        }
                    }
                }
            },
            ["properties"] = props,
            ["limit"]       = limit
        };
        if (after != null) payload["after"] = after;
        return payload;
    }

    private static DateTime  ParseDate(string?    s) => DateTime.TryParse(s, out var d) ? d : DateTime.MinValue;
    private static DateTime? TryParseDate(string? s) => DateTime.TryParse(s, out var d) ? d : (DateTime?)null;

    // ── Mini DTOs for JSON deserialization ─────────────────────────────────────
    private sealed class SearchResponse
    {
        public List<SearchResult>? Results { get; set; }
        public Paging?             Paging  { get; set; }
    }
    private sealed class SearchResult
    {
        public string?                       Id         { get; set; }
        public Dictionary<string, string?>?  Properties { get; set; }
    }
    private sealed class Paging   { public PagingNext? Next { get; set; } }
    private sealed class PagingNext { public string? After { get; set; } }

    private sealed class AssocResponse { public List<AssocIdResult>? Results { get; set; } }
    private sealed class AssocIdResult
    {
        public string? Id           { get; set; }
        public long?   ToObjectId   { get; set; }
    }
    private sealed class SingleObjectResponse
    {
        public string?                       Id         { get; set; }
        public Dictionary<string, string?>?  Properties { get; set; }
    }
}
