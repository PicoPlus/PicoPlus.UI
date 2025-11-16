using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PicoPlus.Infrastructure.Services;
using PicoPlus.Infrastructure.State;
using PicoPlus.Services.CRM.Commerce;
using PicoPlus.Services.CRM.Objects;
using PicoPlus.Services.CRM;
using PicoPlus.Services.Identity;
using ContactModel = PicoPlus.Models.CRM.Objects.Contact;
using DealModel = PicoPlus.Models.CRM.Objects.Deal;
using ProductModel = PicoPlus.Models.CRM.Commerce.Products;
using DealService = PicoPlus.Services.CRM.Objects.Deal;
using AssociateService = PicoPlus.Services.CRM.Associate;
using System.Linq;
using System.Globalization;

namespace PicoPlus.ViewModels.User;

/// <summary>
/// ViewModel for User Home/Panel page
/// </summary>
public partial class UserHomeViewModel : BaseViewModel
{
    private readonly Product _productService;
    private readonly DealService _dealService;
    private readonly AssociateService _associateService;
    private readonly Contact _contactService;
    private readonly Zibal _zibalService;
    private readonly LineItem _lineItemService;
    private readonly INavigationService _navigationService;
    private readonly ISessionStorageService _sessionStorage;
    private readonly IDialogService _dialogService;
    private readonly AuthenticationStateService _authState;
    private readonly ILogger<UserHomeViewModel> _logger;

    [ObservableProperty]
    private ContactModel.Search.Response.Result? contactModel;

    [ObservableProperty]
    private List<ProductModel.Get.Response.Result> paginatedItems = new();

    [ObservableProperty]
    private List<DealModel.GetBatch.Response.Result> deals = new();

    [ObservableProperty]
    private int totalDeals;

    [ObservableProperty]
    private int closedDeals;

    [ObservableProperty]
    private int openDeals;

    [ObservableProperty]
    private decimal totalRevenue;

    [ObservableProperty]
    private decimal walletBalance;

    [ObservableProperty]
    private bool showChangeMobileDialog = false;

    [ObservableProperty]
    private bool showCompleteBirthDateDialog = false;

    [ObservableProperty]
    private bool showCreateDealDialog = false;

    [ObservableProperty]
    private List<Models.CRM.Commerce.LineItem.Read.Response>? selectedDealLineItems;

    // Avatar URL (from HubSpot file property)
    public string? AvatarUrl { get; private set; }

    public UserHomeViewModel(
        Product productService,
        DealService dealService,
        AssociateService associateService,
        Contact contactService,
        Zibal zibalService,
        LineItem lineItemService,
        INavigationService navigationService,
        ISessionStorageService sessionStorage,
        IDialogService dialogService,
        AuthenticationStateService authState,
        ILogger<UserHomeViewModel> logger)
    {
        _productService = productService;
        _dealService = dealService;
        _associateService = associateService;
        _contactService = contactService;
        _zibalService = zibalService;
        _lineItemService = lineItemService;
        _navigationService = navigationService;
        _sessionStorage = sessionStorage;
        _dialogService = dialogService;
        _authState = authState;
        _logger = logger;
        Title = "??? ??????";
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await ExecuteAsync(async () =>
        {
            ContactModel = await _sessionStorage.GetItemAsync<ContactModel.Search.Response.Result>("ContactModel", cancellationToken);

            if (ContactModel is null)
            {
                _logger.LogWarning("User not authenticated, redirecting to login");
                _navigationService.NavigateTo("/auth/login");
                return;
            }

            // Debug log all properties
            _logger.LogInformation("Contact loaded - ID: {Id}, Phone: {Phone}, Email: {Email}, Gender: {Gender}, FatherName: {FatherName}, ShahkarStatus: {Shahkar}, BirthDate: {BirthDate}",
                ContactModel.id,
                ContactModel.properties?.phone ?? "NULL",
                ContactModel.properties?.email ?? "NULL",
                ContactModel.properties?.gender ?? "NULL",
                ContactModel.properties?.father_name ?? "NULL",
                ContactModel.properties?.shahkar_status ?? "NULL",
                ContactModel.properties?.dateofbirth ?? "NULL");

            _authState.SetAuthenticatedUser(ContactModel);

            // Set avatar URL if exists
            AvatarUrl = ContactModel.properties?.last_products_bought_product_1_image_url;

            // Load real data
            await LoadUserDealsAsync(cancellationToken);
            LoadUserStatistics();

            // Check if birth date is missing - show completion dialog ONCE
            if (string.IsNullOrWhiteSpace(ContactModel.properties?.dateofbirth))
            {
                _logger.LogInformation("Birth date is missing for contact: {ContactId}. Showing completion dialog.", ContactModel.id);
                ShowCompleteBirthDateDialog = true;
            }

            _logger.LogInformation("User panel initialized for: {UserId}", ContactModel.id);
        }, cancellationToken);
    }

