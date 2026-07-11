#nullable enable

using PicoPlus.Domain.Enums;
using PicoPlus.Domain.Events;

namespace PicoPlus.Domain.Events.Deal;

/// <summary>
/// Raised when a deal moves from one pipeline stage to another.
/// Handlers may: update Kanban board state, trigger conditional automation.
/// </summary>
public sealed record DealStageChangedEvent : IDomainEvent
{
    public Guid           EventId      { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt   { get; init; } = DateTimeOffset.UtcNow;

    public required string    DealId      { get; init; }
    public required DealStage PreviousStage { get; init; }
    public required DealStage NewStage      { get; init; }
    public           string?  ContactId     { get; init; }
}
