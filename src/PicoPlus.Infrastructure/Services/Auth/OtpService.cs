using Microsoft.Extensions.Logging;

namespace PicoPlus.Services.Auth;

/// <summary>
/// Service for generating and validating OTP (One-Time Password) codes
/// </summary>
public class OtpService
{
    private readonly ILogger<OtpService> _logger;
    private readonly Dictionary<string, OtpData> _otpStore = new();
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
        var random = new Random();
        var code = random.Next(100000, 999999).ToString();
        _logger.LogDebug("Generated OTP code: {Code}", code);
        return code;
    }

    /// <summary>
    /// Store OTP code for a phone number
    /// </summary>
    public void StoreOtp(string phoneNumber, string otpCode)
    {
        var normalizedPhone = NormalizePhoneNumber(phoneNumber);

        _otpStore[normalizedPhone] = new OtpData
        {
            Code = otpCode,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.Add(_otpExpiration),
            AttemptCount = 0
        };

        _logger.LogInformation("OTP stored for phone: {Phone}, Code: {Code}, expires at: {ExpiresAt}",
            normalizedPhone, otpCode, _otpStore[normalizedPhone].ExpiresAt);
    }

    /// <summary>
    /// Validate OTP code for a phone number
    /// </summary>
    public OtpValidationResult ValidateOtp(string phoneNumber, string enteredCode)
    {
        var normalizedPhone = NormalizePhoneNumber(phoneNumber);

        _logger.LogInformation("Validating OTP - Phone: {Phone}, Entered Code: {EnteredCode}", normalizedPhone, enteredCode);

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

        _logger.LogInformation("Stored OTP - Code: {StoredCode}, CreatedAt: {CreatedAt}, ExpiresAt: {ExpiresAt}, Attempts: {Attempts}",
            otpData.Code, otpData.CreatedAt, otpData.ExpiresAt, otpData.AttemptCount);

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

        _logger.LogInformation("Comparing codes - Stored: '{StoredCode}' (Length: {StoredLength}), Entered: '{EnteredCode}' (Length: {EnteredLength})",
            normalizedStoredCode, normalizedStoredCode.Length, normalizedEnteredCode, normalizedEnteredCode.Length);

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
}

/// <summary>
/// Result of OTP validation
/// </summary>
public class OtpValidationResult
{
    public bool IsValid { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}
