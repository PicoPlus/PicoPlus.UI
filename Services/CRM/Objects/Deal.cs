using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace PicoPlus.Services.CRM.Objects
{
    public partial class Deal
    {
        private readonly HttpClient _httpClient;
        private readonly string _hubSpotToken;

        public Deal(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _hubSpotToken = configuration["HubSpot:Token"];
        }

        public async Task<Models.CRM.Objects.Deal.Create.Response> Create(Models.CRM.Objects.Deal.Create.Request dealInfo)
        {
            var url = "https://api.hubapi.com/crm/v3/objects/deals";

            var requestJson = JsonSerializer.Serialize(dealInfo);
            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(requestJson, Encoding.UTF8, "application/json")
            };

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _hubSpotToken);

            using var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Models.CRM.Objects.Deal.Create.Response>(responseJson);
        }

        public async Task<Models.CRM.Objects.Deal.Get.Response> GetDeal(string id)
        {
            var url = $"https://api.hubapi.com/crm/v3/objects/deals/{id}?limit=10&archived=false&associations=contacts,line_items,notes";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _hubSpotToken);

            using var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Models.CRM.Objects.Deal.Get.Response>(responseJson);
        }

        public async Task<Models.CRM.Objects.Deal.GetBatch.Response> GetDeals(Models.CRM.Objects.Deal.GetBatch.Request reqData)
        {
            var url = "https://api.hubapi.com/crm/v3/objects/deals/batch/read?archived=false";

            var requestJson = JsonSerializer.Serialize(reqData);
            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(requestJson, Encoding.UTF8, "application/json")
            };

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _hubSpotToken);

            using var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Models.CRM.Objects.Deal.GetBatch.Response>(responseJson);
        }
    }
}
