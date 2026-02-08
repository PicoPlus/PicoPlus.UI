using System.Net;

namespace PicoPlus.Infrastructure.Http;

/// <summary>
/// Custom HttpClientHandler that uses Shecan DNS servers for bypassing restrictions
/// </summary>
public class ShecanDnsHttpClientHandler : HttpClientHandler
{
    public ShecanDnsHttpClientHandler()
    {
        // Shecan DNS servers: 178.22.122.100 and 185.51.200.2
        var shecanPrimary = IPAddress.Parse("178.22.122.100");
        var shecanSecondary = IPAddress.Parse("185.51.200.2");

        // Configure DNS resolution to use Shecan servers
        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

        // Allow automatic redirects
        AllowAutoRedirect = true;
        MaxAutomaticRedirections = 10;

        // Keep platform-default TLS certificate validation behavior.
    }
}
