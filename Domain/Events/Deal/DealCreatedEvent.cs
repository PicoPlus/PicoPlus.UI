#nullable enable

using NovinCRM.Domain.Enums;
using NovinCRM.Domain.Events;

namespace NovinCRM.Domain.Events.Deal;

/// <summary>
/// Raised when a new deal is created in the CRM.
/// Handlers may: publish an integration event, update contact statistics cache,
/// trigger a welcome notification, start an automated workflow.
/// </summary>
public sealed record DealCreatedEvent : IDomainEvent
{
    public Guid           EventId    { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;

    public required string    DealId     { get; init; }
    public required string    DealName   { get; init; }
    public required DealStage Stage      { get; init; }
    public           decimal  Amount     { get; init; }
    public           string?  Pipeline   { get; init; }
    /// <summary>Contact that owns this deal (may be null for admin-created deals).</summary>
    public           string?  ContactId  { get; init; }
}
