using Microsoft.Extensions.Logging;
using PicoPlus.Application.Abstractions.Auth;
using PicoPlus.Application.Abstractions.Services;

namespace PicoPlus.Services.Auth;

/// <summary>
/// Authentication Service for handling role-based authentication
/// </summary>
public class AuthService : IAuthService
{
    private const string KeyLoginState = "LogInState";
    private const string KeyContact = "ContactModel";
    private const string KeyRole = "user_role"; // normalized key

    private readonly ISessionStorageService _sessionStorage;
    private readonly ILocalStorageService _localStorage;
    private readonly INavigationService _navigationService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        ISessionStorageService sessionStorage,
        ILocalStorageService localStorage,
        INavigationService navigationService,
        ILogger<AuthService> logger)
    {
        _sessionStorage = sessionStorage;
        _localStorage = localStorage;
        _navigationService = navigationService;
        _logger = logger;
    }

    /// <summary>
    /// Navigate user based on their role
    /// </summary>
    /// <param name="role">User role (User or Admin)</param>
    /// <param name="persist">Persist across sessions (remember me)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task NavigateByRoleAsync(string role, bool persist = false, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Navigating user with role: {Role}", role);

        // Store role in session
        await _sessionStorage.SetItemAsync(KeyRole, role, cancellationToken);

        if (persist)
        {
            await _localStorage.SetItemAsync(KeyRole, role, cancellationToken);
            await _localStorage.SetItemAsync(KeyLoginState, 1, cancellationToken);
        }

        // Navigate based on role
        if (role == "Admin")
        {
            _navigationService.NavigateTo("/admin/dashboard");
        }
        else
        {
            _navigationService.NavigateTo("/user/panel");
        }
    }

    /// <summary>
    /// Get current user role from storage
    /// </summary>
    public async Task<string> GetCurrentRoleAsync(CancellationToken cancellationToken = default)
    {
        var role = await _sessionStorage.GetItemAsync<string>(KeyRole, cancellationToken)
                   ?? await _localStorage.GetItemAsync<string>(KeyRole, cancellationToken);
        return role ?? "User";
    }

    /// <summary>
    /// Check if current user has admin role
    /// </summary>
    public async Task<bool> IsAdminAsync(CancellationToken cancellationToken = default)
    {
        var role = await GetCurrentRoleAsync(cancellationToken);
        return role == "Admin" || role == "SuperAdmin";
    }

    /// <summary>
    /// Check if user is authenticated
    /// </summary>
    public async Task<bool> IsAuthenticatedAsync(CancellationToken cancellationToken = default)
    {
        var loginState = await _sessionStorage.GetItemAsync<int>(KeyLoginState, cancellationToken);
        if (loginState == 1) return true;
        var persisted = await _localStorage.GetItemAsync<int>(KeyLoginState, cancellationToken);
        return persisted == 1;
    }

    /// <summary>
    /// Logout user and clear storage
    /// </summary>
    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Logging out user");

        await _sessionStorage.RemoveItemAsync(KeyLoginState, cancellationToken);
        await _sessionStorage.RemoveItemAsync(KeyContact, cancellationToken);
        await _sessionStorage.RemoveItemAsync(KeyRole, cancellationToken);

        await _localStorage.RemoveItemAsync(KeyLoginState, cancellationToken);
        await _localStorage.RemoveItemAsync(KeyContact, cancellationToken);
        await _localStorage.RemoveItemAsync(KeyRole, cancellationToken);

        _navigationService.NavigateTo("/auth/login");
    }

    /// <summary>
    /// Validate required role
    /// </summary>
    public async Task<bool> ValidateRoleAccessAsync(string requiredRole, CancellationToken cancellationToken = default)
    {
        var isAuthenticated = await IsAuthenticatedAsync(cancellationToken);
        if (!isAuthenticated)
        {
            _logger.LogWarning("User not authenticated");
            return false;
        }

        var currentRole = await GetCurrentRoleAsync(cancellationToken);

        if (currentRole != requiredRole)
        {
            _logger.LogWarning("User with role {CurrentRole} attempted to access {RequiredRole} resource",
                currentRole, requiredRole);
            return false;
        }

        return true;
    }
}
