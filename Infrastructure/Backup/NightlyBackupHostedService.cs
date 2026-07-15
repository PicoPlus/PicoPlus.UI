#nullable enable

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using NovinCRM.Application.Common.Interfaces;
using NovinCRM.Infrastructure.Persistence;
using NovinCRM.Infrastructure.Persistence.Entities;

namespace NovinCRM.Infrastructure.Backup;

/// <summary>
/// Fires every night at 00:00, runs EF migrations, enables maintenance mode,
/// executes the HubSpot backup, then disables maintenance by 00:30 (hard cutoff).
/// </summary>
public sealed class NightlyBackupHostedService : BackgroundService
{
    private readonly IServiceScopeFactory           _scopeFactory;
    private readonly IMaintenanceModeService        _maintenance;
    private readonly IConfiguration                 _config;
    private readonly ILogger<NightlyBackupHostedService> _logger;

    public NightlyBackupHostedService(
        IServiceScopeFactory                 scopeFactory,
        IMaintenanceModeService              maintenance,
        IConfiguration                       config,
        ILogger<NightlyBackupHostedService>  logger)
    {
        _scopeFactory = scopeFactory;
        _maintenance  = maintenance;
        _config       = config;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Skip backup if not enabled
        if (!bool.TryParse(_config["Backup:Enabled"], out var enabled) || !enabled)
        {
            _logger.LogInformation("NightlyBackupHostedService: disabled via Backup:Enabled — exiting");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = DelayUntilNextRun();
            _logger.LogInformation("NightlyBackupHostedService: next run in {Delay}", delay);

            try { await Task.Delay(delay, stoppingToken); }
            catch (TaskCanceledException) { return; }

            await RunWindowAsync(stoppingToken);
        }
    }

    private async Task RunWindowAsync(CancellationToken stoppingToken)
    {
        var hardDeadline = int.TryParse(_config["Backup:HardDeadlineMinutes"], out var hd) ? hd : 28;
        var date         = DateOnly.FromDateTime(DateTime.Today);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        cts.CancelAfter(TimeSpan.FromMinutes(hardDeadline));

        BackupRun? run = null;

        try
        {
            // ── Step 1: Migrate ───────────────────────────────────────────────
            _logger.LogInformation("NightlyBackupHostedService: applying EF migrations");
            await using (var scope = _scopeFactory.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<NovinBackupDbContext>();
                await db.Database.MigrateAsync(stoppingToken);
            }

            // ── Step 2: Maintenance ON ────────────────────────────────────────
            var estimatedEnd = DateTime.Today.AddMinutes(30);
            _maintenance.Enable("پشتیبان‌گیری شبانه در حال اجراست", estimatedEnd);
            _logger.LogInformation("NightlyBackupHostedService: maintenance mode ON");

            // ── Step 3: Backup ────────────────────────────────────────────────
            await using (var scope = _scopeFactory.CreateAsyncScope())
            {
                var db      = scope.ServiceProvider.GetRequiredService<NovinBackupDbContext>();
                var backup  = scope.ServiceProvider.GetRequiredService<IHubSpotBackupService>();

                run = new BackupRun { StartedAt = DateTime.UtcNow, Status = BackupRunStatus.Running };
                db.BackupRuns.Add(run);
                await db.SaveChangesAsync(stoppingToken);

                BackupResult result;
                try
                {
                    result = await backup.RunDailyBackupAsync(date, cts.Token);
                }
                catch (OperationCanceledException)
                {
                    run.Status       = BackupRunStatus.PartialSuccess;
                    run.ErrorMessage = "Backup exceeded hard deadline and was cancelled.";
                    run.FinishedAt   = DateTime.UtcNow;
                    await db.SaveChangesAsync(stoppingToken);
                    _logger.LogWarning("NightlyBackupHostedService: backup cancelled at hard deadline");
                    return;
                }

                run.Status               = result.Success ? BackupRunStatus.Completed : BackupRunStatus.Failed;
                run.ContactsUpserted     = result.ContactsUpserted;
                run.DealsUpserted        = result.DealsUpserted;
                run.LineItemsUpserted    = result.LineItemsUpserted;
                run.AssociationsUpserted = result.AssociationsUpserted;
                run.NotesUpserted        = result.NotesUpserted;
                run.ErrorMessage         = result.Error;
                run.FinishedAt           = DateTime.UtcNow;
                await db.SaveChangesAsync(stoppingToken);

                _logger.LogInformation(
                    "NightlyBackupHostedService: backup completed — contacts={C} deals={D} lineItems={LI}",
                    result.ContactsUpserted, result.DealsUpserted, result.LineItemsUpserted);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "NightlyBackupHostedService: unexpected error");
        }
        finally
        {
            // ── Step 4: Maintenance OFF (always, hard cutoff) ─────────────────
            _maintenance.Disable();
            _logger.LogInformation("NightlyBackupHostedService: maintenance mode OFF");
        }
    }

    /// <summary>Returns how long to wait before 00:00 tonight (or tomorrow if past 00:30).</summary>
    private static TimeSpan DelayUntilNextRun()
    {
        var now  = DateTime.Now;
        // If we're in the 00:00–00:30 window (unlikely on startup), push to next night
        var next = now < DateTime.Today.AddMinutes(30)
            ? DateTime.Today.AddDays(1)
            : DateTime.Today.AddDays(1);
        return next - now;
    }
}
