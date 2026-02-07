using Microsoft.AspNetCore.Components;
using PicoPlus.State.UserPanel;
using PicoPlus.ViewModels.User;

namespace PicoPlus.Pages.User;

/// <summary>
/// Thin page code-behind that delegates UI/business state to UserPanelViewModel.
/// </summary>
public partial class Panel : ComponentBase, IAsyncDisposable
{
    [Inject] private UserPanelViewModel ViewModel { get; set; } = default!;

    private UserPanelViewModel _vm = default!;
    private CancellationTokenSource? _cts;

    protected override async Task OnInitializedAsync()
    {
        _vm = ViewModel;
        _cts = new CancellationTokenSource();
        await _vm.InitializeAsync(_cts.Token);
    }

    private void ShowProfileTab() => _vm.ShowProfileTab();

    private void ShowDealsTab() => _vm.ShowDealsTab();

    private void ShowDealDetails(DealSummary deal) => _vm.ShowDealDetails(deal);

    private void CloseDealDetails() => _vm.CloseDealDetails();

    private Task HandleSignOut() => _vm.SignOutAsync(_cts?.Token ?? default);

    private void NavigateToLogin() => _vm.NavigateToLogin();

    private void ClearError() => _vm.ClearTransientError();

    public async ValueTask DisposeAsync()
    {
        if (_cts is not null)
        {
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }

        await Task.CompletedTask;
    }
}
