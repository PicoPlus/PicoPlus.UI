using PicoPlus.Models.CRM.Objects;

namespace PicoPlus.Infrastructure.State;

/// <summary>
/// Application-wide authentication state management.
/// </summary>
public class AuthenticationStateService
{
    public bool IsAuthenticated { get; private set; }

    public Contact.Search.Response.Result? CurrentUser { get; private set; }

    public int LoginState { get; private set; }

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
