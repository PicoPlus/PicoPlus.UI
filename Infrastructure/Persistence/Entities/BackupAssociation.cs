using System.ComponentModel.DataAnnotations;

namespace NovinCRM.Infrastructure.Persistence.Entities;

public class BackupAssociation
{
    [Key] public int    Id             { get; set; }
    public string       FromObjectType { get; set; } = null!;
    public string       FromId         { get; set; } = null!;
    public string       ToObjectType   { get; set; } = null!;
    public string       ToId           { get; set; } = null!;
    public DateTime     SnapshotAt     { get; set; }
}
