using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PicoPlus.Infrastructure.Services;
using PicoPlus.Infrastructure.State;
using Microsoft.Extensions.Logging;
using ContactModel = PicoPlus.Models.CRM.Objects.Contact;
using DealService = PicoPlus.Services.CRM.Objects.Deal;
using DealModel = PicoPlus.Models.CRM.Objects.Deal;
using PipelineService = PicoPlus.Services.CRM.Pipelines;
using PipelineModel = PicoPlus.Models.CRM.Pipelines;
using OwnerService = PicoPlus.Services.CRM.Owners;
using OwnerModel = PicoPlus.Models.CRM.Owners;
using PicoPlus.Services.CRM.Commerce;
using PicoPlus.Services.SMS;

namespace PicoPlus.ViewModels.Deal;

/// <summary>
/// ViewModel for Deal Create dialog/page
/// </summary>
public partial class DealCreateViewModel : BaseViewModel
{
    private readonly DealService _dealService;
    private readonly PipelineService _pipelineService;
    private readonly OwnerService _ownerService;
    private readonly LineItem _lineItemService;
    private readonly SMS.Send _smsService;
    private readonly IDialogService _dialogService;
    private readonly ISessionStorageService _sessionStorage;
    private readonly AuthenticationStateService _authState;
    private readonly ILogger<DealCreateViewModel> _logger;

    [ObservableProperty]
    private ContactModel.Search.Response.Result? contactModel;

    [ObservableProperty]
    private string userId = string.Empty;

    [ObservableProperty]
    private PipelineModel.List pipelineList = new();

    [ObservableProperty]
    private OwnerModel.GetAll ownerList = new();

    [ObservableProperty]
    private string selectedPipelineId = string.Empty;

    [ObservableProperty]
    private string selectedDealStage = string.Empty;

    [ObservableProperty]
    private string selectedOwnerId = string.Empty;

    [ObservableProperty]
    private string dealName = string.Empty;

    [ObservableProperty]
    private string dealAmount = string.Empty;

    private readonly List<DealModel.Create.Request.Association> _dealAssociations = new();

    public DealCreateViewModel(
        DealService dealService,
        PipelineService pipelineService,
        OwnerService ownerService,
        LineItem lineItemService,
        SMS.Send smsService,
        IDialogService dialogService,
        ISessionStorageService sessionStorage,
        AuthenticationStateService authState,
        ILogger<DealCreateViewModel> logger)
    {
        _dealService = dealService;
        _pipelineService = pipelineService;
        _ownerService = ownerService;
        _lineItemService = lineItemService;
        _smsService = smsService;
        _dialogService = dialogService;
        _sessionStorage = sessionStorage;
        _authState = authState;
        _logger = logger;
        Title = "????? ?????";
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await ExecuteAsync(async () =>
        {
            ContactModel = await _sessionStorage.GetItemAsync<ContactModel.Search.Response.Result>("ContactModel", cancellationToken);
            ContactModel ??= _authState.CurrentUser;

            if (ContactModel is null)
            {
                _logger.LogWarning("No contact model available");
                return;
            }

            UserId = ContactModel.id;
            _logger.LogInformation("Initializing deal creation for user: {UserId}", UserId);

            await LoadPipelinesAsync(cancellationToken);
            await LoadOwnersAsync(cancellationToken);

        }, cancellationToken);
    }

    private async Task LoadPipelinesAsync(CancellationToken cancellationToken)
    {
        PipelineList = await _pipelineService.GetPipelines("deals");
        _logger.LogInformation("Loaded {Count} pipelines", PipelineList.results?.Count ?? 0);
    }

    private async Task LoadOwnersAsync(CancellationToken cancellationToken)
    {
        OwnerList = await _ownerService.GetAll();
        _logger.LogInformation("Loaded {Count} owners", OwnerList.results?.Count ?? 0);
    }

