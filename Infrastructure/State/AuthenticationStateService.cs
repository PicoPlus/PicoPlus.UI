#nullable enable

using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Distributed;
using NovinCRM.Models.CRM.Objects;
using DomainContact = NovinCRM.Domain.Entities.Contact;

namespace NovinCRM.Infrastructure.State;

/// <summary>
/// Durable authentication state backed by <see cref="IDistributedCache"/> + <see cref="IDataProtector"/>.
///
/// Session data is encrypted at rest using ASP.NET Core Data Protection and stored in
/// the distributed cache (in-process memory by default, Redis when configured).
///
/// This replaces the previous in-memory singleton which lost all sessions on restart.
/// </summary>
public class AuthenticationStateService
{
    private const string PurposeName = "AuthenticationStateService.v1";
    private static readonly TimeSpan SessionTtl = TimeSpan.FromDays(7);

    private readonly IDistributedCache   _cache;
    private readonly IDataProtector      _protector;

    // ── Circuit-local state ───────────────────────────────────────────────────
    // These fields are intentionally NOT singleton-level. They are populated from
    // the durable cache on RestoreFromSession() and reflect the current circuit's view.
    public bool    IsAuthenticated { get; set; }
    public int     LoginState      { get; set; }
    public Contact.Search.Response.Result? CurrentUser { get; private set; }

    public event EventHandler? AuthenticationStateChanged;

    public AuthenticationStateService(
        IDistributedCache        cache,
        IDataProtectionProvider  dataProtection)
    {
        _cache     = cache;
        _protector = dataProtection.CreateProtector(PurposeName);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void SetAuthenticatedUser(Contact.Search.Response.Result user)
    {
        CurrentUser     = user;
        IsAuthenticated = true;
        LoginState      = 1;
        OnAuthenticationStateChanged();
    }

    public void SetAuthenticatedUser(DomainContact contact)
    {
        CurrentUser = new Contact.Search.Response.Result
        {
            id = contact.Id,
            properties = new Contact.Search.Response.Result.Properties
            {
                firstname            = contact.FirstName,
                lastname             = contact.LastName,
                email                = contact.Email,
                phone                = contact.Phone,
                ncode                = contact.NationalCode,
                dateofbirth          = contact.DateOfBirth,
                father_name          = contact.FatherName,
                gender               = contact.Gender,
                shahkar_status       = contact.ShahkarStatus,
                wallet               = contact.Wallet?.ToString(),
                total_revenue        = contact.TotalRevenue?.ToString(),
                num_associated_deals = contact.NumAssociatedDeals?.ToString(),
                contact_plan         = contact.ContactPlan,
                last_products_bought_product_1_image_url = contact.AvatarUrl
            }
        };
        IsAuthenticated = true;
        LoginState      = 1;
        OnAuthenticationStateChanged();
    }

    public void ClearAuthentication()
    {
        CurrentUser     = null;
        IsAuthenticated = false;
        LoginState      = 0;
        OnAuthenticationStateChanged();
    }

    public Task<bool> IsAuthenticatedAsync() => Task.FromResult(IsAuthenticated);

    /// <summary>Restores circuit-local state from in-memory values (already deserialized).</summary>
    public void RestoreFromSession(Contact.Search.Response.Result? user, int loginState)
    {
        CurrentUser     = user;
        IsAuthenticated = loginState == 1 && user != null;
        LoginState      = loginState;
        OnAuthenticationStateChanged();
    }

    // ── Durable session persistence ───────────────────────────────────────────

    /// <summary>
    /// Persists the current auth state to IDistributedCache (encrypted).
    /// Call this after <see cref="SetAuthenticatedUser"/> to survive restarts.
    /// </summary>
    public async Task PersistSessionAsync(string sessionKey, CancellationToken ct = default)
    {
        if (!IsAuthenticated || CurrentUser == null)
        {
            await _cache.RemoveAsync(CacheKey(sessionKey), ct);
            return;
        }

        try
        {
            var json      = JsonSerializer.Serialize(CurrentUser);
            var encrypted = _protector.Protect(json);
            var bytes     = System.Text.Encoding.UTF8.GetBytes(encrypted);

            await _cache.SetAsync(CacheKey(sessionKey), bytes,
                new DistributedCacheEntryOptions
                {
                    SlidingExpiration = SessionTtl
                }, ct);
        }
        catch
        {
            // Non-fatal — session simply won't survive restart
        }
    }

    /// <summary>
    /// Attempts to restore auth state from IDistributedCache.
    /// Returns <c>true</c> if a valid session was found.
    /// </summary>
    public async Task<bool> TryRestoreFromCacheAsync(string sessionKey, CancellationToken ct = default)
    {
        try
        {
            var bytes = await _cache.GetAsync(CacheKey(sessionKey), ct);
            if (bytes == null) return false;

            var encrypted = System.Text.Encoding.UTF8.GetString(bytes);
            var json      = _protector.Unprotect(encrypted);
            var user      = JsonSerializer.Deserialize<Contact.Search.Response.Result>(json);

            if (user == null) return false;

            RestoreFromSession(user, loginState: 1);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Removes the persisted session from the cache on logout.</summary>
    public Task RemoveSessionAsync(string sessionKey, CancellationToken ct = default)
        => _cache.RemoveAsync(CacheKey(sessionKey), ct);

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string CacheKey(string sessionKey) => $"auth:session:{sessionKey}";

    private void OnAuthenticationStateChanged()
        => AuthenticationStateChanged?.Invoke(this, EventArgs.Empty);
}
