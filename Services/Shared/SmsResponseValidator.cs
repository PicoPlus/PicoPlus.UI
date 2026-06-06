using Microsoft.Extensions.Logging;

namespace PicoPlus.Services.Shared;

/// <summary>
/// Shared validator for SMS.ir API responses.
/// Consolidates duplicated response-checking logic from SmsIrService methods.
/// </summary>
public static class SmsResponseValidator
{
    /// <summary>
    /// Validate an SMS.ir response and throw if unsuccessful.
    /// Handles the quirk where SMS.ir sometimes returns success in the message even with non-200 status codes.
    /// </summary>
    public static void ValidateResponse(int? status, string? message, string? messageId, string operationName, ILogger logger)
    {
        if (status == 200 || status == 201 || messageId != null)
        {
            logger.LogInformation("{Operation} sent successfully via SMS.ir. MessageId: {MessageId}",
                operationName, messageId);
            return;
        }

        logger.LogWarning("SMS.ir returned unexpected status: {Status}, Message: {Message}",
            status, message);

        if (!string.IsNullOrEmpty(message) &&
            (message.Contains("موفق") || message.Contains("success", StringComparison.OrdinalIgnoreCase)))
        {
            logger.LogInformation("SMS sent successfully despite non-200 status (Message indicates success)");
            return;
        }

        throw new Exception($"SMS.ir خطا: {message}");
    }
}