    [RelayCommand]
    private async Task SubmitDealAsync(List<Models.CRM.Commerce.LineItem.Create.Request.Input> lineItems, CancellationToken cancellationToken)
    {
        await ExecuteAsync(async () =>
        {
            if (!ValidateInput())
                return;

            if (lineItems is null || lineItems.Count == 0)
            {
                ErrorMessage = "????? ????? ?? ???? ?? ????? ????? ????";
                HasError = true;
                return;
            }

            _logger.LogInformation("Creating deal: {DealName}", DealName);

            var createdLineItems = await _lineItemService.CreateLineAsync(new Models.CRM.Commerce.LineItem.Create.Request
            {
                inputs = lineItems
            });

            _logger.LogInformation("Created {Count} line items with status: {Status}",
                createdLineItems.results?.Count ?? 0,
                createdLineItems.status);

            _dealAssociations.Clear();

            if (createdLineItems.results is not null)
            {
                foreach (var lineItem in createdLineItems.results)
                {
                    _dealAssociations.Add(new DealModel.Create.Request.Association
                    {
                        to = new DealModel.Create.Request.To
                        {
                            id = long.Parse(lineItem.id)
                        },
                        types = new List<DealModel.Create.Request.Type>
                        {
                            new()
                            {
                                associationCategory = "HUBSPOT_DEFINED",
                                associationTypeId = 19
                            }
                        }
                    });
                }
            }

            if (!string.IsNullOrEmpty(UserId))
            {
                _dealAssociations.Add(new DealModel.Create.Request.Association
                {
                    to = new DealModel.Create.Request.To
                    {
                        id = long.Parse(UserId)
                    },
                    types = new List<DealModel.Create.Request.Type>
                    {
                        new()
                        {
                            associationCategory = "HUBSPOT_DEFINED",
                            associationTypeId = 3
                        }
                    }
                });
            }

            var deal = await _dealService.Create(new DealModel.Create.Request
            {
                properties = new DealModel.Create.Request.Properties
                {
                    amount = DealAmount,
                    dealname = DealName,
                    hubspot_owner_id = SelectedOwnerId,
                    dealstage = SelectedDealStage,
                    pipeline = SelectedPipelineId
                },
                associations = _dealAssociations
            });

            _logger.LogInformation("Deal created successfully: {DealId}", deal.id);
            await _dialogService.ShowSuccessAsync("????", $"????? ?? ?????? ????? ??. ?????: {deal.id}");

            if (deal.properties.dealstage == "closedwon" && ContactModel?.properties is not null)
            {
                await SendDealClosedWonSmsAsync(deal.id, cancellationToken);
            }

        }, cancellationToken);
    }

    private bool ValidateInput()
    {
        if (string.IsNullOrWhiteSpace(DealName))
        {
            ErrorMessage = "????? ????? ????????? ???? ????";
            HasError = true;
            return false;
        }

        if (string.IsNullOrWhiteSpace(SelectedPipelineId))
        {
            ErrorMessage = "?????? ????? ?????? ???";
            HasError = true;
            return false;
        }

        if (string.IsNullOrWhiteSpace(SelectedDealStage))
        {
            ErrorMessage = "?????? ????? ????? ?????? ???";
            HasError = true;
            return false;
        }

        if (string.IsNullOrWhiteSpace(DealAmount))
        {
            ErrorMessage = "???? ????? ????????? ???? ????";
            HasError = true;
            return false;
        }

        return true;
    }

    private async Task SendDealClosedWonSmsAsync(string dealId, CancellationToken cancellationToken)
    {
        try
        {
            if (ContactModel?.properties is null)
                return;

            _logger.LogInformation("Sending deal closed won SMS");

            await _smsService.SendDealClosedWon(new Models.Services.SMS.SMS.DealClosedWon
            {
                toNum = ContactModel.properties.phone,
                patternCode = "sarlemrkderzb4c",
                inputData = new List<Models.Services.SMS.SMS.DealClosedWonInputdata>
                {
                    new()
                    {
                        firstname = ContactModel.properties.firstname,
                        lastname = ContactModel.properties.lastname,
                        id = dealId
                    }
                }
            });

            _logger.LogInformation("Deal closed won SMS sent successfully");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send deal closed won SMS");
        }
    }

    protected override void OnError(Exception exception)
    {
        _logger.LogError(exception, "Error in DealCreateViewModel");
    }
}
