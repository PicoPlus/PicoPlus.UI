using System.ComponentModel.DataAnnotations;

namespace NovinCRM.Infrastructure.Persistence.Entities;

public enum BackupRunStatus { Running, Completed, Failed, PartialSuccess }

/// <summary>Audit row — one per nightly backup execution.</summary>
public class BackupRun
{
    [Key] public int           Id                   { get; set; }
    public DateTime            StartedAt            { get; set; }
    public DateTime?           FinishedAt           { get; set; }
    public BackupRunStatus     Status               { get; set; }
    public int                 ContactsUpserted     { get; set; }
    public int                 DealsUpserted        { get; set; }
    public int                 LineItemsUpserted    { get; set; }
    public int                 AssociationsUpserted { get; set; }
    public int                 NotesUpserted        { get; set; }
    public string?             ErrorMessage         { get; set; }
}
