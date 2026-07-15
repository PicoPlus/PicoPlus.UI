#nullable enable

using NovinCRM.Domain.Events;

namespace NovinCRM.Domain.Events.Contact;

/// <summary>
/// Raised when a new contact (customer) is created during registration.
/// Handlers may: send welcome SMS, notify CRM enrichment pipeline,
/// publish an integration event to external systems.
/// </summary>
public sealed record ContactRegisteredEvent : IDomainEvent
{
    public Guid           EventId     { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt  { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Opaque external identifier assigned by the CRM (e.g. HubSpot contact ID).</summary>
    public required string ContactId   { get; init; }
    public required string FirstName   { get; init; }
    public required string LastName    { get; init; }
    public required string Phone       { get; init; }
    public           string? Email     { get; init; }
    public           string? NationalCode { get; init; }
    /// <summary>Shahkar phone-ownership verification status at time of registration.</summary>
    public           string? ShahkarStatus { get; init; }
}
