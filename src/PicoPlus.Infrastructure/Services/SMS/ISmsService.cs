namespace PicoPlus.Services.SMS
{
    /// <summary>
    /// SMS Provider types
    /// </summary>
    public enum SmsProvider
    {
        FarazSMS,
        SmsIr
    }

    /// <summary>
    /// Common interface for SMS providers
    /// </summary>
    public interface ISmsService
    {
        /// <summary>
        /// Send OTP code to mobile number
        /// </summary>
        Task SendOtpAsync(string mobile, string otpCode);

        /// <summary>
        /// Send welcome message to new user
        /// </summary>
        Task SendWelcomeAsync(string mobile, string firstName, string lastName, string? customerId = null);

        /// <summary>
        /// Send deal closed notification
        /// </summary>
        Task SendDealClosedAsync(string mobile, string firstName, string lastName, string dealId);

        /// <summary>
        /// Get SMS provider name
        /// </summary>
        string ProviderName { get; }
    }
}
