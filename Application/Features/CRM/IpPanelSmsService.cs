#nullable enable

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PicoPlus.SMS.Models.IPPanel;

namespace PicoPlus.Services.SMS;

/// <summary>
/// IPPanel Edge API implementation of ISmsService.
/// Pattern codes and sender ID are read from env vars → appsettings.
/// </summary>
public class IpPanelSmsService : ISmsService
{
    private readonly IpPanelClient             _client;
    private readonly IConfiguration            _config;
    private readonly ILogger<IpPanelSmsService> _logger;

    public string ProviderName => "IPPanel";

    private string Sender =>
        _config["IPPanel:Sender"] ?? "+983000505";

    /// <summary>Reads pattern ID as a string. Config key holds the pattern ID assigned by IPPanel.</summary>
    private string OtpPatternId =>
        _config["IPPanel:Patterns:OTP"]
        ?? throw new InvalidOperationException(
            "IPPanel:Patterns:OTP must be configured. Check appsettings.json.");

    private string WelcomePatternId =>
        _config["IPPanel:Patterns:Welcome"] ?? string.Empty;

    private string DealClosedPatternId =>
        _config["IPPanel:Patterns:DealClosed"] ?? string.Empty;

    public IpPanelSmsService(
        IpPanelClient             client,
        IConfiguration            config,
        ILogger<IpPanelSmsService> logger)
    {
        _client = client;
        _config = config;
        _logger = logger;
    }

    // ── ISmsService ───────────────────────────────────────────────────────────

    public async Task SendOtpAsync(string mobile, string otpCode)
    {
        _logger.LogInformation("IPPanel: sending OTP to {Mobile}", mobile);

        var req = new SendPatternRequest
        {
            FromNumber = Sender,
            Code       = OtpPatternId,
            Recipients = new List<string> { NormalizePhone(mobile) },
            Params     = new Dictionary<string, string> { ["otp"] = otpCode }
        };

        var res = await _client.SendPatternAsync(req);
        LogResult("OTP", mobile, res);
    }

    public async Task SendWelcomeAsync(string mobile, string firstName, string lastName, string? customerId = null)
    {
        _logger.LogInformation("IPPanel: sending welcome to {Mobile}", mobile);

        if (string.IsNullOrEmpty(WelcomePatternId)) { _logger.LogWarning("IPPanel Welcome pattern ID not configured — skipping"); return; }

        var req = new SendPatternRequest
        {
            FromNumber = Sender,
            Code       = WelcomePatternId,
            Recipients = new List<string> { NormalizePhone(mobile) },
            Params     = new Dictionary<string, string>
            {
                ["first_name"]   = firstName,
                ["last_name"]    = lastName,
                ["customer_id"]  = customerId ?? ""
            }
        };

        var res = await _client.SendPatternAsync(req);
        LogResult("Welcome", mobile, res);
    }

    public async Task SendDealClosedAsync(string mobile, string firstName, string lastName, string dealId, string dealLink = "")
    {
        _logger.LogInformation("IPPanel: sending deal-closed to {Mobile}", mobile);

        if (string.IsNullOrEmpty(DealClosedPatternId)) { _logger.LogWarning("IPPanel DealClosed pattern ID not configured — skipping"); return; }

        var req = new SendPatternRequest
        {
            FromNumber = Sender,
            Code       = DealClosedPatternId,
            Recipients = new List<string> { NormalizePhone(mobile) },
            Params     = new Dictionary<string, string>
            {
                ["oid"]       = dealId,
                ["DealiLInk"] = dealLink
            }
        };

        var res = await _client.SendPatternAsync(req);
        LogResult("DealClosed", mobile, res);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Normalise an Iranian mobile to E.164 +989xxxxxxxxx format.</summary>
    private static string NormalizePhone(string phone)
    {
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        if (digits.StartsWith("98") && digits.Length == 12) return "+" + digits;
        if (digits.StartsWith("9")  && digits.Length == 10) return "+98" + digits;
        if (digits.StartsWith("0")  && digits.Length == 11) return "+98" + digits[1..];
        return phone.StartsWith("+") ? phone : "+" + digits; // pass through
    }

    private void LogResult(string type, string mobile, SendPatternResponse res)
    {
        var ok      = res.Meta?.Status ?? false;
        var message = res.Meta?.Message;
        var code    = res.Meta?.MessageCode;

        if (ok)
            _logger.LogInformation("IPPanel {Type} sent to {Mobile}: {Code} {Message}", type, mobile, code, message);
        else
            _logger.LogWarning("IPPanel {Type} to {Mobile} failed — {Code}: {Message}", type, mobile, code, message);
    }
}
