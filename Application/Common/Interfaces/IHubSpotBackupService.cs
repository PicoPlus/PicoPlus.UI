#nullable enable

namespace NovinCRM.Application.Common.Interfaces;

/// <summary>Result of a single nightly backup run.</summary>
public sealed record BackupResult(
    int     ContactsUpserted,
    int     DealsUpserted,
    int     LineItemsUpserted,
    int     AssociationsUpserted,
    int     NotesUpserted,
    bool    Success,
    string? Error = null);

/// <summary>
/// Pulls today's changed records from HubSpot and upserts them into SQL Server.
/// </summary>
public interface IHubSpotBackupService
{
    Task<BackupResult> RunDailyBackupAsync(DateOnly date, CancellationToken ct = default);
}
