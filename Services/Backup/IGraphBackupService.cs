using PicoPlus.State.UserPanel;

namespace PicoPlus.Services.Backup;

/// <summary>
/// Provides optional graph backup persistence (Neo4j).
/// </summary>
public interface IGraphBackupService
{
    Task BackupUserPanelStateAsync(string contactId, UserPanelState state, CancellationToken cancellationToken = default);
}
