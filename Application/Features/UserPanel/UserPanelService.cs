#nullable enable

using Microsoft.Extensions.Caching.Memory;
using NovinCRM.Domain.Aggregates;
using NovinCRM.Domain.Enums;
using NovinCRM.Domain.Extensions;
using NovinCRM.Domain.ValueObjects;
using NovinCRM.Infrastructure.Services;
using NovinCRM.Infrastructure.State;
using NovinCRM.Services.CRM;
using ContactSvc   = NovinCRM.Services.CRM.Objects.Contact;
using DealSvc      = NovinCRM.Services.CRM.Objects.Deal;
using ContactModel = NovinCRM.Models.CRM.Objects.Contact;
using DealModel    = NovinCRM.Models.CRM.Objects.Deal;
using DomainContact = NovinCRM.Domain.Entities.Contact;
using DomainDeal    = NovinCRM.Domain.Entities.Deal;

namespace NovinCRM.Services.UserPanel;

/// <summary>
/// Implements IUserPanelService.
/// Loads and aggregates user panel data from CRM, returning Domain types.
/// </summary>
public class UserPanelService : IUserPanelService
{
    private readonly ContactSvc _contactService;
    private readonly DealSvc    _dealService;
    private readonly Associate  _associateService;
    private readonly ISessionStorageService _sessionStorage;
    private readonly AuthenticationStateService _authState;
    private readonly IMemoryCache _cache;
    private readonly ILogger<UserPanelService> _logger;

    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public UserPanelService(
        ContactSvc contactService,
        DealSvc    dealService,
        Associate  associateService,
        ISessionStorageService sessionStorage,
        AuthenticationStateService authState,
        IMemoryCache cache,
        ILogger<UserPanelService> logger)
    {
        _contactService = contactService;
        _dealService    = dealService;
        _associateService = associateService;
        _sessionStorage = sessionStorage;
        _authState      = authState;
        _cache          = cache;
        _logger         = logger;
    }

