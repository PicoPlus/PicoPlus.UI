#nullable enable

using Microsoft.Extensions.Caching.Memory;
using PicoPlus.Infrastructure.Services;
using PicoPlus.Infrastructure.State;
using PicoPlus.Services.CRM;
using PicoPlus.Services.CRM.Objects;
using PicoPlus.State.UserPanel;
using PicoPlus.Extensions;
using PicoPlus.Services.Backup;
using ContactModel = PicoPlus.Models.CRM.Objects.Contact;
using DealModel = PicoPlus.Models.CRM.Objects.Deal;

namespace PicoPlus.Services.UserPanel;

/// <summary>
/// Implementation of user panel service
/// Handles business logic for user panel data operations
/// </summary>
public class UserPanelService : IUserPanelService
{
    private readonly Contact _contactService;
    private readonly Deal _dealService;
    private readonly Associate _associateService;
    private readonly ISessionStorageService _sessionStorage;
    private readonly AuthenticationStateService _authState;
    private readonly IMemoryCache _cache;
    private readonly ILogger<UserPanelService> _logger;
    private readonly IGraphBackupService _graphBackupService;

    private const string CacheKeyPrefix = "UserPanel_";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public UserPanelService(
        Contact contactService,
        Deal dealService,
        Associate associateService,
        ISessionStorageService sessionStorage,
        AuthenticationStateService authState,
        IMemoryCache cache,
        ILogger<UserPanelService> logger,
        IGraphBackupService graphBackupService)
    {
        _contactService = contactService;
        _dealService = dealService;
        _associateService = associateService;
        _sessionStorage = sessionStorage;
        _authState = authState;
        _cache = cache;
        _logger = logger;
        _graphBackupService = graphBackupService;
    }

