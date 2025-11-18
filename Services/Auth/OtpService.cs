using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace PicoPlus.Services.Auth;

/// <summary>
/// Service for generating and validating OTP (One-Time Password) codes
/// </summary>
public class OtpService
{
    private readonly ILogger<OtpService> _logger;
    private readonly Dictionary<string, OtpData> _otpStore = new();
    private readonly Dictionary<string, RateLimitData> _rateLimitStore = new();
    private readonly TimeSpan _otpExpiration = TimeSpan.FromMinutes(5);
    private readonly TimeSpan _rateLimitWindow = TimeSpan.FromMinutes(15);
    private const int MaxOtpRequestsPerWindow = 5;

    public OtpService(ILogger<OtpService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Generate a new 6-digit OTP code using cryptographically secure random number generation
    /// </summary>
    public string GenerateOtp()
    {
        // Use cryptographically secure random number generator
        var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        _logger.LogDebug("Generated OTP code");
        return code;
    }

    /// <summary>
    /// Store OTP code for a phone number with rate limiting
    /// </summary>
    public bool StoreOtp(string phoneNumber, string otpCode, out string errorMessage)
    {
        errorMessage = string.Empty;
        var normalizedPhone = NormalizePhoneNumber(phoneNumber);

        // Check rate limit
        if (!CheckRateLimit(normalizedPhone))
        {
            errorMessage = "تعداد درخواست‌های OTP بیش از حد مجاز است. لطفاً بعداً تلاش کنید.";
            _logger.LogWarning("Rate limit exceeded for phone: {Phone}", normalizedPhone);
            return false;
        }

        _otpStore[normalizedPhone] = new OtpData
        {
            Code = otpCode,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.Add(_otpExpiration),
            AttemptCount = 0
        };

        _logger.LogInformation("OTP stored for phone: {Phone}, expires at: {ExpiresAt}",
            normalizedPhone, _otpStore[normalizedPhone].ExpiresAt);
        
        return true;
    }

    /// <summary>
    /// Check if rate limit is exceeded for a phone number
    /// </summary>
    private bool CheckRateLimit(string normalizedPhone)
    {
        var now = DateTime.UtcNow;
        
        if (!_rateLimitStore.ContainsKey(normalizedPhone))
        {
            _rateLimitStore[normalizedPhone] = new RateLimitData
            {
                WindowStart = now,
                RequestCount = 1
            };
            return true;
        }

        var rateLimitData = _rateLimitStore[normalizedPhone];
        
        // Reset window if expired
        if (now - rateLimitData.WindowStart > _rateLimitWindow)
        {
            rateLimitData.WindowStart = now;
            rateLimitData.RequestCount = 1;
            return true;
        }

        // Check if limit exceeded
        if (rateLimitData.RequestCount >= MaxOtpRequestsPerWindow)
        {
            return false;
        }

        rateLimitData.RequestCount++;
        return true;
    }

    /// <summary>
    /// Validate OTP code for a phone number
    /// </summary>
    public OtpValidationResult ValidateOtp(string phoneNumber, string enteredCode)
    {
        var normalizedPhone = NormalizePhoneNumber(phoneNumber);

        _logger.LogInformation("Validating OTP for phone: {Phone}", normalizedPhone);

        if (!_otpStore.ContainsKey(normalizedPhone))
        {
            _logger.LogWarning("OTP validation failed: No OTP found for phone: {Phone}", normalizedPhone);
            return new OtpValidationResult
            {
                IsValid = false,
                ErrorMessage = "?? ????? ???? ???. ????? ?????? ????? ????."
            };
        }

        var otpData = _otpStore[normalizedPhone];

        _logger.LogInformation("Stored OTP - CreatedAt: {CreatedAt}, ExpiresAt: {ExpiresAt}, Attempts: {Attempts}",
            otpData.CreatedAt, otpData.ExpiresAt, otpData.AttemptCount);

        // Check expiration
        if (DateTime.UtcNow > otpData.ExpiresAt)
        {
            _logger.LogWarning("OTP validation failed: Expired OTP for phone: {Phone}", normalizedPhone);
            _otpStore.Remove(normalizedPhone);
            return new OtpValidationResult
            {
                IsValid = false,
                ErrorMessage = "?? ????? ????? ??? ???. ????? ?????? ????? ????."
            };
        }

        // Increment attempt count
        otpData.AttemptCount++;

        // Check max attempts
        if (otpData.AttemptCount > 3)
        {
            _logger.LogWarning("OTP validation failed: Max attempts exceeded for phone: {Phone}", normalizedPhone);
            _otpStore.Remove(normalizedPhone);
            return new OtpValidationResult
            {
                IsValid = false,
                ErrorMessage = "????? ???????? ??? ??? ?? ?? ???. ????? ?????? ????? ????."
            };
        }

        // Normalize and trim both codes for comparison
        var normalizedStoredCode = otpData.Code.Trim();
        var normalizedEnteredCode = enteredCode.Trim();

        _logger.LogInformation("Comparing codes - Stored Length: {StoredLength}, Entered Length: {EnteredLength}",
            normalizedStoredCode.Length, normalizedEnteredCode.Length);

        // Validate code
        if (normalizedStoredCode == normalizedEnteredCode)
        {
            _logger.LogInformation("OTP validation successful for phone: {Phone}", normalizedPhone);
            _otpStore.Remove(normalizedPhone);
            return new OtpValidationResult
            {
                IsValid = true,
                ErrorMessage = string.Empty
            };
        }

        _logger.LogWarning("OTP validation failed: Invalid code for phone: {Phone}, attempts: {Attempts}",
            normalizedPhone, otpData.AttemptCount);

        return new OtpValidationResult
        {
            IsValid = false,
            ErrorMessage = $"?? ????? ?????? ???. ({3 - otpData.AttemptCount} ??? ???? ?????????)"
        };
    }

    /// <summary>
    /// Check if OTP exists and is valid for a phone number
    /// </summary>
    public bool HasValidOtp(string phoneNumber)
    {
        var normalizedPhone = NormalizePhoneNumber(phoneNumber);

        if (!_otpStore.ContainsKey(normalizedPhone))
            return false;

        var otpData = _otpStore[normalizedPhone];
        return DateTime.UtcNow <= otpData.ExpiresAt;
    }

    /// <summary>
    /// Get remaining time for OTP
    /// </summary>
    public TimeSpan? GetRemainingTime(string phoneNumber)
    {
        var normalizedPhone = NormalizePhoneNumber(phoneNumber);

        if (!_otpStore.ContainsKey(normalizedPhone))
            return null;

        var otpData = _otpStore[normalizedPhone];
        var remaining = otpData.ExpiresAt - DateTime.UtcNow;

        return remaining.TotalSeconds > 0 ? remaining : null;
    }

    /// <summary>
    /// Clear OTP for a phone number
    /// </summary>
    public void ClearOtp(string phoneNumber)
    {
        var normalizedPhone = NormalizePhoneNumber(phoneNumber);
        _otpStore.Remove(normalizedPhone);
        _logger.LogDebug("OTP cleared for phone: {Phone}", normalizedPhone);
    }

    /// <summary>
    /// Clean up expired OTPs
    /// </summary>
    public void CleanupExpiredOtps()
    {
        var now = DateTime.UtcNow;
        var expiredKeys = _otpStore
            .Where(kvp => now > kvp.Value.ExpiresAt)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _otpStore.Remove(key);
        }

        if (expiredKeys.Any())
        {
            _logger.LogInformation("Cleaned up {Count} expired OTPs", expiredKeys.Count);
        }
    }

    /// <summary>
    /// Normalize phone number for consistent storage
    /// </summary>
    private static string NormalizePhoneNumber(string phoneNumber)
    {
        // Remove all non-digit characters
        var digits = new string(phoneNumber.Where(char.IsDigit).ToArray());

        // If starts with 98, remove it (Iran country code)
        if (digits.StartsWith("98") && digits.Length == 12)
        {
            digits = digits.Substring(2);
        }

        // If starts with 0, keep it
        if (!digits.StartsWith("0") && digits.Length == 10)
        {
            digits = "0" + digits;
        }

        return digits;
    }

    private class OtpData
    {
        public string Code { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public int AttemptCount { get; set; }
    }

    private class RateLimitData
    {
        public DateTime WindowStart { get; set; }
        public int RequestCount { get; set; }
    }
}

/// <summary>
/// Result of OTP validation
/// </summary>
public class OtpValidationResult
{
    public bool IsValid { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}
