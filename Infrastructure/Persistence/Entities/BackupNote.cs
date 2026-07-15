using System.ComponentModel.DataAnnotations;

namespace NovinCRM.Infrastructure.Persistence.Entities;

public class BackupNote
{
    [Key] public string  HubSpotId           { get; set; } = null!;
    public string?       Body                { get; set; }
    public string?       AssociatedDealId    { get; set; }
    public string?       AssociatedContactId { get; set; }
    public DateTime      HsCreatedAt         { get; set; }
    public DateTime      SnapshotAt          { get; set; }
}
