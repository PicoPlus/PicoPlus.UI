#nullable enable

namespace NovinCRM.Domain.Entities;

/// <summary>
/// Domain entity representing an engagement note attached to a CRM object.
/// </summary>
public sealed record Note
{
    /// <summary>Opaque external identifier (e.g. HubSpot note ID).</summary>
    public required string Id { get; init; }
    public string? Body { get; init; }
    public string? AttachmentIds { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}
