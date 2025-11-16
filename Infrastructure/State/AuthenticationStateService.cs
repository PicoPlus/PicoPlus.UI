using CommunityToolkit.Mvvm.ComponentModel;
using PicoPlus.Models.CRM.Objects;

namespace PicoPlus.Infrastructure.State;

/// <summary>
/// Application-wide authentication state management
/// </summary>
public partial class AuthenticationStateService : ObservableObject
{
    [ObservableProperty]
    private bool isAuthenticated;

    [ObservableProperty]
    private Contact.Search.Response.Result? currentUser;

    [ObservableProperty]
    private int loginState;

    public event EventHandler? AuthenticationStateChanged;

    public void SetAuthenticatedUser(Contact.Search.Response.Result user)
    {
        CurrentUser = user;
        IsAuthenticated = true;
        LoginState = 1;
        OnAuthenticationStateChanged();
    }

    public void ClearAuthentication()
    {
        CurrentUser = null;
        IsAuthenticated = false;
        LoginState = 0;
        OnAuthenticationStateChanged();
    }

    /// <summary>
    /// Check if user is authenticated (async for compatibility)
    /// </summary>
    public Task<bool> IsAuthenticatedAsync()
    {
        return Task.FromResult(IsAuthenticated);
    }

    public void RestoreFromSession(Contact.Search.Response.Result? user, int loginState)
    {
        CurrentUser = user;
        IsAuthenticated = loginState == 1 && user != null;
        LoginState = loginState;
        OnAuthenticationStateChanged();
    }

    private void OnAuthenticationStateChanged()
    {
        AuthenticationStateChanged?.Invoke(this, EventArgs.Empty);
    }
}
