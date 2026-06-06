using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using PicoPlus.Services.Shared;

namespace PicoPlus.Services.CRM.Objects
{
    /// <summary>
    /// HubSpot Deals API Service
    /// https://developers.hubspot.com/docs/api/crm/deals
    /// </summary>
    public partial class Deal
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _hubSpotToken;
        private const string BaseUrl = "/crm/v3/objects/deals";

        public Deal(IHttpClientFactory httpClientFactory, HubSpotTokenProvider tokenProvider)
        {
            _httpClientFactory = httpClientFactory;
            _hubSpotToken = tokenProvider.Token;
        }

        /// <summary>
        /// Create a new deal
        /// POST /crm/v3/objects/deals
        /// </summary>
        public async Task<Models.CRM.Objects.Deal.Create.Response> Create(
            Models.CRM.Objects.Deal.Create.Request dealInfo)
        {
            var httpClient = _httpClientFactory.CreateClient("HubSpot");
            return await HubSpotRequestHelper.PostAsync<Models.CRM.Objects.Deal.Create.Response>(
                httpClient, BaseUrl, dealInfo, _hubSpotToken);
        }

        /// <summary>
        /// Get deal by ID with associations
        /// GET /crm/v3/objects/deals/{dealId}
        /// </summary>
        public async Task<Models.CRM.Objects.Deal.Get.Response> GetDeal(
            string id,
            string[]? properties = null,
            string[]? associations = null)
        {
            var httpClient = _httpClientFactory.CreateClient("HubSpot");
            var query = HubSpotQueryBuilder.BuildQueryString(
                properties,
                associations,
                defaultAssociations: new[] { "contacts", "line_items", "notes" },
                extraParams: new Dictionary<string, string> { { "archived", "false" } });
            var url = $"{BaseUrl}/{id}?{query}";

            return await HubSpotRequestHelper.GetAsync<Models.CRM.Objects.Deal.Get.Response>(
                httpClient, url, _hubSpotToken);
        }

        /// <summary>
        /// Get multiple deals by batch read
        /// POST /crm/v3/objects/deals/batch/read
        /// </summary>
        public async Task<Models.CRM.Objects.Deal.GetBatch.Response> GetDeals(
            Models.CRM.Objects.Deal.GetBatch.Request reqData)
        {
            var httpClient = _httpClientFactory.CreateClient("HubSpot");
            var url = $"{BaseUrl}/batch/read?archived=false";
            return await HubSpotRequestHelper.PostAsync<Models.CRM.Objects.Deal.GetBatch.Response>(
                httpClient, url, reqData, _hubSpotToken);
        }

        /// <summary>
        /// Update a deal
        /// PATCH /crm/v3/objects/deals/{dealId}
        /// </summary>
        public async Task<Models.CRM.Objects.Deal.Create.Response> Update(string dealId, object updatedProperties)
        {
            var httpClient = _httpClientFactory.CreateClient("HubSpot");
            var url = $"{BaseUrl}/{dealId}";
            return await HubSpotRequestHelper.PatchAsync<Models.CRM.Objects.Deal.Create.Response>(
                httpClient, url, new { properties = updatedProperties }, _hubSpotToken);
        }

        /// <summary>
        /// Delete a deal (archive)
        /// DELETE /crm/v3/objects/deals/{dealId}
        /// </summary>
        public async Task<bool> Delete(string dealId)
        {
            var httpClient = _httpClientFactory.CreateClient("HubSpot");
            var url = $"{BaseUrl}/{dealId}";
            return await HubSpotRequestHelper.DeleteAsync(httpClient, url, _hubSpotToken);
        }

        /// <summary>
        /// Search deals with filter criteria
        /// POST /crm/v3/objects/deals/search
        /// </summary>
        public async Task<dynamic> Search(object searchRequest)
        {
            var httpClient = _httpClientFactory.CreateClient("HubSpot");
            var url = $"{BaseUrl}/search";
            return await HubSpotRequestHelper.PostAsync<dynamic>(httpClient, url, searchRequest, _hubSpotToken);
        }

        /// <summary>
        /// Get all deals (paginated)
        /// GET /crm/v3/objects/deals
        /// </summary>
        public async Task<dynamic> GetAll(int limit = 100, string? after = null, string[]? properties = null)
        {
            var httpClient = _httpClientFactory.CreateClient("HubSpot");
            var query = HubSpotQueryBuilder.BuildPaginationQuery(limit, after, properties);
            var url = $"{BaseUrl}?{query}&archived=false";
            return await HubSpotRequestHelper.GetAsync<dynamic>(httpClient, url, _hubSpotToken);
        }

        /// <summary>
        /// Batch create deals
        /// POST /crm/v3/objects/deals/batch/create
        /// </summary>
        public async Task<dynamic> BatchCreate(List<Models.CRM.Objects.Deal.Create.Request> deals)
        {
            var httpClient = _httpClientFactory.CreateClient("HubSpot");
            var url = $"{BaseUrl}/batch/create";
            return await HubSpotRequestHelper.PostAsync<dynamic>(httpClient, url, new { inputs = deals }, _hubSpotToken);
        }

        /// <summary>
        /// Batch update deals
        /// POST /crm/v3/objects/deals/batch/update
        /// </summary>
        public async Task<dynamic> BatchUpdate(List<object> updates)
        {
            var httpClient = _httpClientFactory.CreateClient("HubSpot");
            var url = $"{BaseUrl}/batch/update";
            return await HubSpotRequestHelper.PostAsync<dynamic>(httpClient, url, new { inputs = updates }, _hubSpotToken);
        }
    }
}
