using Microsoft.AspNetCore.Components;
using PicoPlus.Domain.Aggregates;
using PicoPlus.Domain.Entities;
using PicoPlus.Domain.Enums;
using PicoPlus.Infrastructure.Services;
using PicoPlus.Services.UserPanel;

namespace PicoPlus.Pages.User;

/// <summary>
/// Code-behind for user panel page.
/// Uses canonical Domain types — no shim aliases.
/// </summary>
public partial class Panel : ComponentBase, IAsyncDisposable
{
    [Inject] private IUserPanelService PanelService   { get; set; } = default!;
    [Inject] private INavigationService NavigationService { get; set; } = default!;
    [Inject] private ILogger<Panel> Logger            { get; set; } = default!;

    private UserPanelState? _state;
    private Deal?   _selectedDeal;
    private TabType _activeTab = TabType.Profile;
    private bool    _isLoading = true;
    private bool    _hasError;
    private string? _errorMessage;
    private CancellationTokenSource? _cts;

    protected override async Task OnInitializedAsync()
    {
        _cts = new CancellationTokenSource();
        try
        {
            _isLoading = true;

            var userId = await PanelService.GetCurrentUserIdAsync(_cts.Token);
            if (string.IsNullOrEmpty(userId))
            {
                Logger.LogWarning("User not authenticated, redirecting to login");
                NavigationService.NavigateTo("/auth/login");
                return;
            }

            _state = await PanelService.LoadUserPanelStateAsync(userId, _cts.Token);

            if (_state == null)
            {
                Logger.LogError("Failed to load user panel state for: {UserId}", userId);
                ShowError("خطا در بارگذاری اطلاعات کاربر. لطفاً دوباره تلاش کنید.");
            }
        }
        catch (OperationCanceledException)
        {
            Logger.LogInformation("Panel initialization cancelled");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error initializing user panel");
            ShowError("خطا در بارگذاری صفحه. لطفاً دوباره تلاش کنید.");
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void ShowProfileTab() { _activeTab = TabType.Profile; }
    private void ShowDealsTab()   { _activeTab = TabType.Deals;   }

    // ── Create Deal FAB ───────────────────────────────────────────────────────
    private bool _createDealOpen;
    private void OpenCreateDeal()  { _createDealOpen = true; }
    private void CloseCreateDeal() { _createDealOpen = false; }
    private void HandleDealCreated(string dealId)
    {
        _createDealOpen = false;
        _activeTab = TabType.Deals;
        // Reload state to surface the new deal in the table
        _ = InvokeAsync(async () =>
        {
            _isLoading = true;
            StateHasChanged();
            try
            {
                var userId = await PanelService.GetCurrentUserIdAsync(_cts?.Token ?? default);
                if (!string.IsNullOrEmpty(userId))
                    _state = await PanelService.LoadUserPanelStateAsync(userId, _cts?.Token ?? default);
            }
            finally { _isLoading = false; StateHasChanged(); }
        });
    }

    private void ShowDealDetails(Deal deal) { _selectedDeal = deal; }
    private void CloseDealDetails()         { _selectedDeal = null; }

    private async Task HandleSignOut()
    {
        try
        {
            _isLoading = true;
            await PanelService.ClearSessionAsync(_cts?.Token ?? default);
            NavigationService.NavigateTo("/auth/login");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error signing out");
            ShowError("خطا در خروج از حساب. لطفاً دوباره تلاش کنید.");
        }
        finally { _isLoading = false; }
    }

    private void NavigateToLogin() => NavigationService.NavigateTo("/auth/login");
    private void ShowError(string message) { _hasError = true; _errorMessage = message; }
    private void ClearError()              { _hasError = false; _errorMessage = null;   }

    protected override bool ShouldRender() => true;

    public async ValueTask DisposeAsync()
    {
        if (_cts != null) { _cts.Cancel(); _cts.Dispose(); _cts = null; }
        await Task.CompletedTask;
    }
}
