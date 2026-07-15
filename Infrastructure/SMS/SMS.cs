using Microsoft.Extensions.Configuration;
using RestSharp;

namespace NovinCRM.Services.SMS
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
                              ?? throw new InvalidOperationException(
                                  "FarazSMS base URL is not configured. Set FarazSMS:BaseUrl in configuration.");

                _client = new RestClient(baseUrl);

                // SMS credentials from environment or configuration
                _apiUser = Environment.GetEnvironmentVariable("FARAZSMS_USER")
                           ?? configuration["FarazSMS:User"]
                           ?? throw new InvalidOperationException(
                               "FarazSMS user is not configured. Set FarazSMS:User in configuration.");

                _apiPassword = Environment.GetEnvironmentVariable("FARAZSMS_PASSWORD")
                               ?? configuration["FarazSMS:Password"]
                               ?? throw new InvalidOperationException(
                                   "FarazSMS password is not configured. Set FarazSMS:Password in configuration.");

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
                                    ?? throw new InvalidOperationException(
                                        "FarazSMS Welcome pattern code is not configured. Set FarazSMS:Patterns:Welcome in configuration.");
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
                                    ?? throw new InvalidOperationException(
                                        "FarazSMS DealClosed pattern code is not configured. Set FarazSMS:Patterns:DealClosed in configuration.");
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
                                    ?? throw new InvalidOperationException(
                                        "FarazSMS OTP pattern code is not configured. Set FarazSMS:Patterns:OTP in configuration.");
                return SendSMS(pData);
            }
        }
    }
}
