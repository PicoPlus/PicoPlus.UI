#nullable enable

using NovinCRM.Domain.Events;

namespace NovinCRM.Domain.Events.Integration;

/// <summary>
/// Integration event published when a contact is registered.
/// Carries only primitive data — no domain types — so external
/// consumers can deserialise it without this assembly.
/// </summary>
public sealed record ContactRegisteredIntegrationEvent : IIntegrationEvent
{
    public Guid           EventId    { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
    public string         EventType  => "contact.registered";

    public required string ContactId    { get; init; }
    public required string FirstName    { get; init; }
    public required string LastName     { get; init; }
    public required string Phone        { get; init; }
    public           string? Email      { get; init; }
    public           string? NationalCode { get; init; }
}
