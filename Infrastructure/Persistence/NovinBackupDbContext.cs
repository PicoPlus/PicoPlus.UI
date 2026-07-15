#nullable enable

using Microsoft.EntityFrameworkCore;
using NovinCRM.Infrastructure.Persistence.Entities;

namespace NovinCRM.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext for nightly HubSpot → SQL Server backup snapshots.
/// Connection string: ConnectionStrings:BackupDb in appsettings / env.
/// </summary>
public class NovinBackupDbContext : DbContext
{
    public NovinBackupDbContext(DbContextOptions<NovinBackupDbContext> options)
        : base(options) { }

    public DbSet<BackupContact>     Contacts     => Set<BackupContact>();
    public DbSet<BackupDeal>        Deals        => Set<BackupDeal>();
    public DbSet<BackupLineItem>    LineItems    => Set<BackupLineItem>();
    public DbSet<BackupAssociation> Associations => Set<BackupAssociation>();
    public DbSet<BackupNote>        Notes        => Set<BackupNote>();
    public DbSet<BackupRun>         BackupRuns   => Set<BackupRun>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Composite unique index on associations — no duplicates
        modelBuilder.Entity<BackupAssociation>()
            .HasIndex(a => new { a.FromObjectType, a.FromId, a.ToObjectType, a.ToId })
            .IsUnique();

        base.OnModelCreating(modelBuilder);
    }
}
