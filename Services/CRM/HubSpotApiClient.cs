using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace PicoPlus.Services.CRM;

public class HubSpotApiClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _hubSpotToken;

    public HubSpotApiClient(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _hubSpotToken = Environment.GetEnvironmentVariable("HUBSPOT_TOKEN")
                        ?? configuration["HubSpot:Token"]
                        ?? throw new InvalidOperationException("HubSpot token is not configured. Set HUBSPOT_TOKEN environment variable or HubSpot:Token in appsettings.");
    }

    public async Task<TResponse?> SendAsync<TResponse>(HttpMethod method, string url, object? payload = null)
    {
        var httpClient = _httpClientFactory.CreateClient("HubSpot");
        using var request = new HttpRequestMessage(method, url);

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _hubSpotToken);

        if (payload is not null)
        {
            var json = JsonSerializer.Serialize(payload);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        using var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        if (typeof(TResponse) == typeof(VoidResult))
        {
            return default;
        }

        var responseJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<TResponse>(responseJson);
    }

    public async Task<bool> SendForStatusAsync(HttpMethod method, string url, object? payload = null)
    {
        var httpClient = _httpClientFactory.CreateClient("HubSpot");
        using var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _hubSpotToken);

        if (payload is not null)
        {
            var json = JsonSerializer.Serialize(payload);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        using var response = await httpClient.SendAsync(request);
        return response.IsSuccessStatusCode;
    }

    public sealed class VoidResult;
}
