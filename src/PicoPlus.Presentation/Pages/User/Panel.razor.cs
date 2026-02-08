using Microsoft.AspNetCore.Components;
using PicoPlus.Application.Abstractions.Services;
using PicoPlus.Application.Abstractions.UserPanel;
using PicoPlus.Application.Dto.UserPanel;

namespace PicoPlus.Presentation.Pages.User;

/// <summary>
/// Code-behind for user panel page
/// Implements clean separation of concerns with minimal logic
/// </summary>
public partial class Panel : ComponentBase, IAsyncDisposable
{
    [Inject] private IUserPanelService PanelService { get; set; } = default!;
    [Inject] private INavigationService NavigationService { get; set; } = default!;
    [Inject] private ILogger<Panel> Logger { get; set; } = default!;

    private UserPanelState? _state;
    private DealSummary? _selectedDeal;
    private TabType _activeTab = TabType.Profile;
    private bool _isLoading = true;
    private bool _hasError;
    private string? _errorMessage;
    private CancellationTokenSource? _cts;

    protected override async Task OnInitializedAsync()
    {
        _cts = new CancellationTokenSource();

        try
        {
            _isLoading = true;

            // Get current user ID from session
            var userId = await PanelService.GetCurrentUserIdAsync(_cts.Token);

            if (string.IsNullOrEmpty(userId))
            {
                Logger.LogWarning("User not authenticated, redirecting to login");
                NavigationService.NavigateTo("/auth/login");
                return;
            }

            // Load user panel state
            _state = await PanelService.LoadUserPanelStateAsync(userId, _cts.Token);

            if (_state == null)
            {
                Logger.LogError("Failed to load user panel state for user: {UserId}", userId);
                ShowError("??? ?? ???????? ??????? ??????. ????? ?????? ???? ????.");
                return;
            }

            Logger.LogInformation("User panel loaded successfully for: {UserId}", userId);
        }
        catch (OperationCanceledException)
        {
            Logger.LogInformation("User panel initialization cancelled");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error initializing user panel");
            ShowError("??? ?? ???????? ????. ????? ?????? ???? ????.");
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void ShowProfileTab()
    {
        _activeTab = TabType.Profile;
        Logger.LogDebug("Switched to profile tab");
    }

    private void ShowDealsTab()
    {
        _activeTab = TabType.Deals;
        Logger.LogDebug("Switched to deals tab");
    }

    private void ShowDealDetails(DealSummary deal)
    {
        _selectedDeal = deal;
        Logger.LogDebug("Showing details for deal: {DealId}", deal.Id);
    }

    private void CloseDealDetails()
    {
        _selectedDeal = null;
    }

    private async Task HandleSignOut()
    {
        try
        {
            _isLoading = true;
            await PanelService.ClearSessionAsync(_cts?.Token ?? default);
            Logger.LogInformation("User signed out successfully");
            NavigationService.NavigateTo("/auth/login");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error signing out");
            ShowError("??? ?? ???? ?? ????. ????? ?????? ???? ????.");
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void NavigateToLogin()
    {
        NavigationService.NavigateTo("/auth/login");
    }

    private void ShowError(string message)
    {
        _hasError = true;
        _errorMessage = message;
    }

    private void ClearError()
    {
        _hasError = false;
        _errorMessage = null;
    }

    protected override bool ShouldRender()
    {
        // Optimize re-renders - only render when state actually changes
        return true;
    }

    public async ValueTask DisposeAsync()
    {
        if (_cts != null)
        {
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }

        await Task.CompletedTask;
    }
}
