#nullable enable

namespace PicoPlus.Domain.Entities;

/// <summary>
/// Domain entity representing a CRM company (business account).
/// Contains only pure business fields — no HubSpot-specific property bags.
/// </summary>
public sealed record Company
{
    /// <summary>Opaque external identifier (e.g. HubSpot company ID).</summary>
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string? Phone { get; init; }
    public string? Website { get; init; }
    public string? Email { get; init; }
    public string? Address { get; init; }
    public string? City { get; init; }
    public string? Country { get; init; }
    public string? Industry { get; init; }
    public int? NumberOfEmployees { get; init; }
    public decimal? AnnualRevenue { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}
