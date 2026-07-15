#nullable enable

using NovinCRM.Domain.Enums;

namespace NovinCRM.Domain.Entities;

/// <summary>
/// Domain entity representing a support ticket.
/// Contains only pure business fields — no HubSpot-specific property bags.
/// </summary>
public sealed record Ticket
{
    /// <summary>Opaque external identifier (e.g. HubSpot ticket ID).</summary>
    public required string Id { get; init; }
    public required string Subject { get; init; }
    public TicketStatus Status { get; init; }
    public TicketPriority Priority { get; init; }
    public string? Description { get; init; }
    public string? Category { get; init; }
    /// <summary>Opaque owner identifier (e.g. HubSpot owner ID).</summary>
    public string? OwnerId { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public DateTime? CloseDate { get; init; }
}
