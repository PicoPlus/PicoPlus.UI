using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace PicoPlus.Services.SMS
{
    /// <summary>
    /// SMS.ir implementation of ISmsService
    /// </summary>
    public class SmsIrService : ISmsService
    {
        private readonly SmsIr _smsIr;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SmsIrService> _logger;

        public string ProviderName => "SMS.ir";

        public SmsIrService(SmsIr smsIr, IConfiguration configuration, ILogger<SmsIrService> logger)
        {
            _smsIr = smsIr;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendOtpAsync(string mobile, string otpCode)
        {
            try
            {
                _logger.LogInformation("Sending OTP via SMS.ir to {Mobile}, Code: {Code}", mobile, otpCode);

                var response = await _smsIr.SendOtpAsync(mobile, otpCode);

                _logger.LogInformation("SMS.ir response - Status: {Status}, Message: {Message}, Data: {Data}",
                    response.status, response.message, response.data?.messageId);

                // Check for successful status (200 or 201 for HTTP success)
                if (response.status == 200 || response.status == 201 || response.data?.messageId != null)
                {
                    _logger.LogInformation("OTP sent successfully via SMS.ir. MessageId: {MessageId}",
                        response.data?.messageId);
                }
                else
                {
                    _logger.LogWarning("SMS.ir returned unexpected status: {Status}, Message: {Message}",
                        response.status, response.message);

                    // Don't throw exception if message says success or if we have a messageId
                    // SMS.ir sometimes returns success in the message even with different status codes
                    if (!string.IsNullOrEmpty(response.message) &&
                        (response.message.Contains("????") || response.message.Contains("success", StringComparison.OrdinalIgnoreCase)))
                    {
                        _logger.LogInformation("SMS sent successfully despite non-200 status (Message indicates success)");
                        return;
                    }

                    throw new Exception($"SMS.ir ???: {response.message}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending OTP via SMS.ir to {Mobile}", mobile);
                throw;
            }
        }

        public async Task SendWelcomeAsync(string mobile, string firstName, string lastName, string? customerId = null)
        {
            try
            {
                _logger.LogInformation("Sending welcome message via SMS.ir to {Mobile}", mobile);

                var response = await _smsIr.SendWelcomeAsync(mobile, firstName, lastName);

                _logger.LogInformation("SMS.ir response - Status: {Status}, Message: {Message}",
                    response.status, response.message);

                // Check for successful status (200 or 201 for HTTP success)
                if (response.status == 200 || response.status == 201 || response.data?.messageId != null)
                {
                    _logger.LogInformation("Welcome message sent successfully via SMS.ir. MessageId: {MessageId}",
                        response.data?.messageId);
                }
                else
                {
                    _logger.LogWarning("SMS.ir returned unexpected status: {Status}, Message: {Message}",
                        response.status, response.message);

                    // Don't throw exception if message says success
                    if (!string.IsNullOrEmpty(response.message) &&
                        (response.message.Contains("????") || response.message.Contains("success", StringComparison.OrdinalIgnoreCase)))
                    {
                        _logger.LogInformation("SMS sent successfully despite non-200 status (Message indicates success)");
                        return;
                    }

                    throw new Exception($"SMS.ir ???: {response.message}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending welcome message via SMS.ir to {Mobile}", mobile);
                throw;
            }
        }

        public async Task SendDealClosedAsync(string mobile, string firstName, string lastName, string dealId)
        {
            try
            {
                _logger.LogInformation("Sending deal closed notification via SMS.ir to {Mobile}", mobile);

                var templateId = int.Parse(
                    Environment.GetEnvironmentVariable("SMSIR_DEAL_CLOSED_TEMPLATE_ID")
                    ?? _configuration["SmsIr:Templates:DealClosed"]
                    ?? throw new InvalidOperationException("DealClosed template ID not configured"));

                var parameters = new Dictionary<string, string>
                {
                    { "FirstName", firstName },
                    { "LastName", lastName },
                    { "DealId", dealId }
                };

                var response = await _smsIr.SendNotificationAsync(mobile, templateId, parameters);

                _logger.LogInformation("SMS.ir response - Status: {Status}, Message: {Message}",
                    response.status, response.message);

                // Check for successful status (200 or 201 for HTTP success)
                if (response.status == 200 || response.status == 201 || response.data?.messageId != null)
                {
                    _logger.LogInformation("Deal closed notification sent successfully via SMS.ir. MessageId: {MessageId}",
                        response.data?.messageId);
                }
                else
                {
                    _logger.LogWarning("SMS.ir returned unexpected status: {Status}, Message: {Message}",
                        response.status, response.message);

                    // Don't throw exception if message says success
                    if (!string.IsNullOrEmpty(response.message) &&
                        (response.message.Contains("????") || response.message.Contains("success", StringComparison.OrdinalIgnoreCase)))
                    {
                        _logger.LogInformation("SMS sent successfully despite non-200 status (Message indicates success)");
                        return;
                    }

                    throw new Exception($"SMS.ir ???: {response.message}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending deal closed notification via SMS.ir to {Mobile}", mobile);
                throw;
            }
        }
    }
}
