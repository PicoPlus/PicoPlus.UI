using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace PicoPlus.Services.CRM.Objects;

/// <summary>
/// HubSpot Tickets API Service
/// https://developers.hubspot.com/docs/api/crm/tickets
/// </summary>
public class Ticket
{
    private readonly HttpClient _httpClient;
    private readonly string _hubSpotToken;
    private const string BaseUrl = "/crm/v3/objects/tickets";

    public Ticket(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;

        // Read from environment variable first, then configuration
        _hubSpotToken = Environment.GetEnvironmentVariable("HUBSPOT_TOKEN")
                        ?? configuration["HubSpot:Token"]
                        ?? throw new InvalidOperationException("HubSpot token is not configured. Set HUBSPOT_TOKEN environment variable or HubSpot:Token in appsettings.");
    }

    public async Task<dynamic> Create(object ticketInfo)
    {
        var json = JsonSerializer.Serialize(ticketInfo);
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

    public async Task<dynamic> Update(string ticketId, object updatedProperties)
    {
        var url = $"{BaseUrl}/{ticketId}";

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

    public async Task<bool> Delete(string ticketId)
    {
        var url = $"{BaseUrl}/{ticketId}";

        var request = new HttpRequestMessage(HttpMethod.Delete, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _hubSpotToken);

        using var response = await _httpClient.SendAsync(request);
        return response.IsSuccessStatusCode;
    }

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

    public async Task<dynamic> BatchCreate(List<object> tickets)
    {
        var url = $"{BaseUrl}/batch/create";
        var payload = new { inputs = tickets };
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
