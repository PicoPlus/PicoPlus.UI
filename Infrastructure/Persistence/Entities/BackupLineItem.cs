using System.ComponentModel.DataAnnotations;

namespace NovinCRM.Infrastructure.Persistence.Entities;

public class BackupLineItem
{
    [Key] public string  HubSpotId          { get; set; } = null!;
    public string?       DealId             { get; set; }
    public string?       Name               { get; set; }
    public decimal       Price              { get; set; }
    public long          Quantity           { get; set; }
    public decimal       DiscountPercentage { get; set; }
    public string?       Sku                { get; set; }
    public DateTime      SnapshotAt         { get; set; }
}
