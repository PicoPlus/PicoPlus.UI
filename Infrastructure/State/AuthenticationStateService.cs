using PicoPlus.Models.CRM.Objects;
using DomainContact = PicoPlus.Domain.Entities.Contact;

namespace PicoPlus.Infrastructure.State;

/// <summary>
/// Application-wide authentication state management.
/// Plain service — no MVVM dependency.
/// </summary>
public class AuthenticationStateService
{
    public bool IsAuthenticated { get; set; }
    public int  LoginState      { get; set; }
    public Contact.Search.Response.Result? CurrentUser { get; private set; }

    public event EventHandler? AuthenticationStateChanged;

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

    public void RestoreFromSession(Contact.Search.Response.Result? user, int loginState)
    {
        CurrentUser     = user;
        IsAuthenticated = loginState == 1 && user != null;
        LoginState      = loginState;
        OnAuthenticationStateChanged();
    }

    private void OnAuthenticationStateChanged()
        => AuthenticationStateChanged?.Invoke(this, EventArgs.Empty);
}
