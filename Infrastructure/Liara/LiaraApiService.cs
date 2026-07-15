using System.Net.Http.Headers;
using System.Text.Json;
using NovinCRM.Models.Services.Liara;

namespace NovinCRM.Services.Utils;

public class LiaraApiService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<LiaraApiService> _logger;

    public LiaraApiService(
        HttpClient httpClient, 
        IConfiguration configuration,
        ILogger<LiaraApiService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string?> GetCurrentBuildVersionAsync()
    {
        try
        {
            var token = _configuration["Liara:ApiToken"]
                ?? throw new InvalidOperationException(
                    "Liara API token is not configured. Set Liara:ApiToken in configuration or LIARA_API_TOKEN environment variable.");

            var projectId = _configuration["Liara:ProjectId"]
                ?? throw new InvalidOperationException(
                    "Liara project ID is not configured. Set Liara:ProjectId in configuration.");
            var url = $"https://api.iran.liara.ir/v1/projects/{projectId}/releases?page=1&count=10";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch Liara releases. Status: {StatusCode}", response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var releaseResponse = JsonSerializer.Deserialize<LiaraReleaseResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (releaseResponse?.Releases == null || !releaseResponse.Releases.Any())
            {
                return null;
            }

            // Find the active release (state == "READY" and matches currentRelease ID)
            var activeRelease = releaseResponse.Releases
                .FirstOrDefault(r => r.State == "READY" && r.Id == releaseResponse.CurrentRelease);

            if (activeRelease != null)
            {
                return activeRelease.Tag; // Returns tag like "v70"
            }

            // Fallback: find first READY release
            var firstReadyRelease = releaseResponse.Releases.FirstOrDefault(r => r.State == "READY");
            return firstReadyRelease?.Tag;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Liara build version");
            return null;
        }
    }

    public async Task<LiaraReleaseResponse?> GetReleasesAsync()
    {
        try
        {
            var token = _configuration["Liara:ApiToken"]
                ?? throw new InvalidOperationException(
                    "Liara API token is not configured. Set Liara:ApiToken in configuration or LIARA_API_TOKEN environment variable.");

            var projectId = _configuration["Liara:ProjectId"]
                ?? throw new InvalidOperationException(
                    "Liara project ID is not configured. Set Liara:ProjectId in configuration.");
            var url = $"https://api.iran.liara.ir/v1/projects/{projectId}/releases?page=1&count=10";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch Liara releases. Status: {StatusCode}", response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<LiaraReleaseResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Liara releases");
            return null;
        }
    }
}
