#nullable enable

namespace NovinCRM.Domain.Entities;

/// <summary>
/// Domain entity representing a HubSpot owner (sales rep / team member).
/// Used to assign deals, contacts, and tickets.
/// </summary>
public sealed record Owner
{
    /// <summary>Opaque external identifier (e.g. HubSpot owner ID).</summary>
    public required string Id { get; init; }
    public required string Email { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }

    public string GetFullName()
    {
        var full = $"{FirstName} {LastName}".Trim();
        return string.IsNullOrEmpty(full) ? Email : full;
    }
}
