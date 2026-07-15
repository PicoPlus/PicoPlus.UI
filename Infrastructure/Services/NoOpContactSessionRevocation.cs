#nullable enable

using Microsoft.Extensions.Logging;
using NovinCRM.Application.Common.Interfaces;

namespace NovinCRM.Infrastructure.Services;

/// <summary>
/// No-op (placeholder) implementation of <see cref="IContactSessionRevocation"/>.
/// Logs session-revocation requests at Information level so they are visible
/// without any real circuit-registry plumbing.  Replace with a real
/// implementation that consults the active Blazor circuit registry.
/// </summary>
public sealed class NoOpContactSessionRevocation : IContactSessionRevocation
{
    private readonly ILogger<NoOpContactSessionRevocation> _logger;

    public NoOpContactSessionRevocation(ILogger<NoOpContactSessionRevocation> logger)
        => _logger = logger;

    public Task RevokeContactSessionsAsync(
        string   contactId,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[SESSION-REVOCATION] Revoked all sessions for contactId={ContactId}",
            contactId);

        return Task.CompletedTask;
    }
}
