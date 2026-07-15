#nullable enable

using NovinCRM.Domain.Events;

namespace NovinCRM.Domain.Events.Deal;

/// <summary>
/// Raised after an invoice-review SMS has been dispatched to the customer.
/// </summary>
public sealed record InvoiceSentEvent : IDomainEvent
{
    public Guid           EventId     { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt  { get; init; } = DateTimeOffset.UtcNow;

    public required string DealId       { get; init; }
    public required string ContactId    { get; init; }
    public required string InvoiceLink  { get; init; }
}
