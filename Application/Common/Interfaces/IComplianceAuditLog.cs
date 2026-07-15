#nullable enable

namespace NovinCRM.Application.Common.Interfaces;

/// <summary>
/// Records compliance events (GDPR / PDPA privacy-deletion requests) to a
/// persistent audit trail.  The production implementation should write to a
/// dedicated append-only store or a structured log sink that cannot be edited
/// after the fact (e.g. Azure Immutable Blob Storage, Seq with archiving, etc.).
///
/// The no-op implementation (<see cref="NovinCRM.Infrastructure.Services.NoOpComplianceAuditLog"/>)
/// logs at Information level so the event is observable in any environment
/// without requiring a real persistence backend.
/// </summary>
public interface IComplianceAuditLog
{
    /// <summary>
    /// Records that a HubSpot privacy-deletion request was received and processed
    /// for the specified contact.  Must complete before returning.
    /// </summary>
    /// <param name="contactId">HubSpot object ID of the deleted contact.</param>
    /// <param name="occurredAt">UTC timestamp from the HubSpot webhook event.</param>
    /// <param name="ct">Cancellation token.</param>
    Task RecordPrivacyDeletionAsync(
        string   contactId,
        DateTime occurredAt,
        CancellationToken ct = default);
}
