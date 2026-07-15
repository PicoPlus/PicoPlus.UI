using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace NovinCRM.Services.SMS;

/// <summary>
/// Factory that resolves the active <see cref="ISmsService"/> by reading the
/// <c>SMS:Provider</c> configuration key (or <c>SMS_PROVIDER</c> env var) and
/// asking the DI container for the matching keyed registration.
///
/// Open/Closed: adding a new provider requires only:
///   1. Implement <see cref="ISmsService"/>
///   2. Register with <c>AddKeyedScoped&lt;ISmsService, MyProvider&gt;("mykey")</c>
///   3. Set <c>SMS:Provider=mykey</c> in config
///
/// No changes to this factory or the <see cref="SmsProvider"/> enum are needed.
/// </summary>
public class SmsServiceFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration   _configuration;
    private readonly ILogger<SmsServiceFactory> _logger;

    // Key constants — must match the keyed DI registrations in Program.cs
    public const string KeySmsIr    = "smsir";
    public const string KeyFarazSms = "farazsms";
    public const string KeyIpPanel  = "ippanel";

    public SmsServiceFactory(
        IServiceProvider serviceProvider,
        IConfiguration   configuration,
        ILogger<SmsServiceFactory> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration   = configuration;
        _logger          = logger;
    }

    /// <summary>Returns the provider selected in configuration.</summary>
    public ISmsService GetSmsService()
    {
        var key = (Environment.GetEnvironmentVariable("SMS_PROVIDER")
                   ?? _configuration["SMS:Provider"]
                   ?? KeyIpPanel).ToLowerInvariant();

        _logger.LogInformation("Using SMS provider key: {ProviderKey}", key);

        var service = _serviceProvider.GetKeyedService<ISmsService>(key);
        if (service is not null) return service;

        _logger.LogWarning(
            "SMS provider key '{Key}' not found — falling back to {Default}",
            key, KeyIpPanel);
        return _serviceProvider.GetRequiredKeyedService<ISmsService>(KeyIpPanel);
    }

    /// <summary>Returns a specific provider by its string key.</summary>
    public ISmsService GetSmsService(string providerKey)
    {
        _logger.LogInformation("Requesting SMS provider: {ProviderKey}", providerKey);
        return _serviceProvider.GetRequiredKeyedService<ISmsService>(providerKey.ToLowerInvariant());
    }

    // ── Legacy overload — kept for backwards compatibility during migration ──
    /// <summary>Resolve by the legacy <see cref="SmsProvider"/> enum value.</summary>
    public ISmsService GetSmsService(SmsProvider provider) =>
        GetSmsService(provider switch
        {
            SmsProvider.SmsIr    => KeySmsIr,
            SmsProvider.FarazSMS => KeyFarazSms,
            SmsProvider.IPPanel  => KeyIpPanel,
            _ => throw new ArgumentOutOfRangeException(nameof(provider))
        });
}

/// <summary>
/// Main <see cref="ISmsService"/> implementation that delegates every call to
/// the provider selected at construction time via <see cref="SmsServiceFactory"/>.
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
        => _provider.SendOtpAsync(mobile, otpCode);

    public Task SendWelcomeAsync(string mobile, string firstName, string lastName, string? customerId = null)
        => _provider.SendWelcomeAsync(mobile, firstName, lastName, customerId);

    public Task SendDealClosedAsync(string mobile, string firstName, string lastName, string dealId, string dealLink = "")
        => _provider.SendDealClosedAsync(mobile, firstName, lastName, dealId, dealLink);

    public Task SendOrderReviewAsync(string mobile, string firstName, string invoiceLink)
        => _provider.SendOrderReviewAsync(mobile, firstName, invoiceLink);
}
