# PicoPlus Blazor Server Remediation Playbook (.NET 9, HubSpot-centric)

This playbook addresses each known issue in strict priority order (critical security first), with:
1. Detailed issue report
2. Fix plan (steps + code/config changes + tests)
3. Conceptual fix snippets aligned with .NET 9 / Blazor Server best practices

---

## 1) CRITICAL — TLS Validation Bypass (`ShecanDnsHttpClientHandler`)

### Issue report
- **Severity:** Critical
- **Description:** `ServerCertificateCustomValidationCallback = (...) => true` disables TLS certificate validation globally.
- **Affected modules:** `Infrastructure/Http/ShecanDnsHttpClientHandler.cs`, all HttpClient registrations using this handler.
- **Potential impact:** MITM interception, credential/token theft (HubSpot/SMS/API secrets), tampered responses.
- **Suggested remediation:** Remove permissive callback, enforce platform trust chain, optionally pin known certificates/public keys for high-value endpoints.

### Fix plan
1. Remove callback that returns true.
2. Keep decompression/redirect settings only.
3. For hardening, add optional pinning check gated by configuration.
4. Add unit test to ensure handler does not disable certificate validation.
5. Run integration smoke against HubSpot/SMS endpoints with real certificate validation.

### Conceptual code fix
```csharp
// Infrastructure/Http/ShecanDnsHttpClientHandler.cs
using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace PicoPlus.Infrastructure.Http;

public class ShecanDnsHttpClientHandler : HttpClientHandler
{
    public ShecanDnsHttpClientHandler(IConfiguration? config = null)
    {
        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
        AllowAutoRedirect = true;
        MaxAutomaticRedirections = 10;

        // No insecure bypass.
        // Optional pinning for selected environments:
        var allowedThumbprints = config?["Security:AllowedServerThumbprints"]
            ?.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            ?? Array.Empty<string>();

        if (allowedThumbprints.Length > 0)
        {
            ServerCertificateCustomValidationCallback = (_, cert, chain, errors) =>
            {
                if (errors != System.Net.Security.SslPolicyErrors.None || cert is null) return false;
                var thumbprint = new X509Certificate2(cert).Thumbprint;
                return allowedThumbprints.Contains(thumbprint, StringComparer.OrdinalIgnoreCase);
            };
        }
    }
}
```

### Unit test (concept)
```csharp
// tests/PicoPlus.Tests/Infrastructure/ShecanDnsHttpClientHandlerTests.cs
public class ShecanDnsHttpClientHandlerTests
{
    [Fact]
    public void Constructor_DoesNotSetInsecureTlsBypass_WhenNoPinningConfigured()
    {
        var handler = new ShecanDnsHttpClientHandler();
        // Null callback means default platform certificate validation is used.
        Assert.Null(handler.ServerCertificateCustomValidationCallback);
    }
}
```

---

## 2) CRITICAL — Hard-coded Admin Credentials

### Issue report
- **Severity:** Critical
- **Description:** Admin credentials are hardcoded in `AdminLoginViewModel`.
- **Affected modules:** `ViewModels/Auth/AdminLoginViewModel.cs`.
- **Potential impact:** Source leak immediately compromises admin panel; no credential rotation/governance.
- **Suggested remediation:** Move admin auth to server-side identity store (DB/IdP), use hashed passwords and role claims.

### Fix plan
1. Introduce `IAdminCredentialValidator` service on server.
2. Store admin accounts in secure configuration/DB with PBKDF2 hashes (or Identity tables).
3. Validate credentials on server only; never return secrets to browser.
4. Issue authenticated principal + role claim (`Admin`) via cookie auth.
5. Add tests for valid/invalid login and lockout behavior.

