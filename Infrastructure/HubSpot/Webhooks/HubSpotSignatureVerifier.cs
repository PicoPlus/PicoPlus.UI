#nullable enable

using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace NovinCRM.Infrastructure.Webhooks;

/// <summary>
/// Verifies HubSpot webhook request authenticity using the v3 signature scheme.
///
/// Algorithm (per HubSpot docs):
///   1. Concatenate: HTTP_METHOD + requestUri + rawBody + timestamp
///   2. Compute HMAC-SHA256 over that string using the app's Client Secret.
///   3. Base64-encode the digest.
///   4. Timing-safe compare against the value in X-HubSpot-Signature-v3.
///
/// Replay-attack protection:
///   The X-HubSpot-Request-Timestamp header is a Unix-epoch-millisecond value.
///   Requests older than <see cref="HubSpotWebhookOptions.MaxRequestAge"/> are
///   rejected outright. Additionally the raw (method+uri+timestamp) hash key is
///   stored in a short-lived IMemoryCache so that the same payload cannot be
///   replayed within the acceptance window even if the timestamp is still fresh.
///
/// v1/v2 fallback is intentionally NOT supported — only v3.
/// </summary>
public sealed class HubSpotSignatureVerifier
{
    private readonly HubSpotWebhookOptions _options;
    private readonly IMemoryCache          _nonceCache;
    private readonly ILogger<HubSpotSignatureVerifier> _logger;

    // Cache entry lifetime = max request age + 30 s buffer so a nonce cannot
    // be replayed even right at the edge of the time window.
    private TimeSpan NonceTtl => _options.MaxRequestAge + TimeSpan.FromSeconds(30);

    public HubSpotSignatureVerifier(
        IOptions<HubSpotWebhookOptions> options,
        IMemoryCache nonceCache,
        ILogger<HubSpotSignatureVerifier> logger)
    {
        _options    = options.Value;
        _nonceCache = nonceCache;
        _logger     = logger;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Fully validates the incoming webhook request.
    /// Returns <c>true</c> iff the signature is valid, the timestamp is fresh,
    /// and the request has not been seen before (replay protection).
    /// </summary>
    /// <param name="signatureV3">Value of X-HubSpot-Signature-v3 header.</param>
    /// <param name="timestampHeader">Value of X-HubSpot-Request-Timestamp header (ms since epoch).</param>
    /// <param name="method">HTTP method, upper-cased (e.g. "POST").</param>
    /// <param name="requestUri">Full request URI including scheme, host, path, and query.</param>
    /// <param name="rawBody">Raw UTF-8 request body bytes read before JSON deserialization.</param>
    public VerificationResult Verify(
        string? signatureV3,
        string? timestampHeader,
        string  method,
        string  requestUri,
        ReadOnlySpan<byte> rawBody)
    {
        // ── 1. Presence checks ────────────────────────────────────────────────
        if (string.IsNullOrEmpty(signatureV3))
        {
            _logger.LogWarning("Webhook rejected: X-HubSpot-Signature-v3 header missing");
            return VerificationResult.MissingSignature;
        }

        if (string.IsNullOrEmpty(timestampHeader))
        {
            _logger.LogWarning("Webhook rejected: X-HubSpot-Request-Timestamp header missing");
            return VerificationResult.MissingTimestamp;
        }

        if (string.IsNullOrEmpty(_options.WebhookClientSecret))
        {
            _logger.LogError("Webhook rejected: HubSpot:WebhookClientSecret is not configured");
            return VerificationResult.ConfigurationError;
        }

        // ── 2. Timestamp freshness ────────────────────────────────────────────
        if (!long.TryParse(timestampHeader, out long timestampMs))
        {
            _logger.LogWarning("Webhook rejected: X-HubSpot-Request-Timestamp is not a valid integer: {Value}", timestampHeader);
            return VerificationResult.InvalidTimestamp;
        }

        var requestTime = DateTimeOffset.FromUnixTimeMilliseconds(timestampMs);
        var age         = DateTimeOffset.UtcNow - requestTime;

        if (age > _options.MaxRequestAge || age < TimeSpan.FromSeconds(-30))
        {
            _logger.LogWarning(
                "Webhook rejected: request timestamp {Timestamp} is outside the acceptance window (age={Age})",
                requestTime, age);
            return VerificationResult.TimestampExpired;
        }

        // ── 3. HMAC-SHA256 v3 ────────────────────────────────────────────────
        //   Signature source: POST{uri}{body}{timestamp}
        //   HubSpot concatenates method + uri + body + timestamp as plain strings.
        var bodyString      = Encoding.UTF8.GetString(rawBody);
        var signatureSource = $"{method.ToUpperInvariant()}{requestUri}{bodyString}{timestampMs}";

        string expectedSignature;
        try
        {
            var secretBytes = Encoding.UTF8.GetBytes(_options.WebhookClientSecret);
            var sourceBytes = Encoding.UTF8.GetBytes(signatureSource);

            using var hmac   = new HMACSHA256(secretBytes);
            var       digest = hmac.ComputeHash(sourceBytes);
            expectedSignature = Convert.ToBase64String(digest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Webhook rejected: error computing HMAC");
            return VerificationResult.ConfigurationError;
        }

        // Timing-safe comparison — prevents timing oracle attacks.
        var expectedBytes = Encoding.UTF8.GetBytes(expectedSignature);
        var receivedBytes = Encoding.UTF8.GetBytes(signatureV3);

        bool signatureValid =
            expectedBytes.Length == receivedBytes.Length &&
            CryptographicOperations.FixedTimeEquals(expectedBytes, receivedBytes);

        if (!signatureValid)
        {
            _logger.LogWarning(
                "Webhook rejected: signature mismatch for request at {Timestamp}", requestTime);
            return VerificationResult.SignatureMismatch;
        }

        // ── 4. Replay protection — nonce cache ────────────────────────────────
        //   Cache key = HMAC of (method + uri + timestamp) — uniquely identifies
        //   this delivery without storing the raw body in the cache.
        var nonceKey = $"hs-webhook-nonce:{signatureV3}";

        if (_nonceCache.TryGetValue(nonceKey, out _))
        {
            _logger.LogWarning(
                "Webhook rejected: replay detected — signature {Sig} already processed",
                signatureV3[..Math.Min(8, signatureV3.Length)] + "…");
            return VerificationResult.ReplayDetected;
        }

        _nonceCache.Set(nonceKey, true, NonceTtl);

        return VerificationResult.Valid;
    }
}

/// <summary>Result codes from <see cref="HubSpotSignatureVerifier.Verify"/>.</summary>
public enum VerificationResult
{
    /// <summary>Request is authentic and has not been replayed.</summary>
    Valid,

    MissingSignature,
    MissingTimestamp,
    InvalidTimestamp,
    TimestampExpired,
    SignatureMismatch,
    ReplayDetected,

    /// <summary>Server-side misconfiguration (client secret not set).</summary>
    ConfigurationError
}
