using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace PicoPlus.Services.SMS
{
    /// <summary>
    /// Factory for creating SMS service instances based on configuration
    /// </summary>
    public class SmsServiceFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SmsServiceFactory> _logger;

        public SmsServiceFactory(
            IServiceProvider serviceProvider,
            IConfiguration configuration,
            ILogger<SmsServiceFactory> logger)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Get the configured SMS service provider
        /// </summary>
        public ISmsService GetSmsService()
        {
            var provider = GetConfiguredProvider();

            _logger.LogInformation("Using SMS provider: {Provider}", provider);

            return provider switch
            {
                SmsProvider.SmsIr => (ISmsService)_serviceProvider.GetService(typeof(SmsIrService))!,
                SmsProvider.FarazSMS => (ISmsService)_serviceProvider.GetService(typeof(FarazSmsService))!,
                _ => throw new InvalidOperationException($"Unknown SMS provider: {provider}")
            };
        }

        /// <summary>
        /// Get specific SMS service provider
        /// </summary>
        public ISmsService GetSmsService(SmsProvider provider)
        {
            _logger.LogInformation("Requesting SMS provider: {Provider}", provider);

            return provider switch
            {
                SmsProvider.SmsIr => (ISmsService)_serviceProvider.GetService(typeof(SmsIrService))!,
                SmsProvider.FarazSMS => (ISmsService)_serviceProvider.GetService(typeof(FarazSmsService))!,
                _ => throw new InvalidOperationException($"Unknown SMS provider: {provider}")
            };
        }

        /// <summary>
        /// Get configured provider from settings
        /// </summary>
        private SmsProvider GetConfiguredProvider()
        {
            var providerName = Environment.GetEnvironmentVariable("SMS_PROVIDER")
                ?? _configuration["SMS:Provider"]
                ?? "FarazSMS";

            if (Enum.TryParse<SmsProvider>(providerName, true, out var provider))
            {
                return provider;
            }

            _logger.LogWarning("Invalid SMS provider configured: {Provider}. Defaulting to FarazSMS", providerName);
            return SmsProvider.FarazSMS;
        }
    }

    /// <summary>
    /// Main SMS service that delegates to configured provider
    /// </summary>
    public class SmsService : ISmsService
    {
        private readonly ISmsService _provider;

        public string ProviderName => _provider.ProviderName;

        public SmsService(SmsServiceFactory factory)
        {
            _provider = factory.GetSmsService();
        }

        public Task SendOtpAsync(string mobile, string otpCode)
        {
            return _provider.SendOtpAsync(mobile, otpCode);
        }

        public Task SendWelcomeAsync(string mobile, string firstName, string lastName, string? customerId = null)
        {
            return _provider.SendWelcomeAsync(mobile, firstName, lastName, customerId);
        }

        public Task SendDealClosedAsync(string mobile, string firstName, string lastName, string dealId)
        {
            return _provider.SendDealClosedAsync(mobile, firstName, lastName, dealId);
        }
    }
}
