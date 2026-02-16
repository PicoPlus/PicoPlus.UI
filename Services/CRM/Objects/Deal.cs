using System.Net.Http;
using PicoPlus.Services.CRM;

namespace PicoPlus.Services.CRM.Objects
{
    /// <summary>
    /// HubSpot Deals API Service
    /// https://developers.hubspot.com/docs/api/crm/deals
    /// </summary>
    public partial class Deal
    {
        private readonly HubSpotApiClient _apiClient;
        private const string BaseUrl = "/crm/v3/objects/deals";

        public Deal(HubSpotApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<Models.CRM.Objects.Deal.Create.Response> Create(Models.CRM.Objects.Deal.Create.Request dealInfo)
            => (await _apiClient.SendAsync<Models.CRM.Objects.Deal.Create.Response>(HttpMethod.Post, BaseUrl, dealInfo))!;

        public async Task<Models.CRM.Objects.Deal.Get.Response> GetDeal(string id, string[]? properties = null, string[]? associations = null)
        {
            var queryParams = new List<string> { "archived=false" };
            if (properties is { Length: > 0 })
            {
                queryParams.AddRange(properties.Select(prop => $"properties={prop}"));
            }

            if (associations is { Length: > 0 })
            {
                queryParams.AddRange(associations.Select(assoc => $"associations={assoc}"));
            }
            else
            {
                queryParams.Add("associations=contacts,line_items,notes");
            }

            var url = $"{BaseUrl}/{id}?{string.Join("&", queryParams)}";
            return (await _apiClient.SendAsync<Models.CRM.Objects.Deal.Get.Response>(HttpMethod.Get, url))!;
        }

        public async Task<Models.CRM.Objects.Deal.GetBatch.Response> GetDeals(Models.CRM.Objects.Deal.GetBatch.Request reqData)
            => (await _apiClient.SendAsync<Models.CRM.Objects.Deal.GetBatch.Response>(HttpMethod.Post, $"{BaseUrl}/batch/read?archived=false", reqData))!;

        public async Task<Models.CRM.Objects.Deal.Create.Response> Update(string dealId, object updatedProperties)
            => (await _apiClient.SendAsync<Models.CRM.Objects.Deal.Create.Response>(HttpMethod.Patch, $"{BaseUrl}/{dealId}", new { properties = updatedProperties }))!;

        public Task<bool> Delete(string dealId)
            => _apiClient.SendForStatusAsync(HttpMethod.Delete, $"{BaseUrl}/{dealId}");

        public async Task<Models.CRM.Objects.Deal.Search.Response> Search(object searchRequest)
            => (await _apiClient.SendAsync<Models.CRM.Objects.Deal.Search.Response>(HttpMethod.Post, $"{BaseUrl}/search", searchRequest))!;

        public async Task<Models.CRM.Objects.Deal.GetAll.Response> GetAll(int limit = 100, string? after = null, string[]? properties = null)
        {
            var queryParams = new List<string> { $"limit={limit}", "archived=false" };

            if (!string.IsNullOrEmpty(after))
            {
                queryParams.Add($"after={after}");
            }

            if (properties is { Length: > 0 })
            {
                queryParams.AddRange(properties.Select(prop => $"properties={prop}"));
            }

            var url = $"{BaseUrl}?{string.Join("&", queryParams)}";
            return (await _apiClient.SendAsync<Models.CRM.Objects.Deal.GetAll.Response>(HttpMethod.Get, url))!;
        }

        public async Task<Models.CRM.Objects.Deal.BatchMutation.Response> BatchCreate(List<Models.CRM.Objects.Deal.Create.Request> deals)
            => (await _apiClient.SendAsync<Models.CRM.Objects.Deal.BatchMutation.Response>(HttpMethod.Post, $"{BaseUrl}/batch/create", new { inputs = deals }))!;

        public async Task<Models.CRM.Objects.Deal.BatchMutation.Response> BatchUpdate(List<object> updates)
            => (await _apiClient.SendAsync<Models.CRM.Objects.Deal.BatchMutation.Response>(HttpMethod.Post, $"{BaseUrl}/batch/update", new { inputs = updates }))!;
    }
}
