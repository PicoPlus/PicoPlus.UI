using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace PicoPlus.Services.CRM.Commerce
{
    public class LineItem
    {
        private readonly HttpClient _httpClient;
        private readonly string _hubspotToken;

        public LineItem(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;

            // Read from environment variable first, then configuration
            _hubspotToken = Environment.GetEnvironmentVariable("HUBSPOT_TOKEN")
                            ?? configuration["HubSpot:Token"]
                            ?? throw new InvalidOperationException("HubSpot token is not configured. Set HUBSPOT_TOKEN environment variable or HubSpot:Token in appsettings.");
        }

        public async Task<Models.CRM.Commerce.LineItem.Create.Response> CreateLineAsync(Models.CRM.Commerce.LineItem.Create.Request reqData)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.hubapi.com/crm/v3/objects/line_items/batch/create");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _hubspotToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var jsonBody = JsonSerializer.Serialize(reqData);
            request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            using var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Models.CRM.Commerce.LineItem.Create.Response>(
                responseContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );
        }

        public async Task<Models.CRM.Commerce.LineItem.Read.Response> GetLineItem(string id)
        {
            var url = $"https://api.hubapi.com/crm/v3/objects/line_items/{id}" +
                      "?properties=name&properties=price&properties=amount" +
                      "&properties=quantity&properties=hs_product_id" +
                      "&properties=hs_discount_percentage&archived=false";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _hubspotToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Models.CRM.Commerce.LineItem.Read.Response>(
                responseContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );
        }
    }
}
