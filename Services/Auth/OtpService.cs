using System.Collections.Concurrent;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;

namespace PicoPlus.Services.Auth;

/// <summary>
/// Service for generating and validating OTP (One-Time Password) codes
/// </summary>
public class OtpService
{
    private readonly ILogger<OtpService> _logger;
    private readonly ConcurrentDictionary<string, OtpData> _otpStore = new();
    private readonly TimeSpan _otpExpiration = TimeSpan.FromMinutes(5);

    public OtpService(ILogger<OtpService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Generate a new 6-digit OTP code
    /// </summary>
    public string GenerateOtp()
    {
        var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        _logger.LogDebug("Generated OTP code");
        return code;
    }

    /// <summary>
    /// Store OTP code for a phone number
    /// </summary>
    public void StoreOtp(string phoneNumber, string otpCode)
    {
        var normalizedPhone = NormalizePhoneNumber(phoneNumber);

        var otpData = new OtpData
        {
            Code = otpCode,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.Add(_otpExpiration),
            AttemptCount = 0
        };

        _otpStore.AddOrUpdate(normalizedPhone, otpData, (_, _) => otpData);

        _logger.LogInformation("OTP stored for phone: {Phone}, expires at: {ExpiresAt}",
            normalizedPhone, otpData.ExpiresAt);
    }

    /// <summary>
    /// Validate OTP code for a phone number
    /// </summary>
    public OtpValidationResult ValidateOtp(string phoneNumber, string enteredCode)
    {
        var normalizedPhone = NormalizePhoneNumber(phoneNumber);

        _logger.LogInformation("Validating OTP for phone: {Phone}", normalizedPhone);

        if (!_otpStore.TryGetValue(normalizedPhone, out var otpData))
        {
            _logger.LogWarning("OTP validation failed: No OTP found for phone: {Phone}", normalizedPhone);
            return new OtpValidationResult
            {
                IsValid = false,
                ErrorMessage = "کد تایید یافت نشد. لطفا مجددا درخواست دهید."
            };
        }

        // Check expiration
        if (DateTime.UtcNow > otpData.ExpiresAt)
        {
            _logger.LogWarning("OTP validation failed: Expired OTP for phone: {Phone}", normalizedPhone);
            _otpStore.TryRemove(normalizedPhone, out _);
            return new OtpValidationResult
            {
                IsValid = false,
                ErrorMessage = "کد تایید منقضی شده است. لطفا مجددا درخواست دهید."
            };
        }

        // Increment attempt count
        otpData.AttemptCount++;

        // Check max attempts
        if (otpData.AttemptCount > 3)
        {
            _logger.LogWarning("OTP validation failed: Max attempts exceeded for phone: {Phone}", normalizedPhone);
            _otpStore.TryRemove(normalizedPhone, out _);
            return new OtpValidationResult
            {
                IsValid = false,
                ErrorMessage = "تعداد تلاش‌ها بیش از حد مجاز است. لطفا مجددا درخواست دهید."
            };
        }

        // Normalize and trim both codes for comparison
        var normalizedStoredCode = otpData.Code.Trim();
        var normalizedEnteredCode = enteredCode.Trim();

        // Validate code
        if (normalizedStoredCode == normalizedEnteredCode)
        {
            _logger.LogInformation("OTP validation successful for phone: {Phone}", normalizedPhone);
            _otpStore.TryRemove(normalizedPhone, out _);
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
            ErrorMessage = $"کد تایید اشتباه است. ({3 - otpData.AttemptCount} بار دیگر مجاز هستید)"
        };
    }

    /// <summary>
    /// Check if OTP exists and is valid for a phone number
    /// </summary>
    public bool HasValidOtp(string phoneNumber)
    {
        var normalizedPhone = NormalizePhoneNumber(phoneNumber);

        if (!_otpStore.TryGetValue(normalizedPhone, out var otpData))
            return false;

        return DateTime.UtcNow <= otpData.ExpiresAt;
    }

    /// <summary>
    /// Get remaining time for OTP
    /// </summary>
    public TimeSpan? GetRemainingTime(string phoneNumber)
    {
        var normalizedPhone = NormalizePhoneNumber(phoneNumber);

        if (!_otpStore.TryGetValue(normalizedPhone, out var otpData))
            return null;

        var remaining = otpData.ExpiresAt - DateTime.UtcNow;

        return remaining.TotalSeconds > 0 ? remaining : null;
    }

    /// <summary>
    /// Clear OTP for a phone number
    /// </summary>
    public void ClearOtp(string phoneNumber)
    {
        var normalizedPhone = NormalizePhoneNumber(phoneNumber);
        _otpStore.TryRemove(normalizedPhone, out _);
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
            _otpStore.TryRemove(key, out _);
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
}

/// <summary>
/// Result of OTP validation
/// </summary>
public class OtpValidationResult
{
    public bool IsValid { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}
