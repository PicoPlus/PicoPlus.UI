#nullable enable

namespace NovinCRM.Domain.Entities;

/// <summary>
/// Domain entity representing a single line item within a deal.
/// Contains only pure business fields — no HubSpot-specific property bags.
/// </summary>
public sealed record LineItem
{
    /// <summary>Opaque external identifier (e.g. HubSpot line item ID).</summary>
    public required string Id { get; init; }
    public required string Name { get; init; }
    public decimal Price { get; init; }
    public long Quantity { get; init; }
    /// <summary>Discount percentage (0–100).</summary>
    public decimal DiscountPercentage { get; init; }
    /// <summary>Calculated total: Price × Quantity × (1 − Discount/100).</summary>
    public decimal TotalAmount => Price * Quantity * (1 - DiscountPercentage / 100);
    /// <summary>Opaque product identifier (e.g. HubSpot product ID).</summary>
    public string? ProductId { get; init; }
    public string? Sku { get; init; }
    public string? RecurringBillingFrequency { get; init; }
    public string? RecurringBillingPeriod { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}
