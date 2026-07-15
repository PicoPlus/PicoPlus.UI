#nullable enable

namespace NovinCRM.Application.Common.Interfaces;

/// <summary>
/// In-memory singleton service that tracks whether the application is in maintenance mode.
/// Enabled by <see cref="Infrastructure.Backup.NightlyBackupHostedService"/> at 00:00 and
/// disabled again by 00:30.
/// </summary>
public interface IMaintenanceModeService
{
    bool IsMaintenanceMode { get; }
    MaintenanceModeInfo? CurrentInfo { get; }

    void Enable(string reason, DateTime? estimatedEnd = null);
    void Disable();
}

/// <summary>Snapshot of the active maintenance window.</summary>
public sealed record MaintenanceModeInfo(
    string    Reason,
    DateTime  StartedAt,
    DateTime? EstimatedEnd);
