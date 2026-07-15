namespace NovinCRM.Infrastructure.Http;

/// <summary>
/// Delegating handler that redacts the Authorization header value in
/// <see cref="HttpRequestMessage"/> objects after the inner handler has sent
/// the request. This prevents structured-logging sinks that capture the request
/// object (e.g., Serilog's HttpClient enricher) from recording the Bearer token.
///
/// The header is present on the actual wire send — only the in-memory
/// <see cref="HttpRequestMessage"/> object is mutated post-send.
/// </summary>
public sealed class SanitizingLoggingHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);

        // Redact post-send so any logging middleware that inspects the request
        // object after the call cannot capture the token value.
        request.Headers.Remove("Authorization");

        return response;
    }
}
