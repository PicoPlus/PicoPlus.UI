#nullable enable

using System.Collections.Concurrent;
using System.Security.Cryptography;
using NovinCRM.Application.Common.Interfaces;
using NovinCRM.Domain.Entities;

namespace NovinCRM.Infrastructure.Services;

/// <summary>
/// In-memory implementation of <see cref="IInvoiceAccessTokenRepository"/>.
/// Uses <see cref="ConcurrentDictionary{TKey,TValue}"/> for thread safety.
/// Tokens are URL-safe Base64-encoded random bytes.
///
/// For multi-instance deployments, replace with a Redis or SQL-backed implementation.
/// </summary>
public sealed class InMemoryInvoiceAccessTokenRepository : IInvoiceAccessTokenRepository
{
    private readonly ConcurrentDictionary<string, InvoiceAccessToken> _tokens = new();

    public Task<string> IssueAsync(string dealId, string contactId, TimeSpan ttl)
    {
        var rawBytes = RandomNumberGenerator.GetBytes(32);
        var token    = Convert.ToBase64String(rawBytes)
                           .Replace('+', '-').Replace('/', '_').TrimEnd('=');

        var entry = new InvoiceAccessToken
        {
            Token     = token,
            DealId    = dealId,
            ContactId = contactId,
            ExpiresAt = DateTime.UtcNow.Add(ttl)
        };

        _tokens[token] = entry;
        return Task.FromResult(token);
    }

    public Task<InvoiceAccessToken?> ValidateAsync(string token)
    {
        if (!_tokens.TryGetValue(token, out var entry))
            return Task.FromResult<InvoiceAccessToken?>(null);

        if (entry.IsConsumed || DateTime.UtcNow > entry.ExpiresAt)
            return Task.FromResult<InvoiceAccessToken?>(null);

        return Task.FromResult<InvoiceAccessToken?>(entry);
    }

    public Task MarkConsumedAsync(string token)
    {
        if (_tokens.TryGetValue(token, out var entry))
        {
            entry.IsConsumed = true;
            entry.ConsumedAt = DateTime.UtcNow;
        }
        return Task.CompletedTask;
    }
}
