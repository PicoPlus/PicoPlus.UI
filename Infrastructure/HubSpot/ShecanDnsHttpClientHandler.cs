using System.Net;

namespace PicoPlus.Infrastructure.Http;

/// <summary>
/// Custom HttpClientHandler that configures compression and redirect behaviour
/// for outbound API calls. TLS certificate validation uses the platform default
/// (i.e., the OS/runtime trust store) — no bypass is applied.
/// </summary>
public class ShecanDnsHttpClientHandler : HttpClientHandler
{
    public ShecanDnsHttpClientHandler()
    {
        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
        AllowAutoRedirect = true;
        MaxAutomaticRedirections = 10;
        // ServerCertificateCustomValidationCallback is intentionally NOT set —
        // the runtime validates the server certificate against the OS trust store.
    }
}
