#nullable enable

using PicoPlus.Domain.Events;

namespace PicoPlus.Domain.Events.Contact;

/// <summary>
/// Raised when a contact's profile data is enriched from an external
/// identity service (Zohal national identity / Shahkar).
/// Handlers may: invalidate user-panel cache, notify compliance systems.
/// </summary>
public sealed record ContactEnrichedEvent : IDomainEvent
{
    public Guid           EventId    { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;

    public required string ContactId    { get; init; }
    /// <summary>Fields that were updated, for targeted cache invalidation.</summary>
    public required IReadOnlyList<string> UpdatedFields { get; init; }
    public           string? ShahkarStatus { get; init; }
}
