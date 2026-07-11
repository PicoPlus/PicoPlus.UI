#nullable enable

using PicoPlus.Domain.Events;

namespace PicoPlus.Domain.Events.Integration;

/// <summary>
/// Integration event published when a deal reaches a terminal stage.
/// </summary>
public sealed record DealClosedIntegrationEvent : IIntegrationEvent
{
    public Guid           EventId    { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
    public string         EventType  => "deal.closed";

    public required string DealId             { get; init; }
    public required string DealName           { get; init; }
    public required string Stage              { get; init; }   // "ClosedWon" | "ClosedLost"
    public           decimal Amount           { get; init; }
    public           string? ContactId        { get; init; }
    public           string? ContactPhone     { get; init; }
    public           string? ContactFirstName { get; init; }
    public           string? ContactLastName  { get; init; }
}
