#nullable enable

namespace NovinCRM.Infrastructure.Logging;

/// <summary>
/// Service that provides the current request's correlation ID.
/// Inject this into application services and event handlers that need to propagate
/// the correlation ID across async boundaries (e.g. webhook handlers, MediatR handlers).
///
/// Usage:
///   using var scope = _correlationIdService.BeginScope(logger);
///   // all log calls inside this using block carry { CorrelationId }
/// </summary>
public sealed class CorrelationIdService
{
    private static readonly AsyncLocal<string?> _current = new();

    /// <summary>Gets or sets the current correlation ID for the execution context.</summary>
    public string? Current
    {
        get => _current.Value;
        set => _current.Value = value;
    }

    /// <summary>
    /// Returns the current correlation ID, or generates a new one if none is set.
    /// </summary>
    public string GetOrCreate()
        => _current.Value ??= Guid.NewGuid().ToString("N");

    /// <summary>
    /// Begins an <see cref="ILogger"/> scope that injects <c>CorrelationId</c>
    /// as a structured property. The scope is disposed when the returned
    /// <see cref="IDisposable"/> is disposed.
    /// </summary>
    public IDisposable? BeginScope(ILogger logger)
        => logger.BeginScope(
            new Dictionary<string, object?> { ["CorrelationId"] = GetOrCreate() });
}
