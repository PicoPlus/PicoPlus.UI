using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace PicoPlus.Services.CRM
{
    public class Pipelines
    {
        private readonly HttpClient _httpClient;
        private readonly string _hubspotToken;

        public Pipelines(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _hubspotToken = configuration["Hubspot:Token"];
        }

        public async Task<Models.CRM.Pipelines.List> GetPipelines(string objectType)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.hubapi.com/crm/v3/pipelines/{objectType}");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _hubspotToken);

            using var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Models.CRM.Pipelines.List>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        public async Task<Models.CRM.Pipelines.GetPipelineByStageID> GetStagesByPipID(string objectName, string stageID)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.hubapi.com/crm/v3/pipelines/{objectName}/default/stages/{stageID}");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _hubspotToken);

            using var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Models.CRM.Pipelines.GetPipelineByStageID>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        public async Task<Models.CRM.Pipelines.GetStages> GetDealStages()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.hubapi.com/crm/v3/pipelines/deals/default/stages");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _hubspotToken);

            using var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Models.CRM.Pipelines.GetStages>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
    }
}
