using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace PicoPlus.Services.SMS
{
    /// <summary>
    /// FarazSMS implementation of ISmsService (wrapper for existing SMS.Send class)
    /// </summary>
    public class FarazSmsService : ISmsService
    {
        private readonly SMS.Send _farazSms;
        private readonly ILogger<FarazSmsService> _logger;

        public string ProviderName => "FarazSMS";

        public FarazSmsService(SMS.Send farazSms, ILogger<FarazSmsService> logger)
        {
            _farazSms = farazSms;
            _logger = logger;
        }

        public async Task SendOtpAsync(string mobile, string otpCode)
        {
            try
            {
                _logger.LogInformation("Sending OTP via FarazSMS to {Mobile}", mobile);

                var data = new Models.Services.SMS.SMS.SenOTP
                {
                    toNum = mobile,
                    inputData = new List<Models.Services.SMS.SMS.SenOTPInputdata>
                    {
                        new() { otp = otpCode }
                    }
                };

                await _farazSms.SendOTP(data);

                _logger.LogInformation("OTP sent successfully via FarazSMS to {Mobile}", mobile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending OTP via FarazSMS to {Mobile}", mobile);
                throw;
            }
        }

        public async Task SendWelcomeAsync(string mobile, string firstName, string lastName, string? customerId = null)
        {
            try
            {
                _logger.LogInformation("Sending welcome message via FarazSMS to {Mobile}", mobile);

                var data = new Models.Services.SMS.SMS.WelcomeNew
                {
                    toNum = mobile,
                    inputData = new List<Models.Services.SMS.SMS.WelcomeNewInputdata>
                    {
                        new()
                        {
                            firstname = firstName,
                            lastname = lastName,
                            cid = customerId ?? ""
                        }
                    }
                };

                await _farazSms.SendWelcomeNew(data);

                _logger.LogInformation("Welcome message sent successfully via FarazSMS to {Mobile}", mobile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending welcome message via FarazSMS to {Mobile}", mobile);
                throw;
            }
        }

        public async Task SendDealClosedAsync(string mobile, string firstName, string lastName, string dealId)
        {
            try
            {
                _logger.LogInformation("Sending deal closed notification via FarazSMS to {Mobile}", mobile);

                var data = new Models.Services.SMS.SMS.DealClosedWon
                {
                    toNum = mobile,
                    inputData = new List<Models.Services.SMS.SMS.DealClosedWonInputdata>
                    {
                        new()
                        {
                            firstname = firstName,
                            lastname = lastName,
                            id = dealId
                        }
                    }
                };

                await _farazSms.SendDealClosedWon(data);

                _logger.LogInformation("Deal closed notification sent successfully via FarazSMS to {Mobile}", mobile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending deal closed notification via FarazSMS to {Mobile}", mobile);
                throw;
            }
        }
    }
}
