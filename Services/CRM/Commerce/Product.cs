using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace PicoPlus.Services.CRM.Commerce
{
    public class Product
    {
        private readonly HttpClient _httpClient;
        private readonly string _hubspotToken;

        public Product(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;

            // Read from environment variable first, then configuration
            _hubspotToken = Environment.GetEnvironmentVariable("HUBSPOT_TOKEN")
                            ?? configuration["HubSpot:Token"]
                            ?? throw new InvalidOperationException("HubSpot token is not configured. Set HUBSPOT_TOKEN environment variable or HubSpot:Token in appsettings.");
        }

        public async Task<List<Models.CRM.Commerce.Products.Get.Response.Result>> ListAsync()
        {
            var allResults = new List<Models.CRM.Commerce.Products.Get.Response.Result>();
            string after = null;

            do
            {
                var url = $"https://api.hubapi.com/crm/v3/objects/products?limit=100&archived=false" +
                          $"&properties=name&properties=price&properties=hs_sku&properties=hs_product_type";

                if (!string.IsNullOrEmpty(after))
                {
                    url += $"&after={after}";
                }

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _hubspotToken);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                using var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();

                var page = JsonSerializer.Deserialize<Models.CRM.Commerce.Products.Get.Response>(
                    content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                if (page?.results != null)
                {
                    allResults.AddRange(page.results);
                }

                after = page?.paging?.next?.after;

            } while (!string.IsNullOrEmpty(after));

            return allResults;
        }
    }
}
