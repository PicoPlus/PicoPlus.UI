using Microsoft.AspNetCore.Components;
using NovinCRM.Infrastructure.State;
using NovinCRM.Infrastructure.Services;

namespace NovinCRM.Infrastructure.Authorization;

/// <summary>
/// Handler for admin authorization checks
/// </summary>
public class AdminAuthorizationHandler
{
    private readonly AuthenticationStateService _authState;
    private readonly ISessionStorageService _sessionStorage;
    private readonly ILogger<AdminAuthorizationHandler> _logger;

    public AdminAuthorizationHandler(
        AuthenticationStateService authState,
        ISessionStorageService sessionStorage,
        ILogger<AdminAuthorizationHandler> logger)
    {
        _authState = authState;
        _sessionStorage = sessionStorage;
        _logger = logger;
    }

    /// <summary>
    /// Check if current user has admin access
    /// </summary>
    public async Task<bool> IsAdminAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Check role in session storage first — works after both soft and hard navigation
            var adminRole = await _sessionStorage.GetItemAsync<string>("user_role", cancellationToken);

            if (adminRole == "Admin" || adminRole == "SuperAdmin")
            {
                // Sync in-memory state if it got reset (e.g. after layout re-render)
                if (!_authState.IsAuthenticated)
                {
                    _authState.IsAuthenticated = true;
                    _authState.LoginState = 1;
                }
                _logger.LogInformation("User has admin access: {Role}", adminRole);
                return true;
            }

            // Fall back to in-memory state (set during same-circuit login)
            var isAuthenticated = await _authState.IsAuthenticatedAsync();
            if (isAuthenticated && _authState.LoginState == 1)
            {
                _logger.LogInformation("User authenticated via in-memory state");
                return true;
            }

            _logger.LogWarning("User does not have admin role: {Role}", adminRole ?? "None");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking admin authorization");
            return false;
        }
    }

    /// <summary>
    /// Returns true if the user is admin. Does NOT navigate — callers are
    /// responsible for redirecting so that Blazor can handle NavigationException
    /// at the correct component boundary.
    /// </summary>
    public async Task<bool> EnsureAdminAccessAsync(CancellationToken cancellationToken = default)
    {
        var isAdmin = await IsAdminAsync(cancellationToken);
        if (!isAdmin)
            _logger.LogWarning("Unauthorized admin access attempt.");
        return isAdmin;
    }

    /// <summary>
    /// Get current admin user info
    /// </summary>
    public async Task<AdminUserInfo?> GetAdminUserInfoAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var isAdmin = await IsAdminAsync(cancellationToken);
            if (!isAdmin) return null;

            var userName = await _sessionStorage.GetItemAsync<string>("user_name", cancellationToken);
            var userEmail = await _sessionStorage.GetItemAsync<string>("user_email", cancellationToken);
            var userRole = await _sessionStorage.GetItemAsync<string>("user_role", cancellationToken);

            return new AdminUserInfo
            {
                Name = userName ?? "Admin",
                Email = userEmail ?? "",
                Role = userRole ?? "Admin"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting admin user info");
            return null;
        }
    }
}

/// <summary>
/// Admin user information
/// </summary>
public class AdminUserInfo
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
