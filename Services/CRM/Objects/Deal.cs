using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

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

        public Deal(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;

            // Read from environment variable first, then configuration
            _hubSpotToken = Environment.GetEnvironmentVariable("HUBSPOT_TOKEN")
                            ?? configuration["HubSpot:Token"]
                            ?? throw new InvalidOperationException("HubSpot token is not configured. Set HUBSPOT_TOKEN environment variable or HubSpot:Token in appsettings.");
        }

        /// <summary>
        /// Create a new deal
        /// POST /crm/v3/objects/deals
        /// </summary>
        public async Task<Models.CRM.Objects.Deal.Create.Response> Create(
            Models.CRM.Objects.Deal.Create.Request dealInfo)
        {
            var httpClient = _httpClientFactory.CreateClient("HubSpot");
            var requestJson = JsonSerializer.Serialize(dealInfo);
            using var request = new HttpRequestMessage(HttpMethod.Post, BaseUrl)
            {
                Content = new StringContent(requestJson, Encoding.UTF8, "application/json")
            };

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _hubSpotToken);

            using var response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Models.CRM.Objects.Deal.Create.Response>(responseJson);
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
            var queryParams = new List<string> { "archived=false" };

            if (properties != null && properties.Length > 0)
            {
                foreach (var prop in properties)
                {
                    queryParams.Add($"properties={prop}");
                }
            }

            if (associations != null && associations.Length > 0)
            {
                foreach (var assoc in associations)
                {
                    queryParams.Add($"associations={assoc}");
                }
            }
            else
            {
                queryParams.Add("associations=contacts,line_items,notes");
            }

            var url = $"{BaseUrl}/{id}?{string.Join("&", queryParams)}";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _hubSpotToken);

            using var response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Models.CRM.Objects.Deal.Get.Response>(responseJson);
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

            var requestJson = JsonSerializer.Serialize(reqData);
            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(requestJson, Encoding.UTF8, "application/json")
            };

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _hubSpotToken);

            using var response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Models.CRM.Objects.Deal.GetBatch.Response>(responseJson);
        }

        /// <summary>
        /// Update a deal
        /// PATCH /crm/v3/objects/deals/{dealId}
        /// </summary>
        public async Task<Models.CRM.Objects.Deal.Create.Response> Update(string dealId, object updatedProperties)
        {
            var httpClient = _httpClientFactory.CreateClient("HubSpot");
            var url = $"{BaseUrl}/{dealId}";

            var json = JsonSerializer.Serialize(new { properties = updatedProperties });
            var request = new HttpRequestMessage(HttpMethod.Patch, url)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _hubSpotToken);

            using var response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Models.CRM.Objects.Deal.Create.Response>(responseJson);
        }

        /// <summary>
        /// Delete a deal (archive)
        /// DELETE /crm/v3/objects/deals/{dealId}
        /// </summary>
        public async Task<bool> Delete(string dealId)
        {
            var httpClient = _httpClientFactory.CreateClient("HubSpot");
            var url = $"{BaseUrl}/{dealId}";

            var request = new HttpRequestMessage(HttpMethod.Delete, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _hubSpotToken);

            using var response = await httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Search deals with filter criteria
        /// POST /crm/v3/objects/deals/search
        /// </summary>
        public async Task<Models.CRM.Objects.Deal.Search.Response> Search(object searchRequest)
        {
            var httpClient = _httpClientFactory.CreateClient("HubSpot");
            var url = $"{BaseUrl}/search";

            var json = JsonSerializer.Serialize(searchRequest);
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _hubSpotToken);

            using var response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Models.CRM.Objects.Deal.Search.Response>(responseJson);
        }

        /// <summary>
        /// Get all deals (paginated)
        /// GET /crm/v3/objects/deals
        /// </summary>
        public async Task<Models.CRM.Objects.Deal.GetAll.Response> GetAll(int limit = 100, string? after = null, string[]? properties = null)
        {
            var httpClient = _httpClientFactory.CreateClient("HubSpot");
            var queryParams = new List<string> { $"limit={limit}", "archived=false" };

            if (!string.IsNullOrEmpty(after))
            {
                queryParams.Add($"after={after}");
            }

            if (properties != null && properties.Length > 0)
            {
                foreach (var prop in properties)
                {
                    queryParams.Add($"properties={prop}");
                }
            }

            var url = $"{BaseUrl}?{string.Join("&", queryParams)}";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _hubSpotToken);

            using var response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Models.CRM.Objects.Deal.GetAll.Response>(responseJson);
        }

        /// <summary>
        /// Batch create deals
        /// POST /crm/v3/objects/deals/batch/create
        /// </summary>
        public async Task<Models.CRM.Objects.Deal.BatchMutation.Response> BatchCreate(List<Models.CRM.Objects.Deal.Create.Request> deals)
        {
            var httpClient = _httpClientFactory.CreateClient("HubSpot");
            var url = $"{BaseUrl}/batch/create";

            var payload = new { inputs = deals };
            var json = JsonSerializer.Serialize(payload);
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _hubSpotToken);

            using var response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Models.CRM.Objects.Deal.BatchMutation.Response>(responseJson);
        }

        /// <summary>
        /// Batch update deals
        /// POST /crm/v3/objects/deals/batch/update
        /// </summary>
        public async Task<Models.CRM.Objects.Deal.BatchMutation.Response> BatchUpdate(List<object> updates)
        {
            var httpClient = _httpClientFactory.CreateClient("HubSpot");
            var url = $"{BaseUrl}/batch/update";

            var payload = new { inputs = updates };
            var json = JsonSerializer.Serialize(payload);
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _hubSpotToken);

            using var response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Models.CRM.Objects.Deal.BatchMutation.Response>(responseJson);
        }
    }
}
