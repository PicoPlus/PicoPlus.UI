#nullable enable

using PicoPlus.Application.Dto.UserPanel;

namespace PicoPlus.Application.Abstractions.UserPanel;

/// <summary>
/// Business logic service for user panel operations
/// Handles data aggregation and transformation
/// </summary>
public interface IUserPanelService
{
    /// <summary>
    /// Load complete user panel state for a given contact ID
    /// </summary>
    /// <param name="contactId">HubSpot contact ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Complete user panel state with contact info, deals, and statistics</returns>
    Task<UserPanelState?> LoadUserPanelStateAsync(string contactId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get user ID from session storage
    /// </summary>
    Task<string?> GetCurrentUserIdAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Clear user session data
    /// </summary>
    Task ClearSessionAsync(CancellationToken cancellationToken = default);
}
