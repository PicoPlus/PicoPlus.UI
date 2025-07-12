using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace PicoPlus.Services.Identity
{
    public class Zibal
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        private readonly string _token;

        public Zibal(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;

            _token = configuration["Zibal:Token"] ?? "de2dbc2e1db94d22bf212584598cd1a2";
        }

        public async Task<Models.Services.Identity.Zibal.NationalIdentityInquiry.Response> NationalIdentityInquiry(
            Models.Services.Identity.Zibal.NationalIdentityInquiry.Request requestDto)
        {
            var requestUrl = "https://api.zibal.ir/v1/facility/nationalIdentityInquiry";
            var json = JsonConvert.SerializeObject(requestDto);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
            requestMessage.Content = httpContent;

            var response = await _httpClient.SendAsync(requestMessage);
            response.EnsureSuccessStatusCode();

            var resultString = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<Models.Services.Identity.Zibal.NationalIdentityInquiry.Response>(resultString);
        }

        public async Task<Models.Services.Identity.Zibal.ShahkarInquiry.Response> ShahkerInquiry(
            Models.Services.Identity.Zibal.ShahkarInquiry.Request requestDto)
        {
            var requestUrl = "https://api.zibal.ir/v1/facility/shahkarInquiry";
            var json = JsonConvert.SerializeObject(requestDto);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
            requestMessage.Content = httpContent;

            var response = await _httpClient.SendAsync(requestMessage);
            response.EnsureSuccessStatusCode();

            var resultString = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<Models.Services.Identity.Zibal.ShahkarInquiry.Response>(resultString);
        }

        public async Task<string> GetPostalCode(string zipCode)
        {
            var requestUrl = "https://api.zibal.ir/v1/facility/postalCodeInquiry";

            var body = new Models.Services.Identity.Zibal.GetPostalCode.Request
            {
                postalCode = zipCode
            };

            var json = JsonConvert.SerializeObject(body);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
            requestMessage.Content = httpContent;

            var response = await _httpClient.SendAsync(requestMessage);

            var resultString = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var resData = JsonConvert.DeserializeObject<Models.Services.Identity.Zibal.GetPostalCode.Response>(resultString);

                var address = $"{resData.data.address.province}, " +
                              $"{resData.data.address.town}, " +
                              $"{resData.data.address.district}, " +
                              $"{resData.data.address.street}, {resData.data.address.street2}, پلاک {resData.data.address.number}, " +
                              $"طبقه {resData.data.address.floor}, واحد {resData.data.address.sideFloor}, نام ساختمان {resData.data.address.buildingName}";

                return address;
            }

            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                var resError = JsonConvert.DeserializeObject<Models.Services.Identity.Zibal.GetPostalCode.Response>(resultString);
                return resError.message ?? "Unknown BadRequest";
            }

            return "Unknown error occurred";
        }
    }
}