### Conceptual code fix
```csharp
// Services/Auth/IAdminCredentialValidator.cs
public interface IAdminCredentialValidator
{
    Task<bool> ValidateAsync(string email, string password, CancellationToken ct = default);
}

// Services/Auth/Pbkdf2AdminCredentialValidator.cs
using System.Security.Cryptography;

public sealed class Pbkdf2AdminCredentialValidator : IAdminCredentialValidator
{
    private readonly IConfiguration _config;
    public Pbkdf2AdminCredentialValidator(IConfiguration config) => _config = config;

    public Task<bool> ValidateAsync(string email, string password, CancellationToken ct = default)
    {
        // Example config: AdminAuth:Users:0:Email, :Salt, :Hash, :Iterations
        var users = _config.GetSection("AdminAuth:Users").Get<List<AdminUserRecord>>() ?? [];
        var user = users.FirstOrDefault(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));
        if (user is null) return Task.FromResult(false);

        var salt = Convert.FromBase64String(user.Salt);
        var expected = Convert.FromBase64String(user.Hash);
        var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, user.Iterations, HashAlgorithmName.SHA256, expected.Length);

        return Task.FromResult(CryptographicOperations.FixedTimeEquals(actual, expected));
    }

    private sealed record AdminUserRecord(string Email, string Salt, string Hash, int Iterations);
}
```

```csharp
// Program.cs
builder.Services
    .AddAuthentication("AdminCookie")
    .AddCookie("AdminCookie", opt =>
    {
        opt.LoginPath = "/auth/login";
        opt.AccessDeniedPath = "/auth/login";
        opt.SlidingExpiration = true;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
});

builder.Services.AddScoped<IAdminCredentialValidator, Pbkdf2AdminCredentialValidator>();
```

### Unit/integration tests (concept)
- `ValidateAsync_ReturnsTrue_ForValidPasswordHash`
- `ValidateAsync_ReturnsFalse_ForWrongPassword`
- Admin endpoint returns 302 to login for unauthenticated user.
- Admin endpoint returns 403 for authenticated non-admin.

---

## 3) HIGH — Client-side Authorization Enforcement

### Issue report
- **Severity:** High
- **Description:** Role checks rely on browser/session state (`user_role`).
- **Affected modules:** `AuthService`, `AdminAuthorizationHandler`, admin pages.
- **Potential impact:** Privilege escalation by storage tampering.
- **Suggested remediation:** Authorize at server endpoint/component boundary with cookie claims.

### Fix plan
1. Add `[Authorize(Policy="AdminOnly")]` to admin routes/components.
2. Replace storage role checks with `AuthenticationStateProvider`/claims.
3. Keep storage only for UI preferences, never trust decisions.
4. Add integration tests for admin route authorization.

### Conceptual snippet
```razor
@attribute [Authorize(Policy = "AdminOnly")]
@page "/admin/dashboard"
```

```csharp
// Example secure check from claims
var user = httpContext.User;
if (!user.IsInRole("Admin")) return Results.Forbid();
```

---

## 4) HIGH — Open Redirect (`/set-culture/{culture}`)

### Issue report
- **Severity:** High
- **Description:** Redirect uses unvalidated `redirectUri`.
- **Affected module:** `Program.cs` endpoint mapping.
- **Potential impact:** Phishing/open redirect abuse.
- **Suggested remediation:** Allow only local URLs; fallback to `/`.

### Fix plan
1. Validate `redirectUri` with local-url check.
2. Reject/normalize external absolute URLs.
3. Add tests for local/external redirect values.

### Conceptual code fix
```csharp
app.MapGet("/set-culture/{culture}", (string culture, string? redirectUri, HttpContext ctx) =>
{
    if (!string.IsNullOrWhiteSpace(culture))
    {
        ctx.Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), HttpOnly = true, Secure = true, SameSite = SameSiteMode.Lax });
    }

    var target = "/";
    if (!string.IsNullOrWhiteSpace(redirectUri) && Uri.TryCreate(redirectUri, UriKind.Relative, out _))
        target = redirectUri;

    return Results.LocalRedirect(target);
});
```

---

## 5) HIGH — Plaintext OTP Logging + Weak OTP State Handling

