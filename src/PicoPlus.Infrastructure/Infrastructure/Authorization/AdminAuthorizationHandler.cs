using Microsoft.AspNetCore.Components;
using PicoPlus.Infrastructure.State;
using PicoPlus.Application.Abstractions.Services;

namespace PicoPlus.Infrastructure.Authorization;

/// <summary>
/// Handler for admin authorization checks
/// </summary>
public class AdminAuthorizationHandler
{
    private readonly AuthenticationStateService _authState;
    private readonly ISessionStorageService _sessionStorage;
    private readonly NavigationManager _navigation;
    private readonly ILogger<AdminAuthorizationHandler> _logger;

    public AdminAuthorizationHandler(
        AuthenticationStateService authState,
        ISessionStorageService sessionStorage,
        NavigationManager navigation,
        ILogger<AdminAuthorizationHandler> logger)
    {
        _authState = authState;
        _sessionStorage = sessionStorage;
        _navigation = navigation;
        _logger = logger;
    }

    /// <summary>
    /// Check if current user has admin access
    /// </summary>
    public async Task<bool> IsAdminAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var isAuthenticated = await _authState.IsAuthenticatedAsync();
            if (!isAuthenticated)
            {
                _logger.LogWarning("User is not authenticated");
                return false;
            }

            // Check for admin role in session storage
            var adminRole = await _sessionStorage.GetItemAsync<string>("user_role", cancellationToken);
            
            if (adminRole == "Admin" || adminRole == "SuperAdmin")
            {
                _logger.LogInformation("User has admin access: {Role}", adminRole);
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
