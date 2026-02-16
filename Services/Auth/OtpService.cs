using System.Collections.Concurrent;
using System.Security.Cryptography;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace PicoPlus.Services.Auth;

/// <summary>
/// Service for generating and validating OTP (One-Time Password) codes
/// </summary>
public class OtpService
{
    private readonly ILogger<OtpService> _logger;
    private readonly IMemoryCache _otpStore;
    private readonly ConcurrentDictionary<string, object> _otpLocks = new();
    private readonly TimeSpan _otpExpiration = TimeSpan.FromMinutes(5);
    private const int MaxAttempts = 3;

    public OtpService(ILogger<OtpService> logger, IMemoryCache otpStore)
    {
        _logger = logger;
        _otpStore = otpStore;
    }

    /// <summary>
    /// Generate a new 6-digit OTP code
    /// </summary>
    public string GenerateOtp()
    {
        var code = RandomNumberGenerator.GetInt32(100000, 1_000_000).ToString();
        _logger.LogDebug("Generated OTP code for request");
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

        var entryOptions = CreateEntryOptions(normalizedPhone, otpData.ExpiresAt);
        _otpStore.Set(normalizedPhone, otpData, entryOptions);

        _logger.LogInformation("OTP stored for phone: {Phone}, expires at: {ExpiresAt}",
            normalizedPhone, otpData.ExpiresAt);
    }

    /// <summary>
    /// Validate OTP code for a phone number
    /// </summary>
    public OtpValidationResult ValidateOtp(string phoneNumber, string enteredCode)
    {
        var normalizedPhone = NormalizePhoneNumber(phoneNumber);

        if (string.IsNullOrWhiteSpace(enteredCode))
        {
            _logger.LogWarning("OTP validation failed: Empty OTP input for phone: {Phone}", normalizedPhone);
            return new OtpValidationResult
            {
                IsValid = false,
                ErrorMessage = "?? ????? ?????? ???."
            };
        }

        _logger.LogInformation("Validating OTP for phone: {Phone}", normalizedPhone);
        var otpLock = _otpLocks.GetOrAdd(normalizedPhone, _ => new object());

        lock (otpLock)
        {
            if (!_otpStore.TryGetValue<OtpData>(normalizedPhone, out var otpData) || otpData is null)
            {
                _logger.LogWarning("OTP validation failed: No OTP found for phone: {Phone}", normalizedPhone);
                return new OtpValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "?? ????? ???? ???. ????? ?????? ????? ????."
                };
            }

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

            otpData.AttemptCount++;

            if (otpData.AttemptCount > MaxAttempts)
            {
                _logger.LogWarning("OTP validation failed: Max attempts exceeded for phone: {Phone}", normalizedPhone);
                _otpStore.Remove(normalizedPhone);
                return new OtpValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "????? ???????? ??? ??? ?? ?? ???. ????? ?????? ????? ????."
                };
            }

            var normalizedStoredCode = otpData.Code.Trim();
            var normalizedEnteredCode = enteredCode.Trim();

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

            _otpStore.Set(normalizedPhone, otpData, CreateEntryOptions(normalizedPhone, otpData.ExpiresAt));

            _logger.LogWarning("OTP validation failed: Invalid code for phone: {Phone}, attempts: {Attempts}",
                normalizedPhone, otpData.AttemptCount);

            return new OtpValidationResult
            {
                IsValid = false,
                ErrorMessage = $"?? ????? ?????? ???. ({MaxAttempts - otpData.AttemptCount} ??? ???? ?????????)"
            };
        }
    }

    /// <summary>
    /// Check if OTP exists and is valid for a phone number
    /// </summary>
    public bool HasValidOtp(string phoneNumber)
    {
        var normalizedPhone = NormalizePhoneNumber(phoneNumber);

        if (!_otpStore.TryGetValue<OtpData>(normalizedPhone, out var otpData) || otpData is null)
            return false;

        return DateTime.UtcNow <= otpData.ExpiresAt;
    }

    /// <summary>
    /// Get remaining time for OTP
    /// </summary>
    public TimeSpan? GetRemainingTime(string phoneNumber)
    {
        var normalizedPhone = NormalizePhoneNumber(phoneNumber);

        if (!_otpStore.TryGetValue<OtpData>(normalizedPhone, out var otpData) || otpData is null)
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
        _otpStore.Remove(normalizedPhone);
        _otpLocks.TryRemove(normalizedPhone, out _);
        _logger.LogDebug("OTP cleared for phone: {Phone}", normalizedPhone);
    }

    /// <summary>
    /// Clean up expired OTPs
    /// </summary>
    public void CleanupExpiredOtps()
    {
        _logger.LogDebug("CleanupExpiredOtps invoked. IMemoryCache handles OTP expiration automatically.");
    }

    private MemoryCacheEntryOptions CreateEntryOptions(string normalizedPhone, DateTime expiresAt)
    {
        return new MemoryCacheEntryOptions
        {
            AbsoluteExpiration = expiresAt,
            Size = 1,
            PostEvictionCallbacks =
            {
                new PostEvictionCallbackRegistration
                {
                    EvictionCallback = (_, _, _, state) =>
                    {
                        if (state is not string phone)
                        {
                            return;
                        }

                        _otpLocks.TryRemove(phone, out _);
                    },
                    State = normalizedPhone
                }
            }
        };
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
