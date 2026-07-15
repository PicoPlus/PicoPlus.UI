namespace NovinCRM.Services.SMS
{
    /// <summary>
    /// SMS Provider types
    /// </summary>
    public enum SmsProvider
    {
        FarazSMS,
        SmsIr,
        IPPanel
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
        Task SendDealClosedAsync(string mobile, string firstName, string lastName, string dealId, string dealLink = "");

        /// <summary>
        /// Send order review / invoice link to customer after deal is closed.
        /// Pattern variables: {firstName}, {link}
        /// </summary>
        Task SendOrderReviewAsync(string mobile, string firstName, string invoiceLink);

        /// <summary>
        /// Get SMS provider name
        /// </summary>
        string ProviderName { get; }
    }
}
