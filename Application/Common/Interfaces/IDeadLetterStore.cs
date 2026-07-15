namespace NovinCRM.Application.Common.Interfaces;

/// <summary>
/// Entry written to the dead-letter store when a webhook event exhausts its retry budget.
/// </summary>
public sealed class DeadLetterEntry
{
    public string           EventId         { get; init; } = string.Empty;
    public string           SubscriptionType { get; init; } = string.Empty;
    public long             PortalId        { get; init; }
    public string           HandlerName     { get; init; } = string.Empty;
    public string           FailureReason   { get; init; } = string.Empty;
    public string?          LastErrorMessage { get; init; }
    public int              AttemptCount    { get; init; }
    public DateTimeOffset   FirstEnqueuedAt { get; init; }
    public DateTimeOffset   DeadLetteredAt  { get; init; }
    /// <summary>Serialized JSON of the original HubSpotWebhookEvent for re-drive.</summary>
    public string           EventPayloadJson { get; init; } = string.Empty;
}

/// <summary>
/// Persistent store for dead-lettered webhook events.
/// Implementations: <see cref="InMemoryDeadLetterStore"/> (default, single-instance),
/// or a Redis / SQL-backed store for multi-instance deployments.
/// </summary>
public interface IDeadLetterStore
{
    Task WriteAsync(DeadLetterEntry entry, CancellationToken ct = default);
    Task<IReadOnlyList<DeadLetterEntry>> GetAllAsync(int limit = 100, CancellationToken ct = default);
    Task<int> CountAsync(CancellationToken ct = default);
    Task DeleteAsync(string eventId, CancellationToken ct = default);
}
