#nullable enable

using PicoPlus.Domain.Enums;
using PicoPlus.Domain.Events;

namespace PicoPlus.Domain.Events.Deal;

/// <summary>
/// Raised when a deal reaches a terminal stage (ClosedWon or ClosedLost).
/// Handlers may: send a deal-closed SMS notification, update analytics,
/// publish an integration event to the accounting system.
/// </summary>
public sealed record DealClosedEvent : IDomainEvent
{
    public Guid           EventId    { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;

    public required string    DealId    { get; init; }
    public required string    DealName  { get; init; }
    public required DealStage Stage     { get; init; }   // ClosedWon or ClosedLost
    public           decimal  Amount    { get; init; }
    public           string?  ContactId { get; init; }
    public           string?  ContactPhone { get; init; }
    public           string?  ContactFirstName { get; init; }
    public           string?  ContactLastName  { get; init; }
}
