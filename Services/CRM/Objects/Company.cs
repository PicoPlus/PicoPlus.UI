using System.Net.Http;
using PicoPlus.Services.Shared;

namespace PicoPlus.Services.CRM.Objects;

/// <summary>
/// HubSpot Companies API Service
/// https://developers.hubspot.com/docs/api/crm/companies
/// </summary>
public class Company
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _hubSpotToken;
    private const string BaseUrl = "/crm/v3/objects/companies";

    public Company(IHttpClientFactory httpClientFactory, HubSpotTokenProvider tokenProvider)
    {
        _httpClientFactory = httpClientFactory;
        _hubSpotToken = tokenProvider.Token;
    }

    /// <summary>
    /// Create a new company
    /// POST /crm/v3/objects/companies
    /// </summary>
    public async Task<dynamic> Create(object companyInfo)
    {
        var httpClient = _httpClientFactory.CreateClient("HubSpot");
        return await HubSpotRequestHelper.PostAsync<dynamic>(httpClient, BaseUrl, companyInfo, _hubSpotToken);
    }

    /// <summary>
    /// Get company by ID
    /// GET /crm/v3/objects/companies/{companyId}
    /// </summary>
    public async Task<dynamic> Read(string id, string[]? properties = null, string[]? associations = null)
    {
        var query = HubSpotQueryBuilder.BuildQueryString(properties, associations);
        var url = string.IsNullOrEmpty(query)
            ? $"{BaseUrl}/{id}"
            : $"{BaseUrl}/{id}?{query}";

        var httpClient = _httpClientFactory.CreateClient("HubSpot");
        return await HubSpotRequestHelper.GetAsync<dynamic>(httpClient, url, _hubSpotToken);
    }

    /// <summary>
    /// Update a company
    /// PATCH /crm/v3/objects/companies/{companyId}
    /// </summary>
    public async Task<dynamic> Update(string companyId, object updatedProperties)
    {
        var url = $"{BaseUrl}/{companyId}";
        var httpClient = _httpClientFactory.CreateClient("HubSpot");
        return await HubSpotRequestHelper.PatchAsync<dynamic>(httpClient, url, new { properties = updatedProperties }, _hubSpotToken);
    }

    /// <summary>
    /// Delete a company (archive)
    /// DELETE /crm/v3/objects/companies/{companyId}
    /// </summary>
    public async Task<bool> Delete(string companyId)
    {
        var url = $"{BaseUrl}/{companyId}";
        var httpClient = _httpClientFactory.CreateClient("HubSpot");
        return await HubSpotRequestHelper.DeleteAsync(httpClient, url, _hubSpotToken);
    }

    /// <summary>
    /// Search companies
    /// POST /crm/v3/objects/companies/search
    /// </summary>
    public async Task<dynamic> Search(object searchRequest)
    {
        var url = $"{BaseUrl}/search";
        var httpClient = _httpClientFactory.CreateClient("HubSpot");
        return await HubSpotRequestHelper.PostAsync<dynamic>(httpClient, url, searchRequest, _hubSpotToken);
    }

    /// <summary>
    /// Get all companies (paginated)
    /// GET /crm/v3/objects/companies
    /// </summary>
    public async Task<dynamic> GetAll(int limit = 100, string? after = null, string[]? properties = null)
    {
        var query = HubSpotQueryBuilder.BuildPaginationQuery(limit, after, properties);
        var url = $"{BaseUrl}?{query}";
        var httpClient = _httpClientFactory.CreateClient("HubSpot");
        return await HubSpotRequestHelper.GetAsync<dynamic>(httpClient, url, _hubSpotToken);
    }

    /// <summary>
    /// Batch operations
    /// </summary>
    public async Task<dynamic> BatchCreate(List<object> companies)
    {
        var url = $"{BaseUrl}/batch/create";
        var httpClient = _httpClientFactory.CreateClient("HubSpot");
        return await HubSpotRequestHelper.PostAsync<dynamic>(httpClient, url, new { inputs = companies }, _hubSpotToken);
    }

    public async Task<dynamic> BatchUpdate(List<object> updates)
    {
        var url = $"{BaseUrl}/batch/update";
        var httpClient = _httpClientFactory.CreateClient("HubSpot");
        return await HubSpotRequestHelper.PostAsync<dynamic>(httpClient, url, new { inputs = updates }, _hubSpotToken);
    }
}
