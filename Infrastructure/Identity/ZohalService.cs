#nullable enable

using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace PicoPlus.Services.Identity;

/// <summary>
/// Zohal Inquiry Services — national identity and Shahkar endpoints.
/// Base URL: https://service.zohal.io/api/v0
/// Docs: https://service.zohal.io
/// </summary>
public class ZohalService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ZohalService> _logger;
    private readonly string _token;

    private const string BaseUrl = "https://service.zohal.io/api/v0";

    public ZohalService(HttpClient httpClient, IConfiguration configuration, ILogger<ZohalService> logger)
    {
        _httpClient = httpClient;
        _logger     = logger;

        _token = Environment.GetEnvironmentVariable("ZOHAL_TOKEN")
                 ?? configuration["Zohal:Token"]
                 ?? throw new InvalidOperationException(
                     "Zohal token is not configured. Set ZOHAL_TOKEN environment variable or Zohal:Token in appsettings.");
    }

    // ── Generic POST helper ───────────────────────────────────────────────────

    private async Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest body)
    {
        var url     = $"{BaseUrl}/{endpoint}";
        var json    = JsonConvert.SerializeObject(body);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);

        _logger.LogDebug("Zohal POST {Endpoint}", endpoint);

        var response = await _httpClient.SendAsync(request);
        var raw      = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            _logger.LogWarning("Zohal {Endpoint} failed HTTP {Status}", endpoint, response.StatusCode);
        else
            _logger.LogDebug("Zohal {Endpoint} responded successfully", endpoint);

        return JsonConvert.DeserializeObject<TResponse>(raw);
    }

    // ── National Identity Inquiry — استعلام اطلاعات هویتی ───────────────────

    public Task<ZohalResponse<NationalIdentityData>?> NationalIdentityInquiryAsync(
        string nationalCode, string birthDate)
        => PostAsync<object, ZohalResponse<NationalIdentityData>>(
            "services/inquiry/national_identity_inquiry",
            new { national_code = nationalCode, birth_date = birthDate });

    // ── Shahkar — شاهکار (تطابق کد ملی و موبایل) ────────────────────────────

    public Task<ZohalResponse<ShahkarData>?> ShahkarInquiryAsync(
        string nationalCode, string mobile)
        => PostAsync<object, ZohalResponse<ShahkarData>>(
            "services/inquiry/shahkar",
            new { national_code = nationalCode, mobile });
}

// ── Shared response envelope ─────────────────────────────────────────────────

public class ZohalResponse<T>
{
    [JsonProperty("response_body")]
    public ZohalResponseBody<T>? ResponseBody { get; set; }

    [JsonProperty("result")]
    public int Result { get; set; }
}

public class ZohalResponseBody<T>
{
    [JsonProperty("data")]
    public T? Data { get; set; }

    [JsonProperty("error_code")]
    public string? ErrorCode { get; set; }

    [JsonProperty("message")]
    public string? Message { get; set; }
}

// ── Domain DTOs ───────────────────────────────────────────────────────────────

public class NationalIdentityData
{
    [JsonProperty("matched")]
    public bool Matched { get; set; }

    [JsonProperty("first_name")]
    public string? FirstName { get; set; }

    [JsonProperty("last_name")]
    public string? LastName { get; set; }

    [JsonProperty("father_name")]
    public string? FatherName { get; set; }

    [JsonProperty("national_code")]
    public string? NationalCode { get; set; }

    [JsonProperty("alive")]
    public bool? Alive { get; set; }

    [JsonProperty("is_dead")]
    public bool? IsDead { get; set; }
}

public class ShahkarData
{
    [JsonProperty("matched")]
    public bool Matched { get; set; }
}
