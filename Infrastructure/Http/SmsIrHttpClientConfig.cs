using Microsoft.Extensions.Configuration;

namespace PicoPlus.Infrastructure.Http;

/// <summary>
/// Configuration for SMS.ir API HTTP client
/// </summary>
public class SmsIrHttpClientConfig
{
    public const string BaseUrl = "https://api.sms.ir";
    public const int TimeoutSeconds = 30;

    /// <summary>
    /// Create configured HTTP client for SMS.ir API
    /// </summary>
    public static HttpClient CreateClient(IConfiguration configuration)
    {
        var handler = new ShecanDnsHttpClientHandler();
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri(BaseUrl),
            Timeout = TimeSpan.FromSeconds(TimeoutSeconds)
        };

        // API key is added per-request in the service
        client.DefaultRequestHeaders.Add("Accept", "application/json");
        client.DefaultRequestHeaders.Add("User-Agent", "PicoPlus-SmsIr-Client/1.0");

        return client;
    }
}
