using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace NovinCRM.Services.Auth;

/// <summary>
/// Service for generating and validating OTP (One-Time Password) codes.
/// OTP state is stored in <see cref="IDistributedCache"/> (Redis when configured,
/// in-process memory cache otherwise), so codes survive application restarts
/// and work correctly across multiple instances.
/// </summary>
public class OtpService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<OtpService> _logger;
    private readonly TimeSpan _otpExpiration = TimeSpan.FromMinutes(5);

    public OtpService(IDistributedCache cache, ILogger<OtpService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Generate a new 6-digit OTP code
    /// </summary>
    public string GenerateOtp()
    {
        var random = new Random();
        var code = random.Next(100000, 999999).ToString();
        // OTP value is never logged — it is a credential.
        _logger.LogDebug("OTP code generated");
        return code;
    }

    /// <summary>
    /// Store OTP code for a phone number
    /// </summary>
    public void StoreOtp(string phoneNumber, string otpCode)
    {
        var key = CacheKey(NormalizePhoneNumber(phoneNumber));
        var data = new OtpData
        {
            Code = otpCode,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.Add(_otpExpiration),
            AttemptCount = 0
        };

        var json = JsonSerializer.Serialize(data);
        _cache.SetString(key, json, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _otpExpiration
        });

        _logger.LogInformation("OTP stored for phone: {Phone}, expires at: {ExpiresAt}",
            NormalizePhoneNumber(phoneNumber), data.ExpiresAt);
    }

    /// <summary>
    /// Validate OTP code for a phone number
    /// </summary>
    public OtpValidationResult ValidateOtp(string phoneNumber, string enteredCode)
    {
        var normalizedPhone = NormalizePhoneNumber(phoneNumber);
        var key = CacheKey(normalizedPhone);

        _logger.LogInformation("Validating OTP for phone: {Phone}", normalizedPhone);

        var json = _cache.GetString(key);
        if (json is null)
        {
            _logger.LogWarning("OTP validation failed: No OTP found for phone: {Phone}", normalizedPhone);
            return new OtpValidationResult
            {
                IsValid = false,
                ErrorMessage = "کدی یافت نشد. لطفاً دوباره درخواست کنید."
            };
        }

        var otpData = JsonSerializer.Deserialize<OtpData>(json)!;

        _logger.LogDebug("OTP entry found - CreatedAt: {CreatedAt}, ExpiresAt: {ExpiresAt}, Attempts: {Attempts}",
            otpData.CreatedAt, otpData.ExpiresAt, otpData.AttemptCount);

        // Increment attempt count and persist
        otpData.AttemptCount++;
        var updatedJson = JsonSerializer.Serialize(otpData);
        _cache.SetString(key, updatedJson, new DistributedCacheEntryOptions
        {
            // Preserve the remaining TTL by using AbsoluteExpiration (not relative)
            AbsoluteExpiration = otpData.ExpiresAt
        });

        // Check max attempts
        if (otpData.AttemptCount > 3)
        {
            _logger.LogWarning("OTP validation failed: Max attempts exceeded for phone: {Phone}", normalizedPhone);
            _cache.Remove(key);
            return new OtpValidationResult
            {
                IsValid = false,
                ErrorMessage = "تعداد تلاش‌های ناموفق از حد مجاز گذشت. لطفاً دوباره درخواست کنید."
            };
        }

        // Normalize and trim both codes for comparison
        var normalizedStoredCode = otpData.Code.Trim();
        var normalizedEnteredCode = enteredCode.Trim();

        // Validate code — do NOT log OTP values.
        if (normalizedStoredCode == normalizedEnteredCode)
        {
            _logger.LogInformation("OTP validation successful for phone: {Phone}", normalizedPhone);
            _cache.Remove(key);
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
            ErrorMessage = $"کد وارد شده اشتباه است. ({3 - otpData.AttemptCount} بار دیگر می‌توانید امتحان کنید)"
        };
    }

    /// <summary>
    /// Check if OTP exists and is valid for a phone number
    /// </summary>
    public bool HasValidOtp(string phoneNumber)
    {
        var key = CacheKey(NormalizePhoneNumber(phoneNumber));
        return _cache.GetString(key) is not null;
    }

    /// <summary>
    /// Get remaining time for OTP (approximate — based on stored ExpiresAt)
    /// </summary>
    public TimeSpan? GetRemainingTime(string phoneNumber)
    {
        var key = CacheKey(NormalizePhoneNumber(phoneNumber));
        var json = _cache.GetString(key);
        if (json is null) return null;

        var otpData = JsonSerializer.Deserialize<OtpData>(json);
        if (otpData is null) return null;

        var remaining = otpData.ExpiresAt - DateTime.UtcNow;
        return remaining.TotalSeconds > 0 ? remaining : null;
    }

    /// <summary>
    /// Clear OTP for a phone number
    /// </summary>
    public void ClearOtp(string phoneNumber)
    {
        var normalizedPhone = NormalizePhoneNumber(phoneNumber);
        _cache.Remove(CacheKey(normalizedPhone));
        _logger.LogDebug("OTP cleared for phone: {Phone}", normalizedPhone);
    }

    // CleanupExpiredOtps is no longer needed — cache TTL handles expiry automatically.

    private static string CacheKey(string normalizedPhone) => $"otp:{normalizedPhone}";

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

    private sealed class OtpData
    {
        public string Code { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public int AttemptCount { get; set; }
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
