using Microsoft.Extensions.Logging;
using PicoPlus.Infrastructure.Services;
using PicoPlus.Infrastructure.State;

namespace PicoPlus.Services.Auth;

/// <summary>
/// Authentication service for login state and logout orchestration.
/// </summary>
public class AuthService
{
    private const string KeyLoginState = "LogInState";
    private const string KeyContact = "ContactModel";

    private readonly ISessionStorageService _sessionStorage;
    private readonly ILocalStorageService _localStorage;
    private readonly INavigationService _navigationService;
    private readonly AuthenticationStateService _authState;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        ISessionStorageService sessionStorage,
        ILocalStorageService localStorage,
        INavigationService navigationService,
        AuthenticationStateService authState,
        ILogger<AuthService> logger)
    {
        _sessionStorage = sessionStorage;
        _localStorage = localStorage;
        _navigationService = navigationService;
        _authState = authState;
        _logger = logger;
    }

    public bool IsAdmin() => _authState.IsAuthenticated && _authState.IsAdminAuthenticated;

    public bool IsAuthenticated() => _authState.IsAuthenticated;

    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Logging out user");

        await _sessionStorage.RemoveItemAsync(KeyLoginState, cancellationToken);
        await _sessionStorage.RemoveItemAsync(KeyContact, cancellationToken);

        await _localStorage.RemoveItemAsync(KeyLoginState, cancellationToken);
        await _localStorage.RemoveItemAsync(KeyContact, cancellationToken);

        _authState.ClearAuthentication();
        _navigationService.NavigateTo("/auth/login");
    }
}