### Issue report
- **Severity:** High
- **Description:** OTP values are logged and generated by `Random`; singleton uses non-thread-safe dictionary.
- **Affected modules:** `Services/Auth/OtpService.cs`, `Services/SMS/SmsIrService.cs`, DI in `Program.cs`.
- **Potential impact:** OTP leakage, brute-force facilitation, race conditions.
- **Suggested remediation:** Redacted logging, cryptographic RNG, thread-safe store, throttling.

### Fix plan
1. Replace `Random` with `RandomNumberGenerator.GetInt32`.
2. Use `ConcurrentDictionary<string, OtpData>`.
3. Remove OTP code from logs (log correlation IDs only).
4. Add per-phone/IP rate limiting and retry cooldown.
5. Unit tests for generation bounds, expiry, attempt lockout, concurrency.

### Conceptual snippet
```csharp
private readonly ConcurrentDictionary<string, OtpData> _otpStore = new();

public string GenerateOtp()
{
    var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString(CultureInfo.InvariantCulture);
    _logger.LogDebug("Generated OTP for request {RequestId}", Guid.NewGuid());
    return code;
}
```

---

## 6) HIGH — Session Key Mismatch (`UserRole` vs `user_role`)

### Issue report
- **Severity:** High
- **Description:** Logout clears `UserRole` but login writes `user_role`.
- **Affected modules:** `UserPanelService`, auth/login flows.
- **Potential impact:** stale role state, inconsistent access behavior.
- **Suggested remediation:** Introduce centralized constants and use everywhere.

### Fix plan
1. Create `StorageKeys` static class.
2. Replace all string literals with constants.
3. Add unit tests verifying login+logout clears all keys.

### Conceptual snippet
```csharp
public static class StorageKeys
{
    public const string LoginState = "LogInState";
    public const string ContactModel = "ContactModel";
    public const string UserRole = "user_role";
}

await _sessionStorage.RemoveItemAsync(StorageKeys.UserRole, cancellationToken);
```

---

## 7) HIGH — Deal Date Mapping Error

### Issue report
- **Severity:** High
- **Description:** `CloseDate` parses `createdate` instead of `closedate`.
- **Affected module:** `Services/UserPanel/UserPanelService.cs`.
- **Potential impact:** Wrong reporting/statistics and user confusion.
- **Suggested remediation:** Parse `closedate` with invariant culture/date parsing fallback.

### Fix plan
1. Replace mapping field to `closedate`.
2. Add null-safe parser helper.
3. Add unit test for mapping accuracy.

### Conceptual snippet
```csharp
CloseDate = DateTime.TryParse(deal.properties?.closedate, CultureInfo.InvariantCulture,
    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var closeDate)
    ? closeDate
    : null,
```

---

## 8) MEDIUM — Admin Login Redirect Loop/Confusion

### Issue report
- **Severity:** Medium
- **Description:** `/admin/login` redirects to `/auth/login`; route intent unclear.
- **Affected module:** `Views/Admin/AdminLogin.razor`.
- **Potential impact:** UX confusion and accidental auth flow breakage.
- **Suggested remediation:** Consolidate to one login route with mode parameter or one dedicated page.

### Fix plan
1. Keep `/auth/login` as single page.
2. Make `/admin/login` route alias with preselected admin mode.
3. Add route tests and UI behavior tests.

### Conceptual snippet
```razor
@page "/admin/login"
@inject NavigationManager Nav
@code {
  protected override void OnInitialized() => Nav.NavigateTo("/auth/login?mode=admin", true);
}
```

---

## 9) MEDIUM — High-Latency Login Flow (HubSpot-first synchronous chain)

### Issue report
- **Severity:** Medium
- **Description:** Login waits on CRM search + update before navigation.
- **Affected modules:** `LoginViewModel`, `ContactUpdateService`.
- **Potential impact:** Slow TTFI, timeout risk, poor UX under HubSpot latency.
- **Suggested remediation:** Split critical path and background enrichment.

