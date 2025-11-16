using Microsoft.Extensions.Configuration;
using RestSharp;

namespace PicoPlus.Services.SMS
{
    public partial class SMS
    {
        public class Send
        {
            private readonly IConfiguration _configuration;
            private readonly RestClient _client;
            private readonly string _apiUser;
            private readonly string _apiPassword;
            private readonly string _fromNumber;

            public Send(IConfiguration configuration)
            {
                _configuration = configuration;

                // Read from environment variables first, then configuration
                var baseUrl = Environment.GetEnvironmentVariable("FARAZSMS_BASEURL")
                              ?? configuration["FarazSMS:BaseUrl"]
                              ?? "https://ippanel.com/api/select";

                _client = new RestClient(baseUrl);

                // SMS credentials from environment or configuration
                _apiUser = Environment.GetEnvironmentVariable("FARAZSMS_USER")
                           ?? configuration["FarazSMS:User"]
                           ?? "09109740017";

                _apiPassword = Environment.GetEnvironmentVariable("FARAZSMS_PASSWORD")
                               ?? configuration["FarazSMS:Password"]
                               ?? "Karim50106735";

                _fromNumber = Environment.GetEnvironmentVariable("FARAZSMS_FROM_NUMBER")
                              ?? configuration["FarazSMS:FromNumber"]
                              ?? "3000505";
            }

            private async Task SendSMS<T>(T data) where T : class
            {
                var request = new RestRequest()
                    .AddHeader("accept", "application/json")
                    .AddHeader("content-type", "application/json")
                    .AddJsonBody(data);

                var response = await _client.PostAsync(request);
                // Handle response if needed
            }

            public Task SendWelcomeNew(Models.Services.SMS.SMS.WelcomeNew pData)
            {
                pData.op = "pattern";
                pData.user = _apiUser;
                pData.fromNum = _fromNumber;
                pData.pass = _apiPassword;
                pData.patternCode = Environment.GetEnvironmentVariable("FARAZSMS_PATTERN_WELCOME")
                                    ?? _configuration["FarazSMS:Patterns:Welcome"]
                                    ?? "hjdntm0kxrir9nb";
                return SendSMS(pData);
            }

            public Task SendDealClosedWon(Models.Services.SMS.SMS.DealClosedWon pData)
            {
                pData.op = "pattern";
                pData.user = _apiUser;
                pData.fromNum = _fromNumber;
                pData.pass = _apiPassword;
                pData.patternCode = Environment.GetEnvironmentVariable("FARAZSMS_PATTERN_DEAL_CLOSED")
                                    ?? _configuration["FarazSMS:Patterns:DealClosed"]
                                    ?? "sarlemrkderzb4c";
                return SendSMS(pData);
            }

            public Task SendOTP(Models.Services.SMS.SMS.SenOTP pData)
            {
                pData.op = "pattern";
                pData.user = _apiUser;
                pData.fromNum = _fromNumber;
                pData.pass = _apiPassword;
                pData.patternCode = Environment.GetEnvironmentVariable("FARAZSMS_PATTERN_OTP")
                                    ?? _configuration["FarazSMS:Patterns:OTP"]
                                    ?? "rw4oh5fhij1ntvq";
                return SendSMS(pData);
            }
        }
    }
}
