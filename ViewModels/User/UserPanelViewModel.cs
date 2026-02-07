using CommunityToolkit.Mvvm.ComponentModel;
using PicoPlus.Infrastructure.Services;
using PicoPlus.Services.UserPanel;
using PicoPlus.State.UserPanel;

namespace PicoPlus.ViewModels.User;

/// <summary>
/// ViewModel for /user/panel page to keep UI state and business operations separated from markup.
/// </summary>
public partial class UserPanelViewModel : BaseViewModel
{
    private readonly IUserPanelService _panelService;
    private readonly INavigationService _navigationService;
    private readonly ILogger<UserPanelViewModel> _logger;

    [ObservableProperty]
    private UserPanelState? state;

    [ObservableProperty]
    private DealSummary? selectedDeal;

    [ObservableProperty]
    private TabType activeTab = TabType.Profile;

    public UserPanelViewModel(
        IUserPanelService panelService,
        INavigationService navigationService,
        ILogger<UserPanelViewModel> logger)
    {
        _panelService = panelService;
        _navigationService = navigationService;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await ExecuteAsync(async () =>
        {
            var userId = await _panelService.GetCurrentUserIdAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogWarning("User not authenticated, redirecting to login");
                _navigationService.NavigateTo("/auth/login");
                return;
            }

            State = await _panelService.LoadUserPanelStateAsync(userId, cancellationToken);
            if (State is null)
            {
                HasError = true;
                ErrorMessage = "بارگذاری اطلاعات پنل کاربری با خطا مواجه شد.";
                return;
            }

            ActiveTab = TabType.Profile;
            _logger.LogInformation("User panel ViewModel initialized for user: {UserId}", userId);
        }, cancellationToken);
    }

    public void ShowProfileTab() => ActiveTab = TabType.Profile;

    public void ShowDealsTab() => ActiveTab = TabType.Deals;

    public void ShowDealDetails(DealSummary deal) => SelectedDeal = deal;

    public void CloseDealDetails() => SelectedDeal = null;

    public async Task SignOutAsync(CancellationToken cancellationToken = default)
    {
        await ExecuteAsync(async () =>
        {
            await _panelService.ClearSessionAsync(cancellationToken);
            _navigationService.NavigateTo("/auth/login");
        }, cancellationToken);
    }

    public void NavigateToLogin() => _navigationService.NavigateTo("/auth/login");

    public void ClearTransientError()
    {
        HasError = false;
        ErrorMessage = string.Empty;
    }
}
