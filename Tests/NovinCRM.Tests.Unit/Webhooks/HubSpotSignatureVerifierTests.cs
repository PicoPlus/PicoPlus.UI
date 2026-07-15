using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NovinCRM.Infrastructure.Webhooks;
using Xunit;
using FluentAssertions;

namespace NovinCRM.Tests.Unit.Webhooks;

/// <summary>
/// Unit tests for <see cref="HubSpotSignatureVerifier"/>.
/// All tests run purely in-process — no HTTP, no external dependencies.
/// </summary>
public class HubSpotSignatureVerifierTests : IDisposable
{
    private const string ClientSecret = "test-secret-1234";
    private const string Method       = "POST";
    private const string Uri          = "https://example.com/webhooks/hubspot";
    private const string Body         = """[{"eventId":1,"objectId":"123"}]""";

    private readonly IMemoryCache          _cache;
    private readonly HubSpotSignatureVerifier _sut;

    public HubSpotSignatureVerifierTests()
    {
        _cache = new MemoryCache(new MemoryCacheOptions());

        var opts = Options.Create(new HubSpotWebhookOptions
        {
            WebhookClientSecret = ClientSecret,
            MaxRequestAge       = TimeSpan.FromMinutes(5)
        });

        _sut = new HubSpotSignatureVerifier(opts, _cache, NullLogger<HubSpotSignatureVerifier>.Instance);
    }

    // ── Happy path ────────────────────────────────────────────────────────────

    [Fact]
    public void Verify_ValidSignature_ReturnsValid()
    {
        var (timestamp, sig) = BuildSignature(Method, Uri, Body);

        var result = _sut.Verify(sig, timestamp, Method, Uri, Encoding.UTF8.GetBytes(Body));

        result.Should().Be(VerificationResult.Valid);
    }

    // ── Missing / null inputs ─────────────────────────────────────────────────

    [Fact]
    public void Verify_NullSignature_ReturnsMissingSignature()
    {
        var (timestamp, _) = BuildSignature(Method, Uri, Body);

        var result = _sut.Verify(null, timestamp, Method, Uri, Encoding.UTF8.GetBytes(Body));

        result.Should().Be(VerificationResult.MissingSignature);
    }

    [Fact]
    public void Verify_NullTimestamp_ReturnsMissingTimestamp()
    {
        var (_, sig) = BuildSignature(Method, Uri, Body);

        var result = _sut.Verify(sig, null, Method, Uri, Encoding.UTF8.GetBytes(Body));

        result.Should().Be(VerificationResult.MissingTimestamp);
    }

    // ── Timestamp freshness ───────────────────────────────────────────────────

    [Fact]
    public void Verify_ExpiredTimestamp_ReturnsTimestampExpired()
    {
        // Build a signature with a timestamp 10 minutes in the past
        var staleTimestampMs = DateTimeOffset.UtcNow.AddMinutes(-10).ToUnixTimeMilliseconds();
        var source = $"{Method}{Uri}{Body}{staleTimestampMs}";
        var sig    = ComputeHmac(ClientSecret, source);

        var result = _sut.Verify(sig, staleTimestampMs.ToString(), Method, Uri, Encoding.UTF8.GetBytes(Body));

        result.Should().Be(VerificationResult.TimestampExpired);
    }

    // ── Signature mismatch ────────────────────────────────────────────────────

    [Fact]
    public void Verify_WrongSignature_ReturnsSignatureMismatch()
    {
        var (timestamp, _) = BuildSignature(Method, Uri, Body);

        var result = _sut.Verify("totally-wrong", timestamp, Method, Uri, Encoding.UTF8.GetBytes(Body));

        result.Should().Be(VerificationResult.SignatureMismatch);
    }

    // ── Replay protection ─────────────────────────────────────────────────────

    [Fact]
    public void Verify_SameSignatureTwice_SecondReturnsReplayDetected()
    {
        var (timestamp, sig) = BuildSignature(Method, Uri, Body);
        var bodyBytes = Encoding.UTF8.GetBytes(Body);

        var first  = _sut.Verify(sig, timestamp, Method, Uri, bodyBytes);
        var second = _sut.Verify(sig, timestamp, Method, Uri, bodyBytes);

        first.Should().Be(VerificationResult.Valid);
        second.Should().Be(VerificationResult.ReplayDetected);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static (string TimestampMs, string Signature) BuildSignature(
        string method, string uri, string body)
    {
        var tsMs   = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var source = $"{method}{uri}{body}{tsMs}";
        return (tsMs.ToString(), ComputeHmac(ClientSecret, source));
    }

    private static string ComputeHmac(string secret, string source)
    {
        var key   = Encoding.UTF8.GetBytes(secret);
        var data  = Encoding.UTF8.GetBytes(source);
        using var hmac = new HMACSHA256(key);
        return Convert.ToBase64String(hmac.ComputeHash(data));
    }

    public void Dispose() => _cache.Dispose();
}
