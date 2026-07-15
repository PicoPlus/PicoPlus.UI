#nullable enable

using NovinCRM.Domain.Events;

namespace NovinCRM.Domain.Events.Integration;

/// <summary>
/// Integration event published when a new deal is created.
/// </summary>
public sealed record DealCreatedIntegrationEvent : IIntegrationEvent
{
    public Guid           EventId    { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
    public string         EventType  => "deal.created";

    public required string DealId    { get; init; }
    public required string DealName  { get; init; }
    public           decimal Amount  { get; init; }
    public           string? Pipeline { get; init; }
    public           string? ContactId { get; init; }
}
