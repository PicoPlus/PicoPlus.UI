using PicoPlus.Features.UserHome.Application.DTOs;
using PicoPlus.Models.CRM.Commerce;
using PicoPlus.Models.CRM.Objects;

namespace PicoPlus.Features.UserHome.State;

public sealed class UserHomeState
{
    public event Action? Changed;

    public UserHomeDto? Data { get; private set; }
    public bool IsLoading { get; private set; }
    public string ActiveTab { get; private set; } = "profile";
    public Deal.GetBatch.Response.Result? SelectedDeal { get; private set; }
    public IReadOnlyList<LineItem.Read.Response> SelectedDealLineItems { get; private set; } = [];
    public bool ShowChangeMobileDialog { get; private set; }
    public bool ShowCompleteBirthDateDialog { get; private set; }
    public bool ShowCreateDealDialog { get; private set; }

    public void SetLoading(bool value) { IsLoading = value; Notify(); }
    public void SetData(UserHomeDto data) { Data = data; Notify(); }
    public void SetActiveTab(string tab) { ActiveTab = tab; Notify(); }
    public void SelectDeal(Deal.GetBatch.Response.Result? deal) { SelectedDeal = deal; Notify(); }
    public void SetLineItems(IReadOnlyList<LineItem.Read.Response> items) { SelectedDealLineItems = items; Notify(); }
    public void SetChangeMobileDialog(bool value) { ShowChangeMobileDialog = value; Notify(); }
    public void SetCompleteBirthDateDialog(bool value) { ShowCompleteBirthDateDialog = value; Notify(); }
    public void SetCreateDealDialog(bool value) { ShowCreateDealDialog = value; Notify(); }

    private void Notify() => Changed?.Invoke();
}
