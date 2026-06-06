using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using PicoPlus.Services.Shared;

namespace PicoPlus.Services.CRM.Commerce
{
    public class LineItem
    {
        private readonly HttpClient _httpClient;
        private readonly string _hubspotToken;

        public LineItem(HttpClient httpClient, HubSpotTokenProvider tokenProvider)
        {
            _httpClient = httpClient;
            _hubspotToken = tokenProvider.Token;
        }

        public async Task<Models.CRM.Commerce.LineItem.Create.Response> CreateLineAsync(Models.CRM.Commerce.LineItem.Create.Request reqData)
        {
            var url = "https://api.hubapi.com/crm/v3/objects/line_items/batch/create";
            return await HubSpotRequestHelper.PostAsync<Models.CRM.Commerce.LineItem.Create.Response>(
                _httpClient, url, reqData, _hubspotToken);
        }

        public async Task<Models.CRM.Commerce.LineItem.Read.Response> GetLineItem(string id)
        {
            var url = $"https://api.hubapi.com/crm/v3/objects/line_items/{id}" +
                      "?properties=name&properties=price&properties=amount" +
                      "&properties=quantity&properties=hs_product_id" +
                      "&properties=hs_discount_percentage&archived=false";

            return await HubSpotRequestHelper.GetAsync<Models.CRM.Commerce.LineItem.Read.Response>(
                _httpClient, url, _hubspotToken);
        }
    }
}
