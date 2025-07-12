
using Microsoft.Extensions.Configuration;

namespace PicoPlus.Services.SMS
{
    public partial class SMS
    {
        public class Send
        {
            private  IConfiguration _configuration;
            private RestClient _client { get; set; }

            public Send(IConfiguration configuration)
            {
                _configuration = configuration;
                _client = new RestClient("https://ippanel.com/api/select");
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
                pData.user = "09109740017";
                pData.fromNum = "3000505";
                pData.pass = "Karim50106735";
                pData.patternCode = "hjdntm0kxrir9nb";
                return SendSMS(pData);
            }

            public Task SendDealClosedWon(Models.Services.SMS.SMS.DealClosedWon pData)
            {
                pData.op = "pattern";
                pData.user = "09109740017";
                pData.fromNum = "3000505";
                pData.pass = "Karim50106735";
                pData.patternCode = "sarlemrkderzb4c";
                return SendSMS(pData);
            }
            public Task SendOTP(Models.Services.SMS.SMS.SenOTP pData)
            {

                pData.op = "pattern";
                pData.user = "09109740017";
                pData.fromNum = "3000505";
                pData.pass = "Karim50106735";
                pData.patternCode = "rw4oh5fhij1ntvq";
              
                return SendSMS(pData);
            }
        }
    }
}
