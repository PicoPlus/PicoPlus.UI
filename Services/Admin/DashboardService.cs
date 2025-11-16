using PicoPlus.Models.Admin;
using ContactService = PicoPlus.Services.CRM.Objects.Contact;
using DealService = PicoPlus.Services.CRM.Objects.Deal;
using PicoPlus.Services.CRM;

namespace PicoPlus.Services.Admin;

/// <summary>
/// Service for dashboard statistics and analytics
/// </summary>
public class DashboardService
{
    private readonly ContactService _contactService;
    private readonly DealService _dealService;
    private readonly Pipelines _pipelineService;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(
        ContactService contactService,
        DealService dealService,
        Pipelines pipelineService,
        ILogger<DashboardService> logger)
    {
        _contactService = contactService;
        _dealService = dealService;
        _pipelineService = pipelineService;
        _logger = logger;
    }

    /// <summary>
    /// Get comprehensive dashboard statistics
    /// </summary>
    public async Task<DashboardStatistics> GetDashboardStatisticsAsync(string? ownerId = null)
    {
        try
        {
            var stats = new DashboardStatistics();

            // Get all deals using extension method
            var allDeals = await _dealService.GetBatchAsync(limit: 1000);

            // Filter by owner if specified (using helper)
            if (!string.IsNullOrEmpty(ownerId))
            {
                allDeals = allDeals.Where(d => PropertyHelpers.GetOwnerId(d.properties) == ownerId).ToList();
            }

            // Deal statistics
            stats.TotalDeals = allDeals.Count;
            stats.OpenDeals = allDeals.Count(d => d.properties?.dealstage != null && 
                                                   !d.properties.dealstage.Contains("closed"));
            stats.ClosedWonDeals = allDeals.Count(d => d.properties?.dealstage?.Contains("closedwon") == true);
            stats.ClosedLostDeals = allDeals.Count(d => d.properties?.dealstage?.Contains("closedlost") == true);

            // Revenue calculations
            stats.TotalRevenue = allDeals
                .Where(d => d.properties?.amount != null)
                .Sum(d => decimal.TryParse(d.properties.amount, out var amount) ? amount : 0);

            var now = DateTime.UtcNow;
            var startOfMonth = new DateTime(now.Year, now.Month, 1);
            
            stats.MonthlyRevenue = allDeals
                .Where(d => d.createdAt >= startOfMonth && d.properties?.amount != null)
                .Sum(d => decimal.TryParse(d.properties.amount, out var amount) ? amount : 0);

            stats.AverageDealValue = stats.TotalDeals > 0 
                ? stats.TotalRevenue / stats.TotalDeals 
                : 0;

            // Conversion rate
            var totalDealsWithOutcome = stats.ClosedWonDeals + stats.ClosedLostDeals;
            stats.ConversionRate = totalDealsWithOutcome > 0 
                ? (decimal)stats.ClosedWonDeals / totalDealsWithOutcome * 100 
                : 0;

            // Pipeline stage statistics
            await LoadPipelineStagesAsync(stats, allDeals);

            // Contact statistics
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

    /// <summary>
    /// Load pipeline stage statistics
    /// </summary>
    private async Task LoadPipelineStagesAsync(DashboardStatistics stats, 
        List<PicoPlus.Models.CRM.Objects.Deal.GetBatch.Response.Result> deals)
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
                        StageId = stage.id,
                        StageName = stage.label,
                        DealCount = stageDeals.Count,
                        TotalValue = stageDeals
                            .Where(d => d.properties?.amount != null)
                            .Sum(d => decimal.TryParse(d.properties.amount, out var amount) ? amount : 0),
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

    /// <summary>
    /// Get recent activities
    /// </summary>
    public async Task<List<RecentActivity>> GetRecentActivitiesAsync(string? ownerId = null, int limit = 10)
    {
        try
        {
            var activities = new List<RecentActivity>();
            
            var deals = await _dealService.GetBatchAsync(limit: 50);

            if (!string.IsNullOrEmpty(ownerId))
            {
                deals = deals.Where(d => PropertyHelpers.GetOwnerId(d.properties) == ownerId).ToList();
            }

            foreach (var deal in deals.Take(limit))
            {
                var activity = new RecentActivity
                {
                    Id = deal.id,
                    Type = "deal",
                    Title = deal.properties?.dealname ?? "?????? ???? ???",
                    Description = $"????: {FormatCurrency(deal.properties?.amount)}",
                    Icon = "bi-briefcase",
                    Color = GetDealStageColor(deal.properties?.dealstage),
                    Timestamp = deal.createdAt,
                    RelativeTime = GetRelativeTime(deal.createdAt)
                };

                activities.Add(activity);
            }

            return activities.OrderByDescending(a => a.Timestamp).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent activities");
            return new List<RecentActivity>();
        }
    }

    private string FormatCurrency(string? amount)
    {
        if (string.IsNullOrEmpty(amount) || !decimal.TryParse(amount, out var value))
            return "0 ?????";

        return $"{value:N0} ?????";
    }

    private string GetDealStageColor(string? stage)
    {
        if (string.IsNullOrEmpty(stage)) return "text-secondary";
        
        if (stage.Contains("closedwon")) return "text-success";
        if (stage.Contains("closedlost")) return "text-danger";
        if (stage.Contains("qualified")) return "text-primary";
        if (stage.Contains("proposal")) return "text-info";
        
        return "text-warning";
    }

    private string GetRelativeTime(DateTime dateTime)
    {
        var timeSpan = DateTime.UtcNow - dateTime;

        if (timeSpan.TotalMinutes < 1) return "???? ????";
        if (timeSpan.TotalMinutes < 60) return $"{(int)timeSpan.TotalMinutes} ????? ???";
        if (timeSpan.TotalHours < 24) return $"{(int)timeSpan.TotalHours} ???? ???";
        if (timeSpan.TotalDays < 7) return $"{(int)timeSpan.TotalDays} ??? ???";
        if (timeSpan.TotalDays < 30) return $"{(int)(timeSpan.TotalDays / 7)} ???? ???";
        
        return $"{(int)(timeSpan.TotalDays / 30)} ??? ???";
    }
}
