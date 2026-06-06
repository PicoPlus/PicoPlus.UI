using System.Net.Http;
using PicoPlus.Services.Shared;

namespace PicoPlus.Services.CRM.Objects;

/// <summary>
/// HubSpot Tickets API Service
/// https://developers.hubspot.com/docs/api/crm/tickets
/// </summary>
public class Ticket
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _hubSpotToken;
    private const string BaseUrl = "/crm/v3/objects/tickets";

    public Ticket(IHttpClientFactory httpClientFactory, HubSpotTokenProvider tokenProvider)
    {
        _httpClientFactory = httpClientFactory;
        _hubSpotToken = tokenProvider.Token;
    }

    public async Task<dynamic> Create(object ticketInfo)
    {
        var httpClient = _httpClientFactory.CreateClient("HubSpot");
        return await HubSpotRequestHelper.PostAsync<dynamic>(httpClient, BaseUrl, ticketInfo, _hubSpotToken);
    }

    public async Task<dynamic> Read(string id, string[]? properties = null, string[]? associations = null)
    {
        var query = HubSpotQueryBuilder.BuildQueryString(properties, associations);
        var url = string.IsNullOrEmpty(query)
            ? $"{BaseUrl}/{id}"
            : $"{BaseUrl}/{id}?{query}";

        var httpClient = _httpClientFactory.CreateClient("HubSpot");
        return await HubSpotRequestHelper.GetAsync<dynamic>(httpClient, url, _hubSpotToken);
    }

    public async Task<dynamic> Update(string ticketId, object updatedProperties)
    {
        var url = $"{BaseUrl}/{ticketId}";
        var httpClient = _httpClientFactory.CreateClient("HubSpot");
        return await HubSpotRequestHelper.PatchAsync<dynamic>(httpClient, url, new { properties = updatedProperties }, _hubSpotToken);
    }

    public async Task<bool> Delete(string ticketId)
    {
        var url = $"{BaseUrl}/{ticketId}";
        var httpClient = _httpClientFactory.CreateClient("HubSpot");
        return await HubSpotRequestHelper.DeleteAsync(httpClient, url, _hubSpotToken);
    }

    public async Task<dynamic> Search(object searchRequest)
    {
        var url = $"{BaseUrl}/search";
        var httpClient = _httpClientFactory.CreateClient("HubSpot");
        return await HubSpotRequestHelper.PostAsync<dynamic>(httpClient, url, searchRequest, _hubSpotToken);
    }

    public async Task<dynamic> GetAll(int limit = 100, string? after = null, string[]? properties = null)
    {
        var query = HubSpotQueryBuilder.BuildPaginationQuery(limit, after, properties);
        var url = $"{BaseUrl}?{query}";
        var httpClient = _httpClientFactory.CreateClient("HubSpot");
        return await HubSpotRequestHelper.GetAsync<dynamic>(httpClient, url, _hubSpotToken);
    }

    public async Task<dynamic> BatchCreate(List<object> tickets)
    {
        var url = $"{BaseUrl}/batch/create";
        var httpClient = _httpClientFactory.CreateClient("HubSpot");
        return await HubSpotRequestHelper.PostAsync<dynamic>(httpClient, url, new { inputs = tickets }, _hubSpotToken);
    }

    public async Task<dynamic> BatchUpdate(List<object> updates)
    {
        var url = $"{BaseUrl}/batch/update";
        var httpClient = _httpClientFactory.CreateClient("HubSpot");
        return await HubSpotRequestHelper.PostAsync<dynamic>(httpClient, url, new { inputs = updates }, _hubSpotToken);
    }
}
