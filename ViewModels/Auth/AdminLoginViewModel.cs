using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PicoPlus.Infrastructure.State;
using PicoPlus.Infrastructure.Services;
using Microsoft.AspNetCore.Components;

namespace PicoPlus.ViewModels.Auth;

public partial class AdminLoginViewModel : ObservableObject
{
    private readonly AuthenticationStateService _authState;
    private readonly ISessionStorageService _sessionStorage;
    private readonly ILocalStorageService _localStorage;
    private readonly NavigationManager _navigationService;
    private readonly ILogger<AdminLoginViewModel> _logger;

    [ObservableProperty] private string email = string.Empty;
    [ObservableProperty] private string password = string.Empty;
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private bool rememberMe;

    public AdminLoginViewModel(
        AuthenticationStateService authState,
        ISessionStorageService sessionStorage,
        ILocalStorageService localStorage,
        NavigationManager navigationService,
        ILogger<AdminLoginViewModel> logger)
    {
        _authState = authState;
        _sessionStorage = sessionStorage;
        _localStorage = localStorage;
        _navigationService = navigationService;
        _logger = logger;
    }

    [RelayCommand]
    private async Task LoginAsync(CancellationToken cancellationToken = default)
    {
        HasError = false;
        ErrorMessage = string.Empty;

        if (!ValidateInputs())
        {
            return;
        }

        IsLoading = true;

        try
        {
            if (await ValidateAdminCredentialsAsync())
            {
                await _sessionStorage.SetItemAsync("LogInState", 1, cancellationToken);
                await _sessionStorage.SetItemAsync("user_email", Email, cancellationToken);
                await _sessionStorage.SetItemAsync("user_name", GetAdminName(Email), cancellationToken);

                if (RememberMe)
                {
                    await _localStorage.SetItemAsync("LogInState", 1, cancellationToken);
                    await _localStorage.SetItemAsync("user_email", Email, cancellationToken);
                    await _localStorage.SetItemAsync("user_name", GetAdminName(Email), cancellationToken);
                }

                _authState.SetAdminAuthenticatedUser(Email, GetAdminName(Email));

                _logger.LogInformation("Admin logged in successfully: {Email}", Email);

                _navigationService.NavigateTo("/admin/owner-select", true);
            }
            else
            {
                HasError = true;
                ErrorMessage = "??????? ???? ?????? ???";
                _logger.LogWarning("Failed admin login attempt for: {Email}", Email);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during admin login");
            HasError = true;
            ErrorMessage = "??? ?? ?????? ????. ???? ?????? ???? ????.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool ValidateInputs()
    {
        if (string.IsNullOrWhiteSpace(Email))
        {
            ErrorMessage = "????? ????????? ???? ????";
            HasError = true;
            return false;
        }

        if (!IsValidEmail(Email))
        {
            ErrorMessage = "????? ???? ??? ????? ????";
            HasError = true;
            return false;
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "??? ???? ????????? ???? ????";
            HasError = true;
            return false;
        }

        if (Password.Length < 6)
        {
            ErrorMessage = "??? ??? ???? ???? ????? 6 ??????? ????";
            HasError = true;
            return false;
        }

        return true;
    }

    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> ValidateAdminCredentialsAsync()
    {
        var validAdmins = new Dictionary<string, string>
        {
            { "admin@picoplus.app", "Admin@123" },
            { "secgen.unity@gmail.com", "Secgen@2024" },
            { "manager@picoplus.app", "Manager@123" }
        };

        if (validAdmins.TryGetValue(Email.ToLowerInvariant(), out var validPassword))
        {
            return Password == validPassword;
        }

        return false;
    }

    private string GetAdminName(string email)
    {
        var atIndex = email.IndexOf('@');
        if (atIndex > 0)
        {
            var name = email.Substring(0, atIndex);
            return char.ToUpper(name[0]) + name.Substring(1);
        }
        return "Admin";
    }

    [RelayCommand]
    private void NavigateToUserLogin()
    {
        _navigationService.NavigateTo("/auth/login");
    }
}
