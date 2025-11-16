using System.ComponentModel.DataAnnotations;
using PicoPlus.Services.CRM.Objects;
using PicoPlus.Services.CRM;
using PicoPlus.Infrastructure.Services;

namespace PicoPlus.ViewModels.Deal;

/// <summary>
/// ViewModel for Create Deal Dialog
/// Handles all business logic for creating a new deal with HubSpot-like interface
/// </summary>
public class DealCreateDialogViewModel : BaseViewModel
{
    private readonly Services.CRM.Objects.Deal _dealService;
    private readonly Pipelines _pipelineService;
    private readonly Owners _ownerService;
    private readonly Services.CRM.Commerce.LineItem _lineItemService;
    private readonly IDialogService _dialogService;

    public DealCreateDialogViewModel(
        Services.CRM.Objects.Deal dealService,
        Pipelines pipelineService,
        Owners ownerService,
        Services.CRM.Commerce.LineItem lineItemService,
        IDialogService dialogService)
    {
        _dealService = dealService;
        _pipelineService = pipelineService;
        _ownerService = ownerService;
        _lineItemService = lineItemService;
        _dialogService = dialogService;
    }

    #region Properties

    private string? _contactId;
    public string? ContactId
    {
        get => _contactId;
        set => SetProperty(ref _contactId, value);
    }

    private string _dealName = string.Empty;
    [Required(ErrorMessage = "??? ?????? ?????? ???")]
    public string DealName
    {
        get => _dealName;
        set => SetProperty(ref _dealName, value);
    }

    private string? _description;
    public string? Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    private string _selectedPipelineId = string.Empty;
    [Required(ErrorMessage = "?????? ?? ???? ?????? ???")]
    public string SelectedPipelineId
    {
        get => _selectedPipelineId;
        set
        {
            if (SetProperty(ref _selectedPipelineId, value))
            {
                UpdateDealStages();
            }
        }
    }

    private string _selectedDealStage = string.Empty;
    [Required(ErrorMessage = "?????? ????? ?????? ?????? ???")]
    public string SelectedDealStage
    {
        get => _selectedDealStage;
        set => SetProperty(ref _selectedDealStage, value);
    }

    private string _selectedOwnerId = string.Empty;
    [Required(ErrorMessage = "?????? ????? ?????? ?????? ???")]
    public string SelectedOwnerId
    {
        get => _selectedOwnerId;
        set => SetProperty(ref _selectedOwnerId, value);
    }

    private decimal? _amount;
    public decimal? Amount
    {
        get => _amount;
        set => SetProperty(ref _amount, value);
    }

    private decimal? _taxAmount;
    public decimal? TaxAmount
    {
        get => _taxAmount;
        set => SetProperty(ref _taxAmount, value);
    }

    private DateTime? _closeDate;
    public DateTime? CloseDate
    {
        get => _closeDate;
        set => SetProperty(ref _closeDate, value);
    }

    private string? _dealType;
    public string? DealType
    {
        get => _dealType;
        set => SetProperty(ref _dealType, value);
    }

    private string? _priority;
    public string? Priority
    {
        get => _priority;
        set => SetProperty(ref _priority, value);
    }

    private List<Models.CRM.Pipelines.List.Result>? _pipelines;
    public List<Models.CRM.Pipelines.List.Result>? Pipelines
    {
        get => _pipelines;
        set => SetProperty(ref _pipelines, value);
    }

    private List<Models.CRM.Pipelines.List.Stage>? _dealStages;
    public List<Models.CRM.Pipelines.List.Stage>? DealStages
    {
        get => _dealStages;
        set => SetProperty(ref _dealStages, value);
    }

    private List<Models.CRM.Owners.GetAll.Result>? _owners;
    public List<Models.CRM.Owners.GetAll.Result>? Owners
    {
        get => _owners;
        set => SetProperty(ref _owners, value);
    }

    private List<Models.CRM.Commerce.LineItem.Create.Request.Input>? _lineItems;
    public List<Models.CRM.Commerce.LineItem.Create.Request.Input>? LineItems
    {
        get => _lineItems;
        set => SetProperty(ref _lineItems, value);
    }

    #endregion

    #region Initialization

