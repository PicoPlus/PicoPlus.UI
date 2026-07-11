#nullable enable

using System.Text.Json;
using PicoPlus.Application.Common.Interfaces;
using PicoPlus.Domain.Webhooks;
using PicoPlus.Infrastructure.Webhooks;

namespace PicoPlus.Presentation.Webhooks;

/// <summary>
/// Maps the HubSpot webhook endpoint: POST /webhooks/hubspot
///
/// Contract with HubSpot:
///   • Must return HTTP 200 within a few seconds or HubSpot considers the delivery failed.
///   • Returning 200 does NOT mean the event has been processed — only received and queued.
///   • Non-200 responses trigger automatic retries (up to ~10 attempts with back-off).
///   • 4xx responses (except 429) cause HubSpot to stop retrying for that delivery.
///
/// This endpoint:
///   1. Reads the raw body once (before any deserialization — required for HMAC).
///   2. Verifies the v3 HMAC signature + timestamp freshness + replay cache.
///   3. Deserializes the JSON array of events.
///   4. Stamps each event with its parsed ObjectType / EventType.
///   5. Enqueues the batch — non-blocking.
///   6. Returns 200 immediately.
///
/// <b>Antiforgery is disabled</b> for this endpoint — HubSpot cannot produce
/// antiforgery tokens and the HMAC signature serves the equivalent purpose.
/// </summary>
public static class HubSpotWebhookEndpoint
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Registers the endpoint on the <see cref="WebApplication"/>.
    /// Call from <c>Program.cs</c> after all middleware is configured.
    /// </summary>
    public static IEndpointRouteBuilder MapHubSpotWebhook(this IEndpointRouteBuilder app)
    {
        // DisableAntiforgery() is required because this is an unauthenticated
        // inbound call from HubSpot's servers — antiforgery tokens are not applicable.
        // Security is provided by the HMAC-SHA256 v3 signature instead.
        app.MapPost("/webhooks/hubspot", HandleAsync)
           .DisableAntiforgery()
           .WithName("HubSpotWebhook")
           .WithTags("Webhooks")
           .Produces(StatusCodes.Status200OK)
           .Produces(StatusCodes.Status400BadRequest)
           .Produces(StatusCodes.Status401Unauthorized);

        return app;
    }

    // ── Handler ───────────────────────────────────────────────────────────────

    private static async Task<IResult> HandleAsync(
        HttpContext                   httpContext,
        HubSpotSignatureVerifier      verifier,
        IWebhookEventQueue            queue,
        ILogger<Program>              logger)
    {
        // ── Step 1: Read the raw body synchronously before any framework binding.
        //    The HMAC must be computed over the exact bytes HubSpot sent.
        //    EnableBuffering() would let us re-read, but we need the raw body
        //    anyway, so we own the read here.
        byte[] rawBodyBytes;
        try
        {
            using var ms = new MemoryStream();
            await httpContext.Request.Body.CopyToAsync(ms).ConfigureAwait(false);
            rawBodyBytes = ms.ToArray();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "HubSpotWebhook: failed to read request body");
            return Results.StatusCode(StatusCodes.Status400BadRequest);
        }

        // ── Step 2: Verify signature + timestamp + replay protection.
        var signatureHeader  = httpContext.Request.Headers["X-HubSpot-Signature-v3"].FirstOrDefault();
        var timestampHeader  = httpContext.Request.Headers["X-HubSpot-Request-Timestamp"].FirstOrDefault();

        // Build the full URI as HubSpot signed it (scheme://host/path?query).
        var request    = httpContext.Request;
        var requestUri = $"{request.Scheme}://{request.Host}{request.Path}{request.QueryString}";

        var result = verifier.Verify(
            signatureV3:     signatureHeader,
            timestampHeader: timestampHeader,
            method:          "POST",
            requestUri:      requestUri,
            rawBody:         rawBodyBytes.AsSpan());

        if (result != VerificationResult.Valid)
        {
            logger.LogWarning(
                "HubSpotWebhook: rejected — {Reason} | URI={Uri} | Timestamp={Ts}",
                result, requestUri, timestampHeader);

            // Return 401 for signature/replay failures so HubSpot does not retry
            // (a 4xx other than 429 stops retries). Return 400 for parse failures.
            return result is VerificationResult.ConfigurationError
                ? Results.StatusCode(StatusCodes.Status500InternalServerError)
                : Results.Unauthorized();
        }

        // ── Step 3: Deserialize the JSON array.
        IReadOnlyList<HubSpotWebhookEvent>? events;
        try
        {
            events = JsonSerializer.Deserialize<List<HubSpotWebhookEvent>>(
                rawBodyBytes, JsonOptions);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "HubSpotWebhook: JSON deserialization failed");
            return Results.BadRequest("Malformed JSON payload");
        }

        if (events is null || events.Count == 0)
        {
            // HubSpot occasionally sends empty test pings — respond 200 silently.
            logger.LogDebug("HubSpotWebhook: received empty or null event array");
            return Results.Ok();
        }

        // ── Step 4: Stamp parsed ObjectType / EventType onto each event record.
        var enrichedEvents = events
            .Select(SubscriptionTypeParser.WithParsedType)
            .ToList();

        // ── Step 5: Enqueue with back-pressure — awaits capacity, bounded by ct.
        //    WriteAsync honours the request's CancellationToken so if Kestrel
        //    closes the connection the write is aborted cleanly.
        await queue.WriteAsync(enrichedEvents, httpContext.RequestAborted).ConfigureAwait(false);

        logger.LogInformation(
            "HubSpotWebhook: queued {Count} event(s) from portal {PortalId} (queue depth: {Depth})",
            enrichedEvents.Count,
            enrichedEvents.FirstOrDefault()?.PortalId,
            queue.Count);

        // ── Step 6: Return 200 immediately.
        return Results.Ok();
    }
}
