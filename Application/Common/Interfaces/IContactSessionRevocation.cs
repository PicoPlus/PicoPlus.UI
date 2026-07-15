#nullable enable

namespace NovinCRM.Application.Common.Interfaces;

/// <summary>
/// Revokes all active Blazor circuits / session-storage tokens that belong to a
/// specific contact.  Called during the GDPR/PDPA privacy-deletion flow to
/// ensure no session can continue accessing data about a deleted subject.
///
/// The no-op implementation (<see cref="NovinCRM.Infrastructure.Services.NoOpContactSessionRevocation"/>)
/// logs at Information level.  A real implementation should consult the active
/// Blazor circuit registry and call <c>ICircuitAccessor.CloseAsync()</c> on each
/// circuit whose session-storage key <c>"ContactModel.id"</c> matches
/// <paramref name="contactId"/>.
/// </summary>
public interface IContactSessionRevocation
{
    /// <summary>
    /// Revokes all active sessions associated with the given contact.
    /// Implementations MUST NOT throw — failures must be logged and swallowed
    /// so the rest of the privacy-deletion pipeline still completes.
    /// </summary>
    /// <param name="contactId">HubSpot object ID of the contact whose sessions must be revoked.</param>
    /// <param name="ct">Cancellation token.</param>
    Task RevokeContactSessionsAsync(
        string   contactId,
        CancellationToken ct = default);
}
