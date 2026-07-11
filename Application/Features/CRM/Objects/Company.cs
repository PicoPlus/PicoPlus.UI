using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace PicoPlus.Services.CRM.Objects;

/// <summary>
/// HubSpot Companies API Service
/// https://developers.hubspot.com/docs/api/crm/companies
/// </summary>
public class Company
{
    private readonly HttpClient _httpClient;
    private readonly string _hubSpotToken;
    private const string BaseUrl = "/crm/v3/objects/companies";

    public Company(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;

        // Read from environment variable first, then configuration
        _hubSpotToken = Environment.GetEnvironmentVariable("HUBSPOT_TOKEN")
                        ?? configuration["HubSpot:Token"]
                        ?? throw new InvalidOperationException("HubSpot token is not configured. Set HUBSPOT_TOKEN environment variable or HubSpot:Token in appsettings.");
    }

    /// <summary>
    /// Create a new company
    /// POST /crm/v3/objects/companies
    /// </summary>
    public async Task<dynamic> Create(object companyInfo)
    {
        var json = JsonSerializer.Serialize(companyInfo);
        var request = new HttpRequestMessage(HttpMethod.Post, BaseUrl)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _hubSpotToken);

        using var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<dynamic>(responseJson);
    }

    /// <summary>
    /// Get company by ID
    /// GET /crm/v3/objects/companies/{companyId}
    /// </summary>
    public async Task<dynamic> Read(string id, string[]? properties = null, string[]? associations = null)
    {
        var queryParams = new List<string>();

        if (properties != null && properties.Length > 0)
        {
            foreach (var prop in properties)
            {
                queryParams.Add($"properties={prop}");
            }
        }

        if (associations != null && associations.Length > 0)
        {
            foreach (var assoc in associations)
            {
                queryParams.Add($"associations={assoc}");
            }
        }

        var url = queryParams.Count > 0
            ? $"{BaseUrl}/{id}?{string.Join("&", queryParams)}"
            : $"{BaseUrl}/{id}";

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _hubSpotToken);

        using var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<dynamic>(responseJson);
    }

    /// <summary>
    /// Update a company
    /// PATCH /crm/v3/objects/companies/{companyId}
    /// </summary>
    public async Task<dynamic> Update(string companyId, object updatedProperties)
    {
        var url = $"{BaseUrl}/{companyId}";

        var json = JsonSerializer.Serialize(new { properties = updatedProperties });
        var request = new HttpRequestMessage(HttpMethod.Patch, url)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _hubSpotToken);

        using var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<dynamic>(responseJson);
    }

    /// <summary>
    /// Delete a company (archive)
    /// DELETE /crm/v3/objects/companies/{companyId}
    /// </summary>
    public async Task<bool> Delete(string companyId)
    {
        var url = $"{BaseUrl}/{companyId}";

        var request = new HttpRequestMessage(HttpMethod.Delete, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _hubSpotToken);

        using var response = await _httpClient.SendAsync(request);
        return response.IsSuccessStatusCode;
    }

    /// <summary>
    /// Search companies
    /// POST /crm/v3/objects/companies/search
    /// </summary>
    public async Task<dynamic> Search(object searchRequest)
    {
        var url = $"{BaseUrl}/search";

        var json = JsonSerializer.Serialize(searchRequest);
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _hubSpotToken);

        using var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<dynamic>(responseJson);
    }

    /// <summary>
    /// Get all companies (paginated)
    /// GET /crm/v3/objects/companies
    /// </summary>
    public async Task<dynamic> GetAll(int limit = 100, string? after = null, string[]? properties = null)
    {
        var queryParams = new List<string> { $"limit={limit}" };

        if (!string.IsNullOrEmpty(after))
        {
            queryParams.Add($"after={after}");
        }

        if (properties != null && properties.Length > 0)
        {
            foreach (var prop in properties)
            {
                queryParams.Add($"properties={prop}");
            }
        }

        var url = $"{BaseUrl}?{string.Join("&", queryParams)}";

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _hubSpotToken);

        using var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<dynamic>(responseJson);
    }

    /// <summary>
    /// Batch operations
    /// </summary>
    public async Task<dynamic> BatchCreate(List<object> companies)
    {
        var url = $"{BaseUrl}/batch/create";
        var payload = new { inputs = companies };
        var json = JsonSerializer.Serialize(payload);
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _hubSpotToken);

        using var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<dynamic>(responseJson);
    }

    public async Task<dynamic> BatchUpdate(List<object> updates)
    {
        var url = $"{BaseUrl}/batch/update";
        var payload = new { inputs = updates };
        var json = JsonSerializer.Serialize(payload);
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _hubSpotToken);

        using var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<dynamic>(responseJson);
    }
}
