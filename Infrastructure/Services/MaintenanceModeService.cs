#nullable enable

using NovinCRM.Application.Common.Interfaces;

namespace NovinCRM.Infrastructure.Services;

/// <summary>
/// Thread-safe in-memory implementation of <see cref="IMaintenanceModeService"/>.
/// Registered as a singleton — single instance shared across all Blazor circuits.
/// </summary>
public sealed class MaintenanceModeService : IMaintenanceModeService
{
    private volatile MaintenanceModeInfo? _info;

    public bool IsMaintenanceMode => _info != null;
    public MaintenanceModeInfo? CurrentInfo => _info;

    public void Enable(string reason, DateTime? estimatedEnd = null)
    {
        _info = new MaintenanceModeInfo(reason, DateTime.UtcNow, estimatedEnd);
    }

    public void Disable()
    {
        _info = null;
    }
}
