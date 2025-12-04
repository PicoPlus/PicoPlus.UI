using Microsoft.AspNetCore.Components;
using PicoPlus.Infrastructure.State;
using PicoPlus.Infrastructure.Services;

namespace PicoPlus.Infrastructure.Authorization;

/// <summary>
/// Handler for admin authorization checks
/// </summary>
public class AdminAuthorizationHandler
{
    private readonly AuthenticationStateService _authState;
    private readonly ISessionStorageService _sessionStorage;
    private readonly ILocalStorageService _localStorage;
    private readonly NavigationManager _navigation;
    private readonly ILogger<AdminAuthorizationHandler> _logger;

    public AdminAuthorizationHandler(
        AuthenticationStateService authState,
        ISessionStorageService sessionStorage,
        ILocalStorageService localStorage,
        NavigationManager navigation,
        ILogger<AdminAuthorizationHandler> logger)
    {
        _authState = authState;
        _sessionStorage = sessionStorage;
        _localStorage = localStorage;
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
            // First check session storage for login state
            var loginState = await _sessionStorage.GetItemAsync<int>("LogInState", cancellationToken);
            
            // If not in session, check local storage (remember me)
            if (loginState != 1)
            {
                loginState = await _localStorage.GetItemAsync<int>("LogInState", cancellationToken);
                
                // If found in local storage, restore to session
                if (loginState == 1)
                {
                    var role = await _localStorage.GetItemAsync<string>("user_role", cancellationToken);
                    var email = await _localStorage.GetItemAsync<string>("user_email", cancellationToken);
                    var name = await _localStorage.GetItemAsync<string>("user_name", cancellationToken);
                    
                    await _sessionStorage.SetItemAsync("LogInState", loginState, cancellationToken);
                    await _sessionStorage.SetItemAsync("user_role", role, cancellationToken);
                    await _sessionStorage.SetItemAsync("user_email", email, cancellationToken);
                    await _sessionStorage.SetItemAsync("user_name", name, cancellationToken);
                    
                    // Update auth state
                    _authState.IsAuthenticated = true;
                    _authState.LoginState = 1;
                }
            }
            
            if (loginState != 1)
            {
                _logger.LogWarning("User is not authenticated (LoginState: {State})", loginState);
                return false;
            }

            // Check for admin role in session storage
            var adminRole = await _sessionStorage.GetItemAsync<string>("user_role", cancellationToken);
            
            if (adminRole == "Admin" || adminRole == "SuperAdmin")
            {
                // Ensure auth state is synced
                _authState.IsAuthenticated = true;
                _authState.LoginState = 1;
                
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
            _logger.LogWarning("Unauthorized admin access attempt. Redirecting to login.");
            _navigation.NavigateTo("/auth/login", true);
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
    
    /// <summary>
    /// Logout the admin user
    /// </summary>
    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _sessionStorage.RemoveItemAsync("LogInState", cancellationToken);
            await _sessionStorage.RemoveItemAsync("user_role", cancellationToken);
            await _sessionStorage.RemoveItemAsync("user_email", cancellationToken);
            await _sessionStorage.RemoveItemAsync("user_name", cancellationToken);
            
            await _localStorage.RemoveItemAsync("LogInState", cancellationToken);
            await _localStorage.RemoveItemAsync("user_role", cancellationToken);
            await _localStorage.RemoveItemAsync("user_email", cancellationToken);
            await _localStorage.RemoveItemAsync("user_name", cancellationToken);
            
            _authState.ClearAuthentication();
            
            _logger.LogInformation("Admin logged out successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during admin logout");
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
