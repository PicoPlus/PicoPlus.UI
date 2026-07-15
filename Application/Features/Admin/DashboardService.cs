using NovinCRM.Models.Admin;
using DashboardContactService = NovinCRM.Services.CRM.Objects.Contact;
using DashboardDealService    = NovinCRM.Services.CRM.Objects.Deal;
using PipelinesService        = NovinCRM.Services.CRM.Pipelines;

namespace NovinCRM.Services.Admin;

/// <summary>
/// Service for dashboard statistics and analytics
/// </summary>
public class DashboardService
{
    private readonly DashboardContactService _contactService;
    private readonly DashboardDealService    _dealService;
    private readonly PipelinesService        _pipelineService;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(
        DashboardContactService  contactService,
        DashboardDealService     dealService,
        PipelinesService         pipelineService,
        ILogger<DashboardService> logger)
    {
        _contactService  = contactService;
        _dealService     = dealService;
        _pipelineService = pipelineService;
        _logger          = logger;
    }

    public async Task<DashboardStatistics> GetDashboardStatisticsAsync(string? ownerId = null)
    {
        try
        {
            var stats    = new DashboardStatistics();
            var allDeals = await _dealService.GetBatchAsync(limit: 1000);

            if (!string.IsNullOrEmpty(ownerId))
                allDeals = allDeals.Where(d => PropertyHelpers.GetOwnerId(d.properties) == ownerId).ToList();

            stats.TotalDeals      = allDeals.Count;
            stats.OpenDeals       = allDeals.Count(d => d.properties?.dealstage != null &&
                                                         !d.properties.dealstage.Contains("closed"));
            stats.ClosedWonDeals  = allDeals.Count(d => d.properties?.dealstage?.Contains("closedwon") == true);
            stats.ClosedLostDeals = allDeals.Count(d => d.properties?.dealstage?.Contains("closedlost") == true);

            stats.TotalRevenue = allDeals
                .Where(d => d.properties?.amount != null)
                .Sum(d => decimal.TryParse(d.properties.amount, out var a) ? a : 0);

            var now          = DateTime.UtcNow;
            var startOfMonth = new DateTime(now.Year, now.Month, 1);

            stats.MonthlyRevenue = allDeals
                .Where(d => d.createdAt >= startOfMonth && d.properties?.amount != null)
                .Sum(d => decimal.TryParse(d.properties.amount, out var a) ? a : 0);

            stats.AverageDealValue = stats.TotalDeals > 0
                ? stats.TotalRevenue / stats.TotalDeals : 0;

            var totalWithOutcome = stats.ClosedWonDeals + stats.ClosedLostDeals;
            stats.ConversionRate = totalWithOutcome > 0
                ? (decimal)stats.ClosedWonDeals / totalWithOutcome * 100 : 0;

            await LoadPipelineStagesAsync(stats, allDeals);
            stats.TotalContacts = 0;

            _logger.LogInformation("Dashboard statistics generated for owner: {Owner}", ownerId ?? "All");
            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating dashboard statistics");
            return new DashboardStatistics();
        }
    }

    private async Task LoadPipelineStagesAsync(
        DashboardStatistics stats,
        List<NovinCRM.Models.CRM.Objects.Deal.GetBatch.Response.Result> deals)
    {
        try
        {
            var pipelinesResponse = await _pipelineService.GetAllAsync();
            if (pipelinesResponse?.results == null) return;

            var stageStats = new List<PipelineStageStats>();

            foreach (var pipeline in pipelinesResponse.results)
            {
                if (pipeline.stages == null) continue;

                foreach (var stage in pipeline.stages)
                {
                    var stageDeals = deals.Where(d => d.properties?.dealstage == stage.id).ToList();

                    stageStats.Add(new PipelineStageStats
                    {
                        StageId    = stage.id,
                        StageName  = stage.label,
                        DealCount  = stageDeals.Count,
                        TotalValue = stageDeals
                            .Where(d => d.properties?.amount != null)
                            .Sum(d => decimal.TryParse(d.properties.amount, out var a) ? a : 0),
                        Position = stage.displayOrder
                    });
                }
            }

            stats.PipelineStages = stageStats.OrderBy(s => s.Position).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading pipeline stages");
        }
    }

    public async Task<List<RecentActivity>> GetRecentActivitiesAsync(string? ownerId = null, int limit = 10)
    {
        try
        {
            var activities = new List<RecentActivity>();
            var deals      = await _dealService.GetBatchAsync(limit: 50);

            if (!string.IsNullOrEmpty(ownerId))
                deals = deals.Where(d => PropertyHelpers.GetOwnerId(d.properties) == ownerId).ToList();

            foreach (var deal in deals.Take(limit))
            {
                activities.Add(new RecentActivity
                {
                    Id           = deal.id,
                    Type         = "deal",
                    Title        = deal.properties?.dealname ?? "بدون نام",
                    Description  = $"مبلغ: {FormatCurrency(deal.properties?.amount)}",
                    Icon         = "bi-briefcase",
                    Color        = GetDealStageColor(deal.properties?.dealstage),
                    Timestamp    = deal.createdAt,
                    RelativeTime = GetRelativeTime(deal.createdAt)
                });
            }

            return activities.OrderByDescending(a => a.Timestamp).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent activities");
            return new List<RecentActivity>();
        }
    }

    private static string FormatCurrency(string? amount)
    {
        if (string.IsNullOrEmpty(amount) || !decimal.TryParse(amount, out var value))
            return "0 تومان";
        return $"{value:N0} تومان";
    }

    private static string GetDealStageColor(string? stage)
    {
        if (string.IsNullOrEmpty(stage)) return "text-secondary";
        if (stage.Contains("closedwon"))  return "text-success";
        if (stage.Contains("closedlost")) return "text-danger";
        if (stage.Contains("qualified")) return "text-primary";
        if (stage.Contains("proposal"))  return "text-info";
        return "text-warning";
    }

    private static string GetRelativeTime(DateTime dt)
    {
        var ts = DateTime.UtcNow - dt;
        if (ts.TotalMinutes < 1)  return "همین الان";
        if (ts.TotalMinutes < 60) return $"{(int)ts.TotalMinutes} دقیقه پیش";
        if (ts.TotalHours   < 24) return $"{(int)ts.TotalHours} ساعت پیش";
        if (ts.TotalDays    < 7)  return $"{(int)ts.TotalDays} روز پیش";
        if (ts.TotalDays    < 30) return $"{(int)(ts.TotalDays / 7)} هفته پیش";
        return $"{(int)(ts.TotalDays / 30)} ماه پیش";
    }
}