    private async Task LoadUserDealsAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (ContactModel?.id == null) return;

            // Get associated deals
            var associations = await _associateService.ListAssoc(ContactModel.id, "contact", "deals");

            if (associations?.results == null || !associations.results.Any())
            {
                _logger.LogInformation("No deals found for contact: {ContactId}", ContactModel.id);
                Deals = new List<DealModel.GetBatch.Response.Result>();
                return;
            }

            // Fetch deal details in batch
            var dealIds = associations.results.Select(r => r.toObjectId.ToString()).ToList();

            var batchRequest = new DealModel.GetBatch.Request
            {
                inputs = dealIds.Select(id => new DealModel.GetBatch.Request.Input { id = id }).ToList(),
                properties = new List<string>
                {
                    "dealname",
                    "amount",
                    "dealstage",
                    "createdate",
                    "hs_lastmodifieddate",
                    "closedate",
                    "pipeline"
                }
            };

            var dealsResponse = await _dealService.GetDeals(batchRequest);

            if (dealsResponse?.results != null)
            {
                Deals = dealsResponse.results.OrderByDescending(d => d.createdAt).ToList();
                _logger.LogInformation("Loaded {Count} deals for contact", Deals.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading deals for contact: {ContactId}", ContactModel?.id);
            Deals = new List<DealModel.GetBatch.Response.Result>();
        }
    }

    private void LoadUserStatistics()
    {
        TotalDeals = Deals.Count;
        ClosedDeals = Deals.Count(d => d.properties.dealstage?.Contains("closed") == true);
        OpenDeals = TotalDeals - ClosedDeals;

        // Calculate total revenue from closed won deals
        TotalRevenue = Deals
            .Where(d => d.properties.dealstage?.Contains("closedwon") == true)
            .Sum(d => decimal.TryParse(d.properties.amount, out var amount) ? amount : 0m);

        // Get wallet balance from contact
        WalletBalance = decimal.TryParse(ContactModel?.properties?.wallet, out var wallet) ? wallet : 0m;
    }

    [RelayCommand]
    private async Task SignOutAsync(CancellationToken cancellationToken)
    {
        await ExecuteAsync(async () =>
        {
            await _sessionStorage.RemoveItemAsync("LogInState", cancellationToken);
            await _sessionStorage.RemoveItemAsync("ContactModel", cancellationToken);
            await _sessionStorage.RemoveItemAsync("UserRole", cancellationToken);
            _authState.ClearAuthentication();
            _logger.LogInformation("User signed out");
            _navigationService.NavigateTo("/auth/login");
        }, cancellationToken);
    }

    [RelayCommand]
    private async Task ChangeMobileAsync(string newMobile, CancellationToken cancellationToken)
    {
        await ExecuteAsync(async () =>
        {
            if (ContactModel == null || string.IsNullOrEmpty(ContactModel.id))
            {
                await _dialogService.ShowErrorAsync("???", "??????? ?????? ???? ???");
                return;
            }

            _logger.LogInformation("Changing mobile number from {OldMobile} to {NewMobile}",
                ContactModel.properties?.phone, newMobile);

            try
            {
                // Update contact phone in HubSpot
                var properties = new Dictionary<string, string>
                {
                    ["phone"] = newMobile
                };

                await _contactService.UpdateContactProperties(ContactModel.id, properties);
                _logger.LogInformation("Phone number updated in HubSpot for contact: {ContactId}", ContactModel.id);

                // Verify new phone with Shahkar
                try
                {
                    var shahkarResponse = await _zibalService.ShahkarInquiryAsync(
                        new Models.Services.Identity.Zibal.ShahkarInquiry.Request
                        {
                            mobile = newMobile,
                            nationalCode = ContactModel.properties.natcode
                        });

                    string shahkarStatus = "0";
                    if (shahkarResponse?.result == 100 && shahkarResponse.data?.matched == true)
                    {
                        shahkarStatus = "100";
                        _logger.LogInformation("Shahkar verification successful for new mobile");
                    }
                    else if (shahkarResponse?.result == 100 && shahkarResponse.data?.matched == false)
                    {
                        shahkarStatus = "101";
                        _logger.LogWarning("Shahkar verification: Phone not matched with national code");
                    }
                    else
                    {
                        shahkarStatus = shahkarResponse?.result?.ToString() ?? "999";
                        _logger.LogWarning("Shahkar verification returned unexpected result: {Result}", shahkarStatus);
                    }

                    // Update Shahkar status
                    properties["shahkar_status"] = shahkarStatus;
                    await _contactService.UpdateContactProperties(ContactModel.id, properties);
                    _logger.LogInformation("Shahkar status updated to: {Status}", shahkarStatus);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to verify Shahkar for new mobile, continuing anyway");
                    // Don't fail the mobile change if Shahkar verification fails
                }

                // Refresh contact data from HubSpot
                var updatedContact = await _contactService.Read(ContactModel.id, new[]
                {
                    "firstname", "lastname", "email", "phone", "natcode",
                    "dateofbirth", "father_name", "gender", "shahkar_status",
                    "wallet", "total_revenue", "num_associated_deals", "contact_plan"
                });

                // Update ContactModel with fresh data
                ContactModel.properties.phone = updatedContact.properties.phone;
                ContactModel.properties.shahkar_status = updatedContact.properties.shahkar_status;

                // Update session storage
                await _sessionStorage.SetItemAsync("ContactModel", ContactModel, cancellationToken);

                await _dialogService.ShowSuccessAsync(
                    "????",
                    "????? ?????? ?? ?????? ????? ???");

                ShowChangeMobileDialog = false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing mobile number");
                await _dialogService.ShowErrorAsync(
                    "???",
                    "??? ?? ????? ????? ??????. ????? ?????? ???? ????.");
            }
        }, cancellationToken);
    }

