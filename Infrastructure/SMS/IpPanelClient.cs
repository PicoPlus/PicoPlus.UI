#nullable enable

using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NovinCRM.SMS.Models.IPPanel;

namespace NovinCRM.Services.SMS;

/// <summary>
/// Low-level IPPanel Edge API HTTP client.
/// Base URL: https://edge.ippanel.com/v1
/// Auth: Bearer API key in Authorization header.
/// </summary>
public class IpPanelClient
{
    private readonly HttpClient              _http;
    private readonly ILogger<IpPanelClient> _logger;
    private readonly string                  _apiKey;

    public const string BaseUrl = "https://edge.ippanel.com/v1";

    public IpPanelClient(HttpClient http, IConfiguration config, ILogger<IpPanelClient> logger)
    {
        _http   = http;
        _logger = logger;

        _apiKey = config["IPPanel:ApiKey"]
               ?? throw new InvalidOperationException(
                   "IPPanel API key not configured. Set IPPanel:ApiKey in appsettings.");
    }

    // ── Public send methods ───────────────────────────────────────────────────

    /// <summary>Send an OTP/notification via pattern code.</summary>
    public Task<SendPatternResponse> SendPatternAsync(SendPatternRequest request, CancellationToken ct = default)
        => PostAsync<SendPatternRequest, SendPatternResponse>("/api/send", request, ct);

    /// <summary>Send a plain-text SMS.</summary>
    public Task<SendResponse> SendAsync(SendRequest request, CancellationToken ct = default)
        => PostAsync<SendRequest, SendResponse>("/api/send", request, ct);

    /// <summary>Get account credit balance.</summary>
    public Task<CreditResponse> GetCreditAsync(CancellationToken ct = default)
        => GetAsync<CreditResponse>("/package/credit", ct);

    /// <summary>Validate the configured API key and return user info. Use for diagnostics.</summary>
    public Task<CheckTokenResponse> CheckTokenAsync(CancellationToken ct = default)
        => PostAsync<object, CheckTokenResponse>("/api/acl/auth/check_token", new { }, ct);

    // ── Private HTTP helpers ──────────────────────────────────────────────────

    private async Task<TRes> PostAsync<TReq, TRes>(string path, TReq body, CancellationToken ct)
        where TReq : class
    {
        var msg = BuildMessage(HttpMethod.Post, path);
        var json = JsonConvert.SerializeObject(body, new JsonSerializerSettings
            { NullValueHandling = NullValueHandling.Ignore });
        msg.Content = new StringContent(json, Encoding.UTF8, "application/json");

        _logger.LogInformation("IPPanel POST → {Url} | body: {Body}", msg.RequestUri?.AbsoluteUri, json);
        return await SendAsync<TRes>(msg, ct);
    }

    private async Task<TRes> GetAsync<TRes>(string path, CancellationToken ct)
    {
        var msg = BuildMessage(HttpMethod.Get, path);
        return await SendAsync<TRes>(msg, ct);
    }

    private HttpRequestMessage BuildMessage(HttpMethod method, string path)
    {
        var msg = new HttpRequestMessage(method, BaseUrl + path);
        // IPPanel expects the raw API key with no scheme prefix: Authorization: <key>
        msg.Headers.TryAddWithoutValidation("Authorization", _apiKey);
        msg.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return msg;
    }

    private async Task<TRes> SendAsync<TRes>(HttpRequestMessage msg, CancellationToken ct)
    {
        try
        {
            var response = await _http.SendAsync(msg, ct);
            var raw      = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "IPPanel {Method} {Path} → HTTP {Status}\nRequest URL : {Url}\nResponse    : {Body}",
                    msg.Method,
                    msg.RequestUri?.PathAndQuery,
                    (int)response.StatusCode,
                    msg.RequestUri?.AbsoluteUri,
                    raw);
            }

            // The new /api/send endpoint returns 200 even for logical errors,
            // so deserialise first and let the caller check meta.status.
            if (response.IsSuccessStatusCode)
                return JsonConvert.DeserializeObject<TRes>(raw)!;

            response.EnsureSuccessStatusCode(); // throws for 4xx/5xx
            return default!; // unreachable
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "IPPanel request error — URL: {Url}", msg.RequestUri?.AbsoluteUri);
            throw;
        }
    }
}
