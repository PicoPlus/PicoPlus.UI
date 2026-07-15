#nullable enable

namespace NovinCRM.Infrastructure.Logging;

/// <summary>
/// ASP.NET Core middleware that assigns a Correlation-ID to every HTTP request.
///
/// Behaviour:
///   1. If the request contains an <c>X-Correlation-Id</c> header, that value is used.
///   2. Otherwise a new <c>NewGuid()</c> is generated.
///   3. The ID is stored in <c>HttpContext.Items["CorrelationId"]</c>.
///   4. The ID is echoed back in the <c>X-Correlation-Id</c> response header.
///   5. A structured log scope <c>{ CorrelationId }</c> is pushed onto the logger for the
///      duration of the request so every log line in that request carries the ID automatically.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    public const string HttpHeader = "X-Correlation-Id";
    public const string ItemsKey   = "CorrelationId";

    private readonly RequestDelegate             _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(
        RequestDelegate next,
        ILogger<CorrelationIdMiddleware> logger)
    {
        _next   = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Resolve or generate correlation ID
        var correlationId = context.Request.Headers.TryGetValue(HttpHeader, out var existing)
            && !string.IsNullOrWhiteSpace(existing)
                ? existing.ToString()
                : Guid.NewGuid().ToString("N");

        // Store in Items so any downstream service can read it
        context.Items[ItemsKey] = correlationId;

        // Echo back to caller
        context.Response.OnStarting(() =>
        {
            context.Response.Headers.TryAdd(HttpHeader, correlationId);
            return Task.CompletedTask;
        });

        // Push structured log scope for the lifetime of the request
        using var scope = _logger.BeginScope(
            new Dictionary<string, object?> { [ItemsKey] = correlationId });

        await _next(context);
    }
}
