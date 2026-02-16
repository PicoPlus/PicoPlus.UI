using Microsoft.AspNetCore.Components;
using PicoPlus.Infrastructure.State;

namespace PicoPlus.Infrastructure.Authorization;

/// <summary>
/// Handler for admin authorization checks
/// </summary>
public class AdminAuthorizationHandler
{
    private static readonly HashSet<string> AllowedAdminEmails = new(StringComparer.OrdinalIgnoreCase)
    {
        "admin@picoplus.app",
        "secgen.unity@gmail.com",
        "manager@picoplus.app"
    };

    private readonly AuthenticationStateService _authState;
    private readonly NavigationManager _navigation;
    private readonly ILogger<AdminAuthorizationHandler> _logger;

    public AdminAuthorizationHandler(
        AuthenticationStateService authState,
        NavigationManager navigation,
        ILogger<AdminAuthorizationHandler> logger)
    {
        _authState = authState;
        _navigation = navigation;
        _logger = logger;
    }

    /// <summary>
    /// Check if current user has admin access
    /// </summary>
    public Task<bool> IsAdminAsync(CancellationToken cancellationToken = default)
    {
        if (!_authState.IsAuthenticated)
        {
            _logger.LogWarning("User is not authenticated");
            return Task.FromResult(false);
        }

        if (!_authState.IsAdminAuthenticated)
        {
            _logger.LogWarning("Authenticated user does not have admin state");
            return Task.FromResult(false);
        }

        if (!AllowedAdminEmails.Contains(_authState.AdminEmail))
        {
            _logger.LogWarning("Admin authorization denied. Unknown admin email: {Email}", _authState.AdminEmail);
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }

    /// <summary>
    /// Ensure user is admin or redirect to login
    /// </summary>
    public async Task<bool> EnsureAdminAccessAsync(CancellationToken cancellationToken = default)
    {
        var isAdmin = await IsAdminAsync(cancellationToken);

        if (!isAdmin)
        {
            _logger.LogWarning("Unauthorized admin access attempt. Redirecting to admin login.");
            _navigation.NavigateTo("/admin/login", true);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Get current admin user info
    /// </summary>
    public async Task<AdminUserInfo?> GetAdminUserInfoAsync(CancellationToken cancellationToken = default)
    {
        var isAdmin = await IsAdminAsync(cancellationToken);
        if (!isAdmin)
        {
            return null;
        }

        return new AdminUserInfo
        {
            Name = string.IsNullOrWhiteSpace(_authState.AdminDisplayName) ? "Admin" : _authState.AdminDisplayName,
            Email = _authState.AdminEmail,
            Role = "Admin"
        };
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
