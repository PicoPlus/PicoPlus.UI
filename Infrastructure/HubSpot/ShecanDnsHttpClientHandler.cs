using System.Net;

namespace NovinCRM.Infrastructure.Http;

/// <summary>
/// Default HttpClientHandler for outbound API calls.
/// Configures decompression and redirect behaviour.
/// TLS certificate validation uses the platform trust store (OS/runtime) —
/// no bypass is applied and no custom DNS override is performed.
/// </summary>
/// <remarks>
/// Previously named <c>ShecanDnsHttpClientHandler</c> when it was used to
/// route traffic through Shecan DNS. The DNS override has been removed;
/// the class now performs only standard decompression and redirect config.
/// Renamed to <c>DefaultApiHttpClientHandler</c> (closes #73 / LP-1).
/// </remarks>
public class DefaultApiHttpClientHandler : HttpClientHandler
{
    public DefaultApiHttpClientHandler()
    {
        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
        AllowAutoRedirect = true;
        MaxAutomaticRedirections = 10;
        // ServerCertificateCustomValidationCallback is intentionally NOT set —
        // the runtime validates the server certificate against the OS trust store.
    }
}

/// <summary>
/// Backward-compatibility alias — existing code that references
/// <see cref="ShecanDnsHttpClientHandler"/> continues to compile.
/// New code should use <see cref="DefaultApiHttpClientHandler"/> directly.
/// </summary>
[Obsolete("Use DefaultApiHttpClientHandler instead. This alias will be removed in a future release.")]
public class ShecanDnsHttpClientHandler : DefaultApiHttpClientHandler { }
