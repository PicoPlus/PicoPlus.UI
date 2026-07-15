using System.ComponentModel.DataAnnotations;

namespace NovinCRM.Infrastructure.Persistence.Entities;

public class BackupContact
{
    [Key] public string   HubSpotId    { get; set; } = null!;
    public string?        FirstName    { get; set; }
    public string?        LastName     { get; set; }
    public string?        Phone        { get; set; }
    public string?        Email        { get; set; }
    public string?        NationalCode { get; set; }
    public string?        Gender       { get; set; }
    public DateTime       HsCreatedAt  { get; set; }
    public DateTime       HsUpdatedAt  { get; set; }
    /// <summary>When this row was written by the backup job.</summary>
    public DateTime       SnapshotAt   { get; set; }
}
