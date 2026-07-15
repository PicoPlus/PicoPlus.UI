#nullable enable

using Microsoft.Extensions.Logging;
using NovinCRM.Application.Common.Interfaces;

namespace NovinCRM.Infrastructure.Services;

/// <summary>
/// No-op (placeholder) implementation of <see cref="IComplianceAuditLog"/>.
/// Logs privacy-deletion events at Information level so they are visible in all
/// environments.  Replace with a real append-only persistence implementation
/// before processing live GDPR / PDPA deletion requests in production.
/// </summary>
public sealed class NoOpComplianceAuditLog : IComplianceAuditLog
{
    private readonly ILogger<NoOpComplianceAuditLog> _logger;

    public NoOpComplianceAuditLog(ILogger<NoOpComplianceAuditLog> logger)
        => _logger = logger;

    public Task RecordPrivacyDeletionAsync(
        string   contactId,
        DateTime occurredAt,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[COMPLIANCE-AUDIT] Privacy deletion recorded: contactId={ContactId} occurredAt={OccurredAt:O}",
            contactId, occurredAt);

        return Task.CompletedTask;
    }
}