    public async Task<UserPanelState?> LoadUserPanelStateAsync(string contactId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check cache first
            var cacheKey = $"{CacheKeyPrefix}{contactId}";
            if (_cache.TryGetValue<UserPanelState>(cacheKey, out var cachedState))
            {
                _logger.LogDebug("Returning cached user panel state for contact: {ContactId}", contactId);
                return cachedState;
            }

            _logger.LogInformation("Loading user panel state for contact: {ContactId}", contactId);

            // Load contact from session or CRM
            var contactModel = await _sessionStorage.GetItemAsync<ContactModel.Search.Response.Result>("ContactModel", cancellationToken);

            if (contactModel == null || contactModel.id != contactId)
            {
                _logger.LogWarning("Contact not found in session, fetching from CRM: {ContactId}", contactId);
                var contactResponse = await _contactService.Read(contactId);
                if (contactResponse == null)
                {
                    _logger.LogError("Contact not found in CRM: {ContactId}", contactId);
                    return null;
                }

                // Use the response directly - it's already the correct type
                contactModel = new ContactModel.Search.Response.Result
                {
                    id = contactResponse.id,
                    properties = new ContactModel.Search.Response.Result.Properties
                    {
                        email = contactResponse.properties.email,
                        firstname = contactResponse.properties.firstname,
                        lastname = contactResponse.properties.lastname,
                        phone = contactResponse.properties.phone,
                        natcode = contactResponse.properties.natcode,
                        dateofbirth = contactResponse.properties.dateofbirth,
                        father_name = contactResponse.properties.father_name,
                        gender = contactResponse.properties.gender,
                        total_revenue = contactResponse.properties.total_revenue,
                        shahkar_status = contactResponse.properties.shahkar_status,
                        wallet = contactResponse.properties.wallet,
                        num_associated_deals = contactResponse.properties.num_associated_deals,
                        contact_plan = contactResponse.properties.contact_plan
                    },
                    createdAt = contactResponse.createdAt.ToString(),
                    updatedAt = contactResponse.updatedAt.ToString(),
                    archived = contactResponse.archived
                };
            }

            // Map contact info
            var contactInfo = MapContactInfo(contactModel);

            // Load associated deals
            var deals = await LoadUserDealsAsync(contactId, cancellationToken);

            // Calculate statistics
            var statistics = CalculateStatistics(deals, contactInfo);

            var state = new UserPanelState
            {
                Contact = contactInfo,
                Statistics = statistics,
                Deals = deals
            };

            // Update authentication state
            _authState.SetAuthenticatedUser(contactModel);

            // Cache the result
            _cache.Set(cacheKey, state, CacheDuration);

            // Best-effort backup for resilience/reporting; failures are handled inside the service.
            await _graphBackupService.BackupUserPanelStateAsync(contactId, state, cancellationToken);

            _logger.LogInformation("Successfully loaded user panel state for: {ContactId}, Deals: {DealCount}", contactId, deals.Count);

            return state;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading user panel state for contact: {ContactId}", contactId);
            return null;
        }
    }

    public async Task<string?> GetCurrentUserIdAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var contactModel = await _sessionStorage.GetItemAsync<ContactModel.Search.Response.Result>("ContactModel", cancellationToken);
            return contactModel?.id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user ID from session");
            return null;
        }
    }

    public async Task ClearSessionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _sessionStorage.RemoveItemAsync("LogInState", cancellationToken);
            await _sessionStorage.RemoveItemAsync("ContactModel", cancellationToken);
            await _sessionStorage.RemoveItemAsync("UserRole", cancellationToken);
            _authState.ClearAuthentication();
            _logger.LogInformation("User session cleared successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing user session");
            throw;
        }
    }

    private async Task<IReadOnlyList<DealSummary>> LoadUserDealsAsync(string contactId, CancellationToken cancellationToken)
    {
        try
        {
            // Get associated deals
            var associations = await _associateService.ListAssoc(contactId, "contact", "deals");

            if (associations?.results == null || !associations.results.Any())
            {
                _logger.LogInformation("No deals found for contact: {ContactId}", contactId);
                return Array.Empty<DealSummary>();
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

            if (dealsResponse?.results == null)
            {
                return Array.Empty<DealSummary>();
            }

            // Map to DealSummary DTOs
            var dealSummaries = dealsResponse.results
                .Select(MapDealSummary)
                .OrderByDescending(d => d.CreatedAt)
                .ToList();

            return dealSummaries;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading deals for contact: {ContactId}", contactId);
            return Array.Empty<DealSummary>();
        }
    }

    private static ContactInfo MapContactInfo(ContactModel.Search.Response.Result contact)
    {
        var props = contact.properties;

        return new ContactInfo
        {
            Id = contact.id,
            FirstName = props?.firstname ?? "?????",
            LastName = props?.lastname ?? "??????",
            NationalCode = props?.natcode,
            Phone = props?.phone,
            Email = props?.email,
            DateOfBirth = props?.dateofbirth,
            FatherName = props?.father_name,
            Wallet = decimal.TryParse(props?.wallet, out var wallet) ? wallet : 0m
        };
    }

    private static DealSummary MapDealSummary(DealModel.GetBatch.Response.Result deal)
    {
        return new DealSummary
        {
            Id = deal.id,
            DealName = deal.properties?.dealname ?? "???? ?????",
            Amount = decimal.TryParse(deal.properties?.amount, out var amount) ? amount : 0m,
            Stage = deal.properties?.dealstage.ParseDealStage() ?? DealStage.Unknown,
            CreatedAt = deal.createdAt,
            UpdatedAt = deal.updatedAt,
            CloseDate = DateTime.TryParse(deal.properties?.createdate, out var closeDate) ? closeDate : null,
            Pipeline = deal.properties?.hs_object_id
        };
    }

    private static UserStatistics CalculateStatistics(IReadOnlyList<DealSummary> deals, ContactInfo contact)
    {
        var totalDeals = deals.Count;
        var closedDeals = deals.Count(d => d.Stage == DealStage.ClosedWon || d.Stage == DealStage.ClosedLost);
        var openDeals = totalDeals - closedDeals;
        var totalRevenue = deals
            .Where(d => d.Stage == DealStage.ClosedWon)
            .Sum(d => d.Amount);

        return new UserStatistics
        {
            TotalDeals = totalDeals,
            ClosedDeals = closedDeals,
            OpenDeals = openDeals,
            TotalRevenue = totalRevenue
        };
    }
}