### Fix plan
1. Authenticate once contact exists; navigate quickly.
2. Queue missing-field enrichment in background hosted service.
3. Add request timeout and cancellation.
4. Add telemetry for p50/p95 login latency.

### Conceptual snippet
```csharp
// On successful contact lookup:
await SignInAsync(contact);
_navigationService.NavigateTo("/user");
_ = _backgroundEnricher.EnqueueContactRefresh(contact.id); // fire-and-forget queue
```

---

## 10) MEDIUM — External Calls Without Resilience Policies

### Issue report
- **Severity:** Medium
- **Description:** No retry/circuit-breaker/jitter policies for HubSpot/SMS/Zibal.
- **Affected modules:** `Program.cs`, CRM/SMS services.
- **Potential impact:** Cascading failures, transient outage amplification.
- **Suggested remediation:** Add Polly-style resilience pipelines for HttpClient.

### Fix plan
1. Configure per-client resilience handler (`AddStandardResilienceHandler`).
2. Set bounded retries with jitter and timeout budget.
3. Add fallback/error mapping by provider.
4. Add integration tests with mocked transient failures.

### Conceptual snippet
```csharp
builder.Services.AddHttpClient("HubSpot", c =>
{
    c.BaseAddress = new Uri("https://api.hubapi.com");
    c.Timeout = TimeSpan.FromSeconds(20);
})
.AddStandardResilienceHandler(options =>
{
    options.Retry.MaxRetryAttempts = 3;
    options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30);
    options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
});
```

---

## 11) LOW-MEDIUM — Unoptimized Rendering (`ShouldRender` always true)

### Issue report
- **Severity:** Low-Medium
- **Description:** `ShouldRender()` override adds no optimization and can mislead maintainers.
- **Affected module:** `Pages/User/Panel.razor.cs`.
- **Potential impact:** Increased cognitive load; missed optimization opportunities.
- **Suggested remediation:** Remove override or implement true change-tracking.

### Fix plan
1. Remove override.
2. Use immutable state updates / targeted `StateHasChanged` invocation.
3. Add component render test if render frequency matters.

### Conceptual snippet
```csharp
// Remove ShouldRender override entirely unless a measurable optimization exists.
```

---

## 12) LOW — Repository Hygiene (stale/accidental files)

### Issue report
- **Severity:** Low
- **Description:** Stray files (`et --hard 78dc6c6`, `temp_fix.txt`, stale `Views/auth/Login`) pollute repo.
- **Affected modules:** repo root + `Views/auth`.
- **Potential impact:** confusion, accidental leakage, CI noise.
- **Suggested remediation:** delete stale files, add lint/allowlist checks.

### Fix plan
1. Remove accidental files.
2. Add `.editorconfig`, `.gitattributes`, CI path allowlist/secret scanning.
3. Add PR template requiring security checklist.

### Conceptual snippet
```yaml
# .github/workflows/repo-hygiene.yml
- name: Fail on suspicious filenames
  run: |
    rg -n "et --hard|temp_fix" . && exit 1 || true
```

---

## Cross-cutting architecture upgrades (HubSpot-specific)

1. **BFF pattern for Blazor Server + HubSpot**
   - Keep HubSpot token only server-side.
   - UI calls internal app services; app services call HubSpot.
2. **Security defaults**
   - Secure cookies, antiforgery for state-changing endpoints, strict transport security in prod.
3. **Observability**
   - Structured logging with PII masking.
   - Correlation IDs across login/HubSpot/SMS pipelines.
4. **Consistency refactor**
   - Centralized constants for storage keys, route names, claim types.

---

## Regression-safe rollout order

1. TLS validation fix + test
2. Admin credential hardening + server auth policies
3. Open-redirect fix
4. OTP security hardening
5. Session key/date mapping bug fixes
6. Resilience policies + login-path latency optimization
7. Rendering and hygiene cleanup

Each step should ship with tests and can be independently rolled back if needed.
