using System.ComponentModel.DataAnnotations;

namespace NovinCRM.Infrastructure.Persistence.Entities;

public class BackupDeal
{
    [Key] public string   HubSpotId  { get; set; } = null!;
    public string?        DealName   { get; set; }
    public decimal        Amount     { get; set; }
    public string?        Stage      { get; set; }
    public string?        Pipeline   { get; set; }
    public DateTime       HsCreatedAt { get; set; }
    public DateTime?      HsCloseDate { get; set; }
    public DateTime       SnapshotAt  { get; set; }
}
