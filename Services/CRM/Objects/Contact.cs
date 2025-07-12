using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace PicoPlus.Services.CRM.Objects
{
    public class Contact
    {
        private readonly HttpClient _httpClient;
        private readonly string _hubSpotToken;

        public Contact(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _hubSpotToken = configuration["HubSpot:Token"];
        }

        public async Task<Models.CRM.Objects.Contact.Search.Response> Search(string query, string paramName, string paramValue, string[] propertiesToInclude)
        {
            var url = "https://api.hubapi.com/crm/v3/objects/contacts/search";

            var payload = new
            {
                query = query,
                limit = 100,
                sorts = new object[] { },
                properties = propertiesToInclude,
                filterGroups = new[]
                {
                    new
                    {
                        filters = new[]
                        {
                            new
                            {
                                propertyName = paramName,
                                value = paramValue,
                                @operator = "EQ"
                            }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(payload);
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _hubSpotToken);

            using var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Models.CRM.Objects.Contact.Search.Response>(responseJson);
        }

        public async Task<Models.CRM.Objects.Contact.Create.Response> Create(Models.CRM.Objects.Contact.Create.Request contactInfo)
        {
            var url = "https://api.hubapi.com/crm/v3/objects/contacts";

            var json = JsonSerializer.Serialize(contactInfo);
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _hubSpotToken);

            using var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Models.CRM.Objects.Contact.Create.Response>(responseJson);
        }

        public async Task<Models.CRM.Objects.Contact.Read.Response> Read(string id)
        {
            var url = $"https://api.hubapi.com/crm/v3/objects/contacts/{id}?associations=deals" +
                      "&properties=isverifiedbycr" +
                      "&properties=father_name" +
                      "&properties=dateofbirth" +
                      "&properties=natcode" +
                      "&properties=total_revenue" +
                      "&properties=firstname" +
                      "&properties=lastname" +
                      "&properties=phone";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _hubSpotToken);

            using var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Models.CRM.Objects.Contact.Read.Response>(responseJson);
        }

        public async Task<Models.CRM.Objects.Contact.Search.Response.Result.Properties> Update(string contactId, object updatedProperties)
        {
            var url = $"https://api.hubapi.com/crm/v3/objects/contacts/{contactId}";

            var json = JsonSerializer.Serialize(new { properties = updatedProperties });
            var request = new HttpRequestMessage(HttpMethod.Patch, url)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _hubSpotToken);

            using var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Models.CRM.Objects.Contact.Search.Response.Result.Properties>(responseJson);
        }
    }
}
