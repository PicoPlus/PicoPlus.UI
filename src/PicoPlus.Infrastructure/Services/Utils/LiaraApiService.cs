using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Text.Json;
using PicoPlus.Models.Services.Liara;

namespace PicoPlus.Services.Utils;

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
                ?? "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VySUQiOiI2ODZkNGMwMTA4ZmZlMWFjOTBmZGJmMzIiLCJ0eXBlIjoiYXV0aCIsImlhdCI6MTc2NDY0ODQyMSwiZXhwIjoxNzY3MjQwNDIxfQ.79sQq8T51TuAR5oq1WMAQNjBXSIyaG-Pd9_zdSl-hvY";
            
            var projectId = _configuration["Liara:ProjectId"] ?? "ipicoplus";
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
                ?? "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VySUQiOiI2ODZkNGMwMTA4ZmZlMWFjOTBmZGJmMzIiLCJ0eXBlIjoiYXV0aCIsImlhdCI6MTc2NDY0ODQyMSwiZXhwIjoxNzY3MjQwNDIxfQ.79sQq8T51TuAR5oq1WMAQNjBXSIyaG-Pd9_zdSl-hvY";
            
            var projectId = _configuration["Liara:ProjectId"] ?? "ipicoplus";
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
