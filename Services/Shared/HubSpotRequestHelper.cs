using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace PicoPlus.Services.Shared;

/// <summary>
/// Shared helper for building and sending HubSpot API requests,
/// eliminating repeated boilerplate across CRM service classes.
/// </summary>
public static class HubSpotRequestHelper
{
    private static readonly JsonSerializerOptions IgnoreNullOptions = new()
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly JsonSerializerOptions CaseInsensitiveOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Send a POST request with a JSON body and deserialize the response.
    /// </summary>
    public static async Task<T> PostAsync<T>(HttpClient httpClient, string url, object payload, string token)
    {
        var json = JsonSerializer.Serialize(payload, IgnoreNullOptions);
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        return await SendAndDeserializeAsync<T>(httpClient, request, token);
    }

    /// <summary>
    /// Send a GET request and deserialize the response.
    /// </summary>
    public static async Task<T> GetAsync<T>(HttpClient httpClient, string url, string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        return await SendAndDeserializeAsync<T>(httpClient, request, token);
    }

    /// <summary>
    /// Send a PATCH request with a JSON body and deserialize the response.
    /// </summary>
    public static async Task<T> PatchAsync<T>(HttpClient httpClient, string url, object payload, string token)
    {
        var json = JsonSerializer.Serialize(payload, IgnoreNullOptions);
        using var request = new HttpRequestMessage(HttpMethod.Patch, url)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        return await SendAndDeserializeAsync<T>(httpClient, request, token);
    }

    /// <summary>
    /// Send a PATCH request with a JSON body (no response body expected).
    /// </summary>
    public static async Task PatchAsync(HttpClient httpClient, string url, object payload, string token)
    {
        var json = JsonSerializer.Serialize(payload, IgnoreNullOptions);
        using var request = new HttpRequestMessage(HttpMethod.Patch, url)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Send a DELETE request and return success status.
    /// </summary>
    public static async Task<bool> DeleteAsync(HttpClient httpClient, string url, string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await httpClient.SendAsync(request);
        return response.IsSuccessStatusCode;
    }

    private static async Task<T> SendAndDeserializeAsync<T>(HttpClient httpClient, HttpRequestMessage request, string token)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(responseJson, CaseInsensitiveOptions)!;
    }
}