    public async Task<UserPanelState?> LoadUserPanelStateAsync(
        string contactId, CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = NovinCRM.Application.Common.CacheKeys.UserPanel(contactId);
            if (_cache.TryGetValue<UserPanelState>(cacheKey, out var cached))
            {
                _logger.LogDebug("Returning cached panel state for: {ContactId}", contactId);
                return cached;
            }

            _logger.LogInformation("Loading panel state for: {ContactId}", contactId);

            // Load contact from session or CRM
            var contactDto = await _sessionStorage
                .GetItemAsync<ContactModel.Search.Response.Result>("ContactModel", cancellationToken);

            if (contactDto == null || contactDto.id != contactId)
            {
                _logger.LogWarning("Contact not in session, fetching from CRM: {ContactId}", contactId);
                var raw = await _contactService.Read(contactId);
                if (raw == null) return null;

                contactDto = new ContactModel.Search.Response.Result
                {
                    id = raw.id,
                    properties = new ContactModel.Search.Response.Result.Properties
                    {
                        firstname            = raw.properties.firstname,
                        lastname             = raw.properties.lastname,
                        email                = raw.properties.email,
                        phone                = raw.properties.phone,
                        ncode                = raw.properties.ncode,
                        dateofbirth          = raw.properties.dateofbirth,
                        father_name          = raw.properties.father_name,
                        gender               = raw.properties.gender,
                        total_revenue        = raw.properties.total_revenue,
                        shahkar_status       = raw.properties.shahkar_status,
                        wallet               = raw.properties.wallet,
                        num_associated_deals = raw.properties.num_associated_deals,
                        contact_plan         = raw.properties.contact_plan
                    },
                    createdAt = raw.createdAt.ToString(),
                    updatedAt = raw.updatedAt.ToString(),
                    archived  = raw.archived
                };
            }

            // Map to Domain entity
            var contact = MapContact(contactDto);

            // Load deals
            var deals = await LoadDealsAsync(contactId, cancellationToken);

            // Calculate statistics
            var statistics = CalculateStatistics(deals, contact);

            var state = new UserPanelState
            {
                Contact    = contact,
                Statistics = statistics,
                Deals      = deals
            };

            _authState.SetAuthenticatedUser(contactDto);
            _cache.Set(cacheKey, state, CacheDuration);

            _logger.LogInformation("Panel state loaded for: {ContactId}, Deals: {Count}", contactId, deals.Count);
            return state;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading panel state for: {ContactId}", contactId);
            return null;
        }
    }

    public async Task<string?> GetCurrentUserIdAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var model = await _sessionStorage
                .GetItemAsync<ContactModel.Search.Response.Result>("ContactModel", cancellationToken);
            return model?.id;
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
            await _sessionStorage.RemoveItemAsync("LogInState",   cancellationToken);
            await _sessionStorage.RemoveItemAsync("ContactModel", cancellationToken);
            await _sessionStorage.RemoveItemAsync("UserRole",     cancellationToken);
            _authState.ClearAuthentication();
            _logger.LogInformation("Session cleared");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing session");
            throw;
        }
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task<IReadOnlyList<DomainDeal>> LoadDealsAsync(
        string contactId, CancellationToken ct)
    {
        try
        {
            var assoc = await _associateService.ListAssoc(contactId, "contact", "deals");
            if (assoc?.results == null || !assoc.results.Any())
                return Array.Empty<DomainDeal>();

            var dealIds = assoc.results.Select(r => r.toObjectId.ToString()).ToList();
            var req = new DealModel.GetBatch.Request
            {
                inputs = dealIds.Select(id => new DealModel.GetBatch.Request.Input { id = id }).ToList(),
                properties = ["dealname", "amount", "dealstage", "createdate",
                              "hs_lastmodifieddate", "closedate", "pipeline"]
            };

            var resp = await _dealService.GetDeals(req);
            if (resp?.results == null) return Array.Empty<DomainDeal>();

            return resp.results
                .Select(MapDeal)
                .OrderByDescending(d => d.CreatedAt)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading deals for: {ContactId}", contactId);
            return Array.Empty<DomainDeal>();
        }
    }

    private static DomainContact MapContact(ContactModel.Search.Response.Result dto)
    {
        var p = dto.properties;
        return new DomainContact
        {
            Id                 = dto.id,
            FirstName          = p?.firstname  ?? "کاربر",
            LastName           = p?.lastname   ?? "مهمان",
            NationalCode       = p?.ncode,
            Phone              = p?.phone,
            Email              = p?.email,
            DateOfBirth        = p?.dateofbirth,
            FatherName         = p?.father_name,
            ShahkarStatus      = p?.shahkar_status,
            Wallet             = decimal.TryParse(p?.wallet,        out var w)  ? w  : null,
            TotalRevenue       = decimal.TryParse(p?.total_revenue, out var tr) ? tr : null,
            NumAssociatedDeals = int.TryParse(p?.num_associated_deals, out var n) ? n : null,
            ContactPlan        = p?.contact_plan
        };
    }

    private static DomainDeal MapDeal(DealModel.GetBatch.Response.Result dto)
        => new()
        {
            Id        = dto.id,
            DealName  = dto.properties?.dealname ?? "بدون نام",
            Amount    = decimal.TryParse(dto.properties?.amount, out var a) ? a : 0m,
            Stage     = (dto.properties?.dealstage ?? string.Empty).ParseDealStage(),
            CreatedAt = dto.createdAt,
            UpdatedAt = dto.updatedAt
            // GetBatch.Response.Properties does not expose closedate or pipeline
        };

    private static UserStatistics CalculateStatistics(
        IReadOnlyList<DomainDeal> deals, DomainContact contact)
    {
        var closed = deals.Count(d => d.Stage == DealStage.ClosedWon || d.Stage == DealStage.ClosedLost);
        return new UserStatistics
        {
            TotalDeals   = deals.Count,
            ClosedDeals  = closed,
            OpenDeals    = deals.Count - closed,
            TotalRevenue = deals.Where(d => d.Stage == DealStage.ClosedWon).Sum(d => d.Amount)
        };
    }
}
