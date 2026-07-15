#nullable enable

using NovinCRM.Domain.Entities;

namespace NovinCRM.Application.Common.Interfaces;

/// <summary>
/// Issues and validates single-use invoice access tokens sent to customers via SMS.
/// The default in-memory implementation stores tokens in a ConcurrentDictionary;
/// replace with a database-backed implementation for multi-instance deployments.
/// </summary>
public interface IInvoiceAccessTokenRepository
{
    /// <summary>Issues a new token for the given deal + contact. Returns the raw token string.</summary>
    Task<string> IssueAsync(string dealId, string contactId, TimeSpan ttl);

    /// <summary>
    /// Validates the token. Returns the <see cref="InvoiceAccessToken"/> if valid (not expired,
    /// not consumed), or null if the token is unknown / expired / consumed.
    /// </summary>
    Task<InvoiceAccessToken?> ValidateAsync(string token);

    /// <summary>Marks the token as consumed so it cannot be used again.</summary>
    Task MarkConsumedAsync(string token);
}
