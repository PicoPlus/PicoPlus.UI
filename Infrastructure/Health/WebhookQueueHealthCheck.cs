using Microsoft.Extensions.Diagnostics.HealthChecks;
using NovinCRM.Application.Common.Interfaces;

namespace NovinCRM.Infrastructure.Health;

/// <summary>
/// Readiness check: reports Degraded when the webhook event queue is near capacity
/// (≥ 90 % of the bounded limit of 1 000 slots).
/// A full queue means HubSpot events are being dropped — this signals a processing backlog.
/// </summary>
public sealed class WebhookQueueHealthCheck : IHealthCheck
{
    private readonly IWebhookEventQueue _queue;

    public WebhookQueueHealthCheck(IWebhookEventQueue queue) => _queue = queue;

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var depth = _queue.Count;
        var result = depth < 900
            ? HealthCheckResult.Healthy($"Queue depth: {depth}")
            : HealthCheckResult.Degraded($"Queue near capacity: {depth}/1000 — events may be dropped");

        return Task.FromResult(result);
    }
}
