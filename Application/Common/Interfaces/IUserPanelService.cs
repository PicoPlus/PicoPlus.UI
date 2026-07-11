#nullable enable

using PicoPlus.Domain.Aggregates;

namespace PicoPlus.Services.UserPanel;

/// <summary>
/// Business logic service for user panel operations.
/// Handles data aggregation and transformation.
/// Returns canonical Domain types — no shim aliases.
/// </summary>
public interface IUserPanelService
{
    /// <summary>Load complete user panel state for a given contact ID.</summary>
    Task<UserPanelState?> LoadUserPanelStateAsync(string contactId, CancellationToken cancellationToken = default);

    /// <summary>Get user ID from session storage.</summary>
    Task<string?> GetCurrentUserIdAsync(CancellationToken cancellationToken = default);

    /// <summary>Clear user session data.</summary>
    Task ClearSessionAsync(CancellationToken cancellationToken = default);
}
