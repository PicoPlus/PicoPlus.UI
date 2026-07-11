namespace PicoPlus.Infrastructure.Http;

/// <summary>
/// Configuration for HubSpot API clients
/// </summary>
public class HubSpotHttpClientConfig
{
    public const string BaseUrl = "https://api.hubapi.com";
    public const int TimeoutSeconds = 30;

    public static HttpClient CreateClient(IConfiguration configuration)
    {
        var handler = new ShecanDnsHttpClientHandler();
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri(BaseUrl),
            Timeout = TimeSpan.FromSeconds(TimeoutSeconds)
        };

        var token = configuration["HubSpot:Token"];
        if (!string.IsNullOrEmpty(token))
        {
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        }

        client.DefaultRequestHeaders.Add("Accept", "application/json");

        return client;
    }
}