    [RelayCommand]
    private async Task CompleteBirthDateAsync(string birthDate, CancellationToken cancellationToken)
    {
        await ExecuteAsync(async () =>
        {
            if (ContactModel == null || string.IsNullOrEmpty(ContactModel.id))
            {
                await _dialogService.ShowErrorAsync("???", "??????? ?????? ???? ???");
                return;
            }

            _logger.LogInformation("Completing birth date for contact: {ContactId}, BirthDate: {BirthDate}",
                ContactModel.id, birthDate);

            try
            {
                // First, save the birth date to HubSpot
                var properties = new Dictionary<string, string>
                {
                    ["dateofbirth"] = birthDate
                };

                await _contactService.UpdateContactProperties(ContactModel.id, properties);
                _logger.LogInformation("Birth date saved to HubSpot: {BirthDate}", birthDate);

                // Now call Zibal National Identity Inquiry to get full profile data
                try
                {
                    _logger.LogInformation("Calling Zibal National Identity Inquiry for: {NatCode}", ContactModel.properties.natcode);

                    var inquiry = await _zibalService.NationalIdentityInquiryAsync(
                        new Models.Services.Identity.Zibal.NationalIdentityInquiry.Request
                        {
                            nationalCode = ContactModel.properties.natcode,
                            birthDate = birthDate,
                            genderInquiry = true
                        });

                    if (inquiry?.result == 1 && inquiry.data?.matched == true)
                    {
                        _logger.LogInformation("Zibal verification successful! Updating profile with: FatherName={FatherName}, Gender={Gender}",
                            inquiry.data.fatherName, inquiry.data.gender);

                        // Update father name if available
                        if (!string.IsNullOrWhiteSpace(inquiry.data.fatherName))
                        {
                            properties["father_name"] = inquiry.data.fatherName;
                        }

                        // Update gender if available
                        if (!string.IsNullOrWhiteSpace(inquiry.data.gender))
                        {
                            properties["gender"] = inquiry.data.gender;
                        }

                        // Update first/last name if they seem more accurate
                        if (!string.IsNullOrWhiteSpace(inquiry.data.firstName))
                        {
                            properties["firstname"] = inquiry.data.firstName;
                        }

                        if (!string.IsNullOrWhiteSpace(inquiry.data.lastName))
                        {
                            properties["lastname"] = inquiry.data.lastName;
                        }

                        // Save all properties to HubSpot (NO isverifiedbycr)
                        await _contactService.UpdateContactProperties(ContactModel.id, properties);
                        _logger.LogInformation("Profile updated with {Count} properties from Zibal", properties.Count);

                        // Refresh contact data from HubSpot
                        var updatedContact = await _contactService.Read(ContactModel.id, new[]
                        {
                            "firstname", "lastname", "email", "phone", "natcode",
                            "dateofbirth", "father_name", "gender", "shahkar_status",
                            "wallet", "total_revenue", "num_associated_deals", "contact_plan"
                        });

                        // Map to Search.Response.Result format and REPLACE ContactModel
                        ContactModel = new ContactModel.Search.Response.Result
                        {
                            id = updatedContact.id,
                            properties = new ContactModel.Search.Response.Result.Properties
                            {
                                email = updatedContact.properties.email,
                                firstname = updatedContact.properties.firstname,
                                lastname = updatedContact.properties.lastname,
                                phone = updatedContact.properties.phone,
                                natcode = updatedContact.properties.natcode,
                                dateofbirth = updatedContact.properties.dateofbirth,
                                father_name = updatedContact.properties.father_name,
                                gender = updatedContact.properties.gender,
                                total_revenue = updatedContact.properties.total_revenue,
                                shahkar_status = updatedContact.properties.shahkar_status,
                                wallet = updatedContact.properties.wallet,
                                num_associated_deals = updatedContact.properties.num_associated_deals,
                                contact_plan = updatedContact.properties.contact_plan
                            },
                            createdAt = updatedContact.createdAt.ToString("o"),
                            updatedAt = updatedContact.updatedAt.ToString("o"),
                            archived = updatedContact.archived
                        };

                        // Update session storage
                        await _sessionStorage.SetItemAsync("ContactModel", ContactModel, cancellationToken);
                        _logger.LogInformation("Session storage updated with new contact data");

                        // Show success message
                        await _dialogService.ShowSuccessAsync(
                            "????",
                            $"??????? ??? ?? ?????? ????? ??!\n??? ???: {inquiry.data.fatherName}\n?????: {inquiry.data.gender}");

                        // Close dialog
                        ShowCompleteBirthDateDialog = false;

                        // Reload statistics to reflect any changes
                        LoadUserStatistics();

                        _logger.LogInformation("Birth date completion successful. UI should refresh automatically.");
                    }
                    else
                    {
                        _logger.LogWarning("Zibal verification failed or not matched. Result: {Result}", inquiry?.result);

                        // Update birth date in current model
                        ContactModel.properties.dateofbirth = birthDate;
                        await _sessionStorage.SetItemAsync("ContactModel", ContactModel, cancellationToken);

                        // Still save the birth date even if verification fails
                        await _dialogService.ShowWarningAsync(
                            "?????",
                            "????? ???? ????? ??? ??? ????? ???? ?? ??? ????? ???? ????. ????? ????? ???? ?? ????? ????.");

                        ShowCompleteBirthDateDialog = false;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error calling Zibal for birth date verification");

                    // Update birth date in current model even if Zibal fails
                    ContactModel.properties.dateofbirth = birthDate;
                    await _sessionStorage.SetItemAsync("ContactModel", ContactModel, cancellationToken);

                    await _dialogService.ShowWarningAsync(
                        "?????",
                        "????? ???? ????? ??? ??? ??? ?? ????? ???? ?? ??? ????? ?? ???.");

                    ShowCompleteBirthDateDialog = false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing birth date");
                await _dialogService.ShowErrorAsync(
                    "???",
                    "??? ?? ????? ????? ????. ????? ?????? ???? ????.");
            }
        }, cancellationToken);
    }

    [RelayCommand]
    private void OpenCreateDealDialog()
    {
        _logger.LogInformation("Opening create deal dialog for contact: {ContactId}", ContactModel?.id);
        ShowCreateDealDialog = true;
    }

    [RelayCommand]
    private void CloseCreateDealDialog()
    {
        _logger.LogInformation("Closing create deal dialog");
        ShowCreateDealDialog = false;
    }

    [RelayCommand]
    private async Task OnDealCreatedAsync(string dealId, CancellationToken cancellationToken)
    {
        await ExecuteAsync(async () =>
        {
            _logger.LogInformation("Deal created successfully: {DealId}, reloading deals", dealId);

            // Close the dialog
            ShowCreateDealDialog = false;

            // Reload deals to show the new one
            await LoadUserDealsAsync(cancellationToken);
            LoadUserStatistics();

            // Show success notification
            await _dialogService.ShowSuccessAsync(
                "????",
                $"?????? ???? ?? ?????? ????? ??!\n?????: {dealId}");
        }, cancellationToken);
    }

    public string GetInitials()
    {
        if (ContactModel?.properties is not null &&
            !string.IsNullOrEmpty(ContactModel.properties.firstname) &&
            !string.IsNullOrEmpty(ContactModel.properties.lastname))
        {
            return $"{ContactModel.properties.firstname[0]}{ContactModel.properties.lastname[0]}";
        }
        return "U";
    }

    public string GetFullName()
    {
        if (ContactModel?.properties is not null)
        {
            var firstName = ContactModel.properties.firstname ?? "";
            var lastName = ContactModel.properties.lastname ?? "";
            return $"{firstName} {lastName}".Trim();
        }
        return "?????";
    }

    public string FormatNumber(decimal? number)
    {
        if (number == null) return "0";
        return string.Format("{0:N0}", number).Replace(",", "?");
    }

    public string FormatDate(DateTime date)
    {
        try
        {
            var persianCalendar = new PersianCalendar();
            int year = persianCalendar.GetYear(date);
            int month = persianCalendar.GetMonth(date);
            int day = persianCalendar.GetDayOfMonth(date);
            return $"{year:D4}/{month:D2}/{day:D2}";
        }
        catch
        {
            return date.ToString("yyyy/MM/dd");
        }
    }

    public string FormatDate(DateTime? date)
    {
        if (date == null) return "-";
        return FormatDate(date.Value);
    }

    public string FormatDate(string? dateString)
    {
        if (string.IsNullOrEmpty(dateString)) return "-";

        if (DateTime.TryParse(dateString, out var date))
        {
            return FormatDate(date);
        }

        return dateString;
    }

    public string GetDealStatusBadge(string? dealStage)
    {
        if (string.IsNullOrEmpty(dealStage)) return "secondary";

        return dealStage.ToLower() switch
        {
            var s when s.Contains("closedwon") => "success",
            var s when s.Contains("closedlost") => "danger",
            var s when s.Contains("qualified") => "info",
            var s when s.Contains("appointment") => "warning",
            _ => "primary"
        };
    }

    public string GetDealStatusText(string? dealStage)
    {
        if (string.IsNullOrEmpty(dealStage)) return "??????";

        return dealStage.ToLower() switch
        {
            var s when s.Contains("closedwon") => "???? ???",
            var s when s.Contains("closedlost") => "?? ???",
            var s when s.Contains("qualified") => "???? ?????",
            var s when s.Contains("appointment") => "?? ??????",
            var s when s.Contains("presentation") => "????? ???",
            var s when s.Contains("decision") => "?? ??? ?????",
            _ => "????"
        };
    }

    // Additional property getters for enhanced profile display
    public string Gender => ContactModel?.properties?.gender ?? "-";
    public string ShahkarStatus => ContactModel?.properties?.shahkar_status ?? "0";
    public string EmailFormatted => ContactModel?.properties?.email ?? "-";
    public string IsVerifiedByCR => "????? ????"; // Property removed from HubSpot
    public string FatherName => ContactModel?.properties?.father_name ?? "-";
    public string BirthDate => ContactModel?.properties?.dateofbirth ?? "-";
    public string NationalCode => ContactModel?.properties?.natcode ?? "-";
    public string Phone => ContactModel?.properties?.phone ?? "-";

    public string GetShahkarStatusText(string? status)
    {
        if (string.IsNullOrEmpty(status)) status = "0";

        return status switch
        {
            "100" => "????? ????",
            "101" => "??? ?????",
            "500" => "??? ?? ?????",
            "0" => "??????? ????",
            "999" => "??????",
            _ => $"?? {status}"
        };
    }

    public string GetGenderText(string? gender)
    {
        if (string.IsNullOrEmpty(gender)) return "-";

        return gender.ToLower() switch
        {
            "???" => "??? ???",
            "??" => "????",
            "male" => "??? ???",
            "female" => "????",
            _ => gender
        };
    }

    protected override void OnError(Exception exception)
    {
        _logger.LogError(exception, "Error in UserHomeViewModel");
    }

    public async Task LoadDealLineItemsAsync(string dealId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Loading line items for deal: {DealId}", dealId);
            
            // Get associated line items for the deal
            var associations = await _associateService.ListAssoc(dealId, "deals", "line_items");

            if (associations?.results == null || !associations.results.Any())
            {
                _logger.LogInformation("No line items found for deal: {DealId}", dealId);
                SelectedDealLineItems = new List<Models.CRM.Commerce.LineItem.Read.Response>();
                return;
            }

            // Fetch line item details
            var lineItemsList = new List<Models.CRM.Commerce.LineItem.Read.Response>();

            foreach (var assoc in associations.results)
            {
                try
                {
                    var lineItem = await _lineItemService.GetLineItem(assoc.toObjectId.ToString());
                    if (lineItem != null)
                    {
                        lineItemsList.Add(lineItem);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load line item: {LineItemId}", assoc.toObjectId);
                }
            }

            SelectedDealLineItems = lineItemsList;
            _logger.LogInformation("Loaded {Count} line items for deal: {DealId}", lineItemsList.Count, dealId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading line items for deal: {DealId}", dealId);
            SelectedDealLineItems = new List<Models.CRM.Commerce.LineItem.Read.Response>();
        }
    }
}
