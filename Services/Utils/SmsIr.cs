using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace PicoPlus.Services.SMS
{
    /// <summary>
    /// SMS.ir Service - Complete implementation of all SMS.ir API endpoints
    /// Documentation: https://docs.sms.ir/
    /// Base URL: https://api.sms.ir/
    /// </summary>
    public class SmsIr
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SmsIr> _logger;
        private readonly string _apiKey;
        private const string BaseUrl = "https://api.sms.ir";

        public SmsIr(HttpClient httpClient, IConfiguration configuration, ILogger<SmsIr> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;

            // Read from environment variable first, then configuration
            _apiKey = Environment.GetEnvironmentVariable("SMSIR_API_KEY")
                     ?? configuration["SmsIr:ApiKey"]
                     ?? throw new InvalidOperationException("SMS.ir API key is not configured. Set SMSIR_API_KEY environment variable or SmsIr:ApiKey in appsettings.");

            _httpClient.BaseAddress = new Uri(BaseUrl);
        }

        #region Private Helper Methods

        /// <summary>
        /// Generic method to send requests to SMS.ir API
        /// </summary>
        private async Task<TResponse> SendRequestAsync<TRequest, TResponse>(
            string endpoint,
            HttpMethod method,
            TRequest? requestDto = null) where TRequest : class
        {
            try
            {
                var requestUrl = $"{BaseUrl}{endpoint}";
                var requestMessage = new HttpRequestMessage(method, requestUrl);
                requestMessage.Headers.Add("x-api-key", _apiKey);
                requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                if (requestDto != null && (method == HttpMethod.Post || method == HttpMethod.Put))
                {
                    var json = JsonConvert.SerializeObject(requestDto, new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore
                    });
                    requestMessage.Content = new StringContent(json, Encoding.UTF8, "application/json");
                    _logger.LogDebug("SMS.ir Request: {Endpoint}, Body: {Body}", endpoint, json);
                }
                else
                {
                    _logger.LogDebug("SMS.ir Request: {Endpoint}", endpoint);
                }

                var response = await _httpClient.SendAsync(requestMessage);
                var resultString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("SMS.ir request failed: {Endpoint}, Status: {Status}, Response: {Response}",
                        endpoint, response.StatusCode, resultString);
                }

                response.EnsureSuccessStatusCode();

                var result = JsonConvert.DeserializeObject<TResponse>(resultString);
                _logger.LogDebug("SMS.ir request successful: {Endpoint}", endpoint);

                return result!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling SMS.ir endpoint: {Endpoint}", endpoint);
                throw;
            }
        }

        /// <summary>
        /// Send GET request
        /// </summary>
        private async Task<TResponse> GetAsync<TResponse>(string endpoint)
        {
            return await SendRequestAsync<object, TResponse>(endpoint, HttpMethod.Get);
        }

        /// <summary>
        /// Send POST request
        /// </summary>
        private async Task<TResponse> PostAsync<TRequest, TResponse>(string endpoint, TRequest requestDto)
            where TRequest : class
        {
            return await SendRequestAsync<TRequest, TResponse>(endpoint, HttpMethod.Post, requestDto);
        }

        /// <summary>
        /// Send DELETE request
        /// </summary>
        private async Task<TResponse> DeleteAsync<TResponse>(string endpoint)
        {
            return await SendRequestAsync<object, TResponse>(endpoint, HttpMethod.Delete);
        }

        #endregion

        #region Send SMS Methods

        /// <summary>
        /// ????? ????? ???
        /// Send single SMS message
        /// </summary>
        public async Task<Models.Services.SMS.SmsIr.SendSms.Response> SendSmsAsync(
            Models.Services.SMS.SmsIr.SendSms.Request request)
        {
            return await PostAsync<
                Models.Services.SMS.SmsIr.SendSms.Request,
                Models.Services.SMS.SmsIr.SendSms.Response>("/v1/send", request);
        }

        /// <summary>
        /// ????? ????? ?????
        /// Send bulk SMS messages
        /// </summary>
        public async Task<Models.Services.SMS.SmsIr.SendBulk.Response> SendBulkAsync(
            Models.Services.SMS.SmsIr.SendBulk.Request request)
        {
            return await PostAsync<
                Models.Services.SMS.SmsIr.SendBulk.Request,
                Models.Services.SMS.SmsIr.SendBulk.Response>("/v1/send/bulk", request);
        }

        /// <summary>
        /// ????? ?? ?? ??
        /// Send individual messages to different recipients
        /// </summary>
        public async Task<Models.Services.SMS.SmsIr.SendLikeToLike.Response> SendLikeToLikeAsync(
            Models.Services.SMS.SmsIr.SendLikeToLike.Request request)
        {
            return await PostAsync<
                Models.Services.SMS.SmsIr.SendLikeToLike.Request,
                Models.Services.SMS.SmsIr.SendLikeToLike.Response>("/v1/send/likeToLike", request);
        }

        /// <summary>
        /// ????? ?? ????? (????)
        /// Send pattern-based SMS (OTP, notifications)
        /// </summary>
        public async Task<Models.Services.SMS.SmsIr.SendVerify.Response> SendVerifyAsync(
            Models.Services.SMS.SmsIr.SendVerify.Request request)
        {
            return await PostAsync<
                Models.Services.SMS.SmsIr.SendVerify.Request,
                Models.Services.SMS.SmsIr.SendVerify.Response>("/v1/send/verify", request);
        }

        /// <summary>
        /// ????? ????? ????
        /// Send pattern-based SMS to multiple recipients
        /// </summary>
        public async Task<Models.Services.SMS.SmsIr.SendVerifyBulk.Response> SendVerifyBulkAsync(
            Models.Services.SMS.SmsIr.SendVerifyBulk.Request request)
        {
            return await PostAsync<
                Models.Services.SMS.SmsIr.SendVerifyBulk.Request,
                Models.Services.SMS.SmsIr.SendVerifyBulk.Response>("/v1/send/verify/bulk", request);
        }

        #endregion

        #region High-Level Send Methods (Helper Methods)

        /// <summary>
        /// Send OTP code to mobile number
        /// Uses template ID 764597 with parameter name "OTP"
        /// </summary>
        public async Task<Models.Services.SMS.SmsIr.SendVerify.Response> SendOtpAsync(
            string mobile,
            string otpCode,
            int? templateId = null)
        {
            // Default to template ID 764597 for OTP
            var tid = templateId
                ?? int.Parse(Environment.GetEnvironmentVariable("SMSIR_OTP_TEMPLATE_ID")
                ?? _configuration["SmsIr:Templates:OTP"]
                ?? "764597"); // Default OTP template ID

            var request = new Models.Services.SMS.SmsIr.SendVerify.Request
            {
                mobile = mobile,
                templateId = tid,
                parameters = new List<Models.Services.SMS.SmsIr.SendVerify.ParameterItem>
                {
                    new() { name = "OTP", value = otpCode }
                }
            };

            return await SendVerifyAsync(request);
        }

        /// <summary>
        /// Send welcome message to new user
        /// </summary>
        public async Task<Models.Services.SMS.SmsIr.SendVerify.Response> SendWelcomeAsync(
            string mobile,
            string firstName,
            string lastName,
            int? templateId = null)
        {
            var tid = templateId
                ?? int.Parse(Environment.GetEnvironmentVariable("SMSIR_WELCOME_TEMPLATE_ID")
                ?? _configuration["SmsIr:Templates:Welcome"]
                ?? throw new InvalidOperationException("Welcome template ID not configured"));

            var request = new Models.Services.SMS.SmsIr.SendVerify.Request
            {
                mobile = mobile,
                templateId = tid,
                parameters = new List<Models.Services.SMS.SmsIr.SendVerify.ParameterItem>
                {
                    new() { name = "FirstName", value = firstName },
                    new() { name = "LastName", value = lastName }
                }
            };

            return await SendVerifyAsync(request);
        }

        /// <summary>
        /// Send notification with custom parameters
        /// </summary>
        public async Task<Models.Services.SMS.SmsIr.SendVerify.Response> SendNotificationAsync(
            string mobile,
            int templateId,
            Dictionary<string, string> parameters)
        {
            var request = new Models.Services.SMS.SmsIr.SendVerify.Request
            {
                mobile = mobile,
                templateId = templateId,
                parameters = parameters.Select(p => new Models.Services.SMS.SmsIr.SendVerify.ParameterItem
                {
                    name = p.Key,
                    value = p.Value
                }).ToList()
            };

            return await SendVerifyAsync(request);
        }

        #endregion

        #region Report Methods

        /// <summary>
        /// ?????? ????? ????
        /// Get report for a specific message
        /// </summary>
        public async Task<Models.Services.SMS.SmsIr.Report.Response> GetReportAsync(long messageId)
        {
            return await GetAsync<Models.Services.SMS.SmsIr.Report.Response>($"/v1/report/{messageId}");
        }

        /// <summary>
        /// ?????? ????? ??? ????
        /// Get reports for multiple messages
        /// </summary>
        public async Task<Models.Services.SMS.SmsIr.BulkReport.Response> GetBulkReportAsync(
            Models.Services.SMS.SmsIr.BulkReport.Request request)
        {
            return await PostAsync<
                Models.Services.SMS.SmsIr.BulkReport.Request,
                Models.Services.SMS.SmsIr.BulkReport.Response>("/v1/report/live", request);
        }

        /// <summary>
        /// ?????? ????? ???????
        /// Get archived message reports by date range
        /// </summary>
        public async Task<Models.Services.SMS.SmsIr.ArchivedReport.Response> GetArchivedReportAsync(
            Models.Services.SMS.SmsIr.ArchivedReport.Request request)
        {
            return await PostAsync<
                Models.Services.SMS.SmsIr.ArchivedReport.Request,
                Models.Services.SMS.SmsIr.ArchivedReport.Response>("/v1/report/archived", request);
        }

        /// <summary>
        /// ?????? ????? ????????? ???????
        /// Get latest archived messages
        /// </summary>
        public async Task<Models.Services.SMS.SmsIr.LatestArchived.Response> GetLatestArchivedAsync()
        {
            return await GetAsync<Models.Services.SMS.SmsIr.LatestArchived.Response>("/v1/report/archived/latest");
        }

        #endregion

        #region Receive Methods

        /// <summary>
        /// ?????? ???????? ???????
        /// Get received messages
        /// </summary>
        public async Task<Models.Services.SMS.SmsIr.ReceivedMessages.Response> GetReceivedMessagesAsync(
            Models.Services.SMS.SmsIr.ReceivedMessages.Request request)
        {
            return await PostAsync<
                Models.Services.SMS.SmsIr.ReceivedMessages.Request,
                Models.Services.SMS.SmsIr.ReceivedMessages.Response>("/v1/receive/live", request);
        }

        /// <summary>
        /// ?????? ???????? ??????? ???????
        /// Get archived received messages by date range
        /// </summary>
        public async Task<Models.Services.SMS.SmsIr.ArchivedReceived.Response> GetArchivedReceivedAsync(
            Models.Services.SMS.SmsIr.ArchivedReceived.Request request)
        {
            return await PostAsync<
                Models.Services.SMS.SmsIr.ArchivedReceived.Request,
                Models.Services.SMS.SmsIr.ArchivedReceived.Response>("/v1/receive/archived", request);
        }

        /// <summary>
        /// ?????? ????? ???????? ???????
        /// Get latest received messages
        /// </summary>
        public async Task<Models.Services.SMS.SmsIr.LatestReceived.Response> GetLatestReceivedAsync()
        {
            return await GetAsync<Models.Services.SMS.SmsIr.LatestReceived.Response>("/v1/receive/latest");
        }

        #endregion

        #region Account Methods

        /// <summary>
        /// ?????? ?????? ????
        /// Get account credit balance
        /// </summary>
        public async Task<Models.Services.SMS.SmsIr.Credit.Response> GetCreditAsync()
        {
            return await GetAsync<Models.Services.SMS.SmsIr.Credit.Response>("/v1/credit");
        }

        /// <summary>
        /// ?????? ???? ????
        /// Get list of SMS lines
        /// </summary>
        public async Task<Models.Services.SMS.SmsIr.Lines.Response> GetLinesAsync()
        {
            return await GetAsync<Models.Services.SMS.SmsIr.Lines.Response>("/v1/line/list");
        }

        /// <summary>
        /// ?????? ???? ??????
        /// Get list of templates/patterns
        /// </summary>
        public async Task<Models.Services.SMS.SmsIr.Templates.Response> GetTemplatesAsync()
        {
            return await GetAsync<Models.Services.SMS.SmsIr.Templates.Response>("/v1/template/list");
        }

        #endregion

        #region Scheduled Message Methods

        /// <summary>
        /// ??? ???? ????????? ???
        /// Delete scheduled message
        /// </summary>
        public async Task<Models.Services.SMS.SmsIr.DeleteScheduled.Response> DeleteScheduledAsync(long messageId)
        {
            return await DeleteAsync<Models.Services.SMS.SmsIr.DeleteScheduled.Response>($"/v1/send/scheduled/{messageId}");
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Check if account has sufficient credit
        /// </summary>
        public async Task<bool> HasSufficientCreditAsync(decimal requiredAmount)
        {
            try
            {
                var creditResponse = await GetCreditAsync();
                return creditResponse.data?.credit >= requiredAmount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking SMS.ir credit");
                return false;
            }
        }

        /// <summary>
        /// Get default SMS line number
        /// </summary>
        public async Task<string?> GetDefaultLineNumberAsync()
        {
            try
            {
                var linesResponse = await GetLinesAsync();
                return linesResponse.data?.lines?.FirstOrDefault(l => l.isActive == true)?.lineNumber;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting default SMS.ir line");
                return null;
            }
        }

        /// <summary>
        /// Verify message delivery status
        /// </summary>
        public async Task<bool> IsMessageDeliveredAsync(long messageId)
        {
            try
            {
                var report = await GetReportAsync(messageId);
                return report.data?.status == Models.Services.SMS.SmsIr.MessageStatus.Delivered;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking message delivery status: {MessageId}", messageId);
                return false;
            }
        }

        #endregion
    }
}