    public async Task InitializeAsync(string? contactId)
    {
        ContactId = contactId;

        try
        {
            IsLoading = true;
            ErrorMessage = null;

            // Load pipelines and owners in parallel
            var pipelinesTask = LoadPipelinesAsync();
            var ownersTask = LoadOwnersAsync();

            await Task.WhenAll(pipelinesTask, ownersTask);

            // Set default close date to 30 days from now
            CloseDate = DateTime.Now.AddDays(30);

            // Initialize line items list
            LineItems = new List<Models.CRM.Commerce.LineItem.Create.Request.Input>();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"??? ?? ???????? ???????: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadPipelinesAsync()
    {
        try
        {
            var response = await _pipelineService.GetPipelines("deals");
            Pipelines = response.results;

            // Set default pipeline if available
            if (Pipelines?.Any() == true)
            {
                SelectedPipelineId = Pipelines.First().id;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"??? ?? ???????? ???? ????: {ex.Message}";
        }
    }

    private async Task LoadOwnersAsync()
    {
        try
        {
            var response = await _ownerService.GetAll();
            Owners = response.results;

            // Set default owner if available
            if (Owners?.Any() == true)
            {
                SelectedOwnerId = Owners.First().id;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"??? ?? ???????? ???????: {ex.Message}";
        }
    }

    private void UpdateDealStages()
    {
        if (string.IsNullOrEmpty(SelectedPipelineId) || Pipelines == null)
        {
            DealStages = null;
            SelectedDealStage = string.Empty;
            return;
        }

        var selectedPipeline = Pipelines.FirstOrDefault(p => p.id == SelectedPipelineId);
        if (selectedPipeline != null)
        {
            DealStages = selectedPipeline.stages?.ToList();

            // Set first stage as default
            if (DealStages?.Any() == true)
            {
                SelectedDealStage = DealStages.First().id;
            }
        }
    }

    #endregion

    #region Commands

    public async Task<string?> CreateDealAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            // Validate required fields
            if (string.IsNullOrWhiteSpace(DealName))
            {
                ErrorMessage = "??? ?????? ?????? ???";
                return null;
            }

            if (string.IsNullOrWhiteSpace(SelectedPipelineId))
            {
                ErrorMessage = "?????? ?? ???? ?????? ???";
                return null;
            }

            if (string.IsNullOrWhiteSpace(SelectedDealStage))
            {
                ErrorMessage = "?????? ????? ?????? ?????? ???";
                return null;
            }

            if (string.IsNullOrWhiteSpace(SelectedOwnerId))
            {
                ErrorMessage = "?????? ????? ?????? ?????? ???";
                return null;
            }

            // Build associations list
            var associations = new List<Models.CRM.Objects.Deal.Create.Request.Association>();

            // Add contact association if available
            if (!string.IsNullOrEmpty(ContactId))
            {
                associations.Add(new Models.CRM.Objects.Deal.Create.Request.Association
                {
                    to = new Models.CRM.Objects.Deal.Create.Request.To
                    {
                        id = long.Parse(ContactId)
                    },
                    types = new List<Models.CRM.Objects.Deal.Create.Request.Type>
                    {
                        new()
                        {
                            associationCategory = "HUBSPOT_DEFINED",
                            associationTypeId = 3 // Contact to Deal association
                        }
                    }
                });
            }

            // Create line items if any
            if (LineItems != null && LineItems.Any())
            {
                var lineItemsRequest = new Models.CRM.Commerce.LineItem.Create.Request
                {
                    inputs = LineItems
                };

                var createdLineItems = await _lineItemService.CreateLineAsync(lineItemsRequest);

                if (createdLineItems.results != null)
                {
                    foreach (var lineItem in createdLineItems.results)
                    {
                        associations.Add(new Models.CRM.Objects.Deal.Create.Request.Association
                        {
                            to = new Models.CRM.Objects.Deal.Create.Request.To
                            {
                                id = long.Parse(lineItem.id)
                            },
                            types = new List<Models.CRM.Objects.Deal.Create.Request.Type>
                            {
                                new()
                                {
                                    associationCategory = "HUBSPOT_DEFINED",
                                    associationTypeId = 19 // Line Item to Deal association
                                }
                            }
                        });
                    }
                }

                // Calculate total amount from line items
                var totalAmount = LineItems.Sum(li => li.properties.TotalPrice);
                Amount = totalAmount;
            }

            // Build deal request
            var request = new Models.CRM.Objects.Deal.Create.Request
            {
                properties = new Models.CRM.Objects.Deal.Create.Request.Properties
                {
                    dealname = DealName,
                    pipeline = SelectedPipelineId,
                    dealstage = SelectedDealStage,
                    hubspot_owner_id = SelectedOwnerId,
                    description = Description,
                    amount = Amount?.ToString(),
                    tax_amount = TaxAmount?.ToString(),
                    deal_type = DealType,
                    hs_priority = Priority,
                    closedate = CloseDate.HasValue
                        ? new DateTimeOffset(CloseDate.Value).ToUnixTimeMilliseconds()
                        : 0
                },
                associations = associations
            };

            // Create the deal
            var response = await _dealService.Create(request);

            await _dialogService.ShowSuccessAsync("????", $"?????? ?? ?????? ????? ??. ?????: {response.id}");

            return response.id;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"??? ?? ????? ??????: {ex.Message}";
            await _dialogService.ShowErrorAsync("???", ErrorMessage);
            return null;
        }
        finally
        {
            IsLoading = false;
        }
    }

    #endregion

    #region Helper Methods

    public void Reset()
    {
        DealName = string.Empty;
        Description = null;
        SelectedDealStage = string.Empty;
        Amount = null;
        TaxAmount = null;
        CloseDate = DateTime.Now.AddDays(30);
        DealType = null;
        Priority = null;
        LineItems = new List<Models.CRM.Commerce.LineItem.Create.Request.Input>();
        ErrorMessage = null;

        // Reset to first pipeline and its first stage
        if (Pipelines?.Any() == true)
        {
            SelectedPipelineId = Pipelines.First().id;
        }

        // Reset to first owner
        if (Owners?.Any() == true)
        {
            SelectedOwnerId = Owners.First().id;
        }
    }

    #endregion
}
