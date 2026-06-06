using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PicoPlus.Infrastructure.Services;
using PicoPlus.Infrastructure.State;
using PicoPlus.Models.CRM.Objects;
using Microsoft.Extensions.Logging;
using ContactService = PicoPlus.Services.CRM.Objects.Contact;
using PicoPlus.Services.CRM;
using PicoPlus.Services.Shared;

namespace PicoPlus.ViewModels.Auth;

/// <summary>
/// ViewModel for Login page
/// Handles national code verification and HubSpot contact lookup
/// </summary>
public partial class LoginViewModel : BaseViewModel
{
    private readonly ContactService _contactService;
    private readonly ContactUpdateService _contactUpdateService;
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;
    private readonly ISessionStorageService _sessionStorage;
    private readonly ILocalStorageService _localStorage;
    private readonly AuthenticationStateService _authState;
    private readonly ILogger<LoginViewModel> _logger;

    [ObservableProperty]
    private string nationalCode = string.Empty;

    [ObservableProperty]
    private string selectedRole = "User";

    public LoginViewModel(
        ContactService contactService,
        ContactUpdateService contactUpdateService,
        INavigationService navigationService,
        IDialogService dialogService,
        ISessionStorageService sessionStorage,
        ILocalStorageService localStorage,
        AuthenticationStateService authState,
        ILogger<LoginViewModel> logger)
    {
        _contactService = contactService;
        _contactUpdateService = contactUpdateService;
        _navigationService = navigationService;
        _dialogService = dialogService;
        _sessionStorage = sessionStorage;
        _localStorage = localStorage;
        _authState = authState;
        _logger = logger;
        Title = "ورود به سامانه";
    }

    [RelayCommand]
    private async Task LoginAsync(CancellationToken cancellationToken)
    {
        await ExecuteAsync(async () =>
        {
            // Validate national code
            if (!ValidateNationalCode())
                return;

            _logger.LogInformation("Attempting login for national code: {NationalCode}", NationalCode);

            // Search for contact in HubSpot
            var searchResponse = await _contactService.Search(
                query: NationalCode,
                paramName: "natcode",
                paramValue: NationalCode,
                propertiesToInclude: new[]
                {
                    "firstname",
                    "lastname",
                    "email",
                    "phone",
                    "natcode",
                    "dateofbirth",
                    "father_name",
                    "gender",
                    "total_revenue",
                    "shahkar_status",
                    "wallet",
                    "num_associated_deals",
                    "contact_plan"
                }
            );

            if (searchResponse?.results != null && searchResponse.results.Any())
            {
                // Contact exists - auto-update missing fields from Zibal
                var contact = searchResponse.results.First();

                _logger.LogInformation("Contact found: {ContactId}. Checking for missing fields...", contact.id);

                // Auto-update missing fields (father_name, gender, shahkar_status, etc.)
                contact = await _contactUpdateService.UpdateMissingFieldsAsync(contact, cancellationToken);

                _logger.LogInformation("Contact data refreshed and updated if needed: {ContactId}", contact.id);

                // Set authentication state
                _authState.SetAuthenticatedUser(contact);
                await _sessionStorage.SetItemAsync("LogInState", 1, cancellationToken);
                await _sessionStorage.SetItemAsync("ContactModel", contact, cancellationToken);
                await _sessionStorage.SetItemAsync("user_role", SelectedRole, cancellationToken);

                // Also persist for browser restarts
                await _localStorage.SetItemAsync("LogInState", 1, cancellationToken);
                await _localStorage.SetItemAsync("ContactModel", contact, cancellationToken);
                await _localStorage.SetItemAsync("user_role", SelectedRole, cancellationToken);

                // Navigate based on role
                if (SelectedRole == "Admin")
                {
                    _navigationService.NavigateTo("/admin/dashboard");
                }
                else
                {
                    _navigationService.NavigateTo("/user");
                }
            }
            else
            {
                // Contact not found - redirect to registration
                _logger.LogInformation("Contact not found for national code: {NationalCode}. Redirecting to registration", NationalCode);

                // Store national code for registration
                await _sessionStorage.SetItemAsync("PendingNationalCode", NationalCode, cancellationToken);

                // Show message and redirect
                await _dialogService.ShowInfoAsync(
                    "کاربر یافت نشد",
                    "حسابی با این کد ملی یافت نشد. لطفاً ثبت‌نام کنید.");

                _navigationService.NavigateTo("/auth/register");
            }
        }, cancellationToken);
    }

    private bool ValidateNationalCode()
    {
        NationalCode = NationalCode?.Trim() ?? string.Empty;
        var error = NationalCodeValidator.Validate(NationalCode);
        if (error != null)
        {
            ErrorMessage = error;
            HasError = true;
            return false;
        }
        return true;
    }

    protected override void OnError(Exception exception)
    {
        _logger.LogError(exception, "Error in LoginViewModel");
    }
}
