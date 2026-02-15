using PicoPlus.Domain.Admin;
using PicoPlus.Services.CRM.Objects;
using PicoPlus.Application.Abstractions;
using PicoPlus.Services.CRM;

namespace PicoPlus.Infrastructure.Admin;

/// <summary>
/// Service for Kanban board operations
/// </summary>
public class KanbanService : IKanbanService
{
    private readonly Deal _dealService;
    private readonly Pipelines _pipelineService;
    private readonly Contact _contactService;
    private readonly ILogger<KanbanService> _logger;

    public KanbanService(
        Deal dealService,
        Pipelines pipelineService,
        Contact contactService,
        ILogger<KanbanService> logger)
    {
        _dealService = dealService;
        _pipelineService = pipelineService;
        _contactService = contactService;
        _logger = logger;
    }

    /// <summary>
    /// Get Kanban board data
    /// </summary>
    public async Task<List<KanbanColumn>> GetKanbanBoardAsync(string? ownerId = null, string? pipelineId = null)
    {
        try
        {
            // Get pipeline stages using extension method
            var pipelinesResponse = await _pipelineService.GetAllAsync();
            if (pipelinesResponse?.results == null || !pipelinesResponse.results.Any())
            {
                _logger.LogWarning("No pipelines found");
                return new List<KanbanColumn>();
            }

            // Select first pipeline or specified one
            var pipeline = string.IsNullOrEmpty(pipelineId)
                ? pipelinesResponse.results.FirstOrDefault()
                : pipelinesResponse.results.FirstOrDefault(p => p.id == pipelineId);

            if (pipeline?.stages == null)
            {
                _logger.LogWarning("No stages found in pipeline");
                return new List<KanbanColumn>();
            }

            // Get all deals using extension method
            var deals = await _dealService.GetBatchAsync(limit: 1000);

            // Filter by owner if specified
            if (!string.IsNullOrEmpty(ownerId))
            {
                deals = deals.Where(d => PropertyHelpers.GetOwnerId(d.properties) == ownerId).ToList();
            }

            // Create columns from stages
            var columns = new List<KanbanColumn>();

            foreach (var stage in pipeline.stages.OrderBy(s => s.displayOrder))
            {
                var stageDeals = deals.Where(d => d.properties?.dealstage == stage.id).ToList();

                var column = new KanbanColumn
                {
                    Id = stage.id,
                    Name = stage.label,
                    Color = GetStageColor(stage.label),
                    Position = stage.displayOrder,
                    Cards = stageDeals.Select(d => MapDealToCard(d)).ToList()
                };

                columns.Add(column);
            }

            _logger.LogInformation("Kanban board loaded with {Count} columns", columns.Count);
            return columns;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading Kanban board");
            return new List<KanbanColumn>();
        }
    }

    /// <summary>
    /// Move card to different stage
    /// </summary>
    public async Task<bool> MoveCardAsync(string dealId, string newStageId)
    {
        try
        {
            // Update deal stage using extension method
            return await _dealService.UpdateStageAsync(dealId, newStageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error moving card {DealId} to stage {StageId}", dealId, newStageId);
            return false;
        }
    }

    /// <summary>
    /// Map deal to Kanban card
    /// </summary>
    private KanbanCard MapDealToCard(PicoPlus.Models.CRM.Objects.Deal.GetBatch.Response.Result deal)
    {
        var amount = decimal.TryParse(deal.properties?.amount, out var amt) ? amt : (decimal?)null;

        return new KanbanCard
        {
            Id = deal.id,
            Title = deal.properties?.dealname ?? "?????? ???? ???",
            Description = PropertyHelpers.GetDescription(deal.properties),
            Amount = amount,
            CloseDate = null,
            ContactName = PropertyHelpers.GetContactName(deal.properties),
            CompanyName = PropertyHelpers.GetCompanyName(deal.properties),
            Priority = GetPriority(null, amount),
            OwnerId = PropertyHelpers.GetOwnerId(deal.properties),
            OwnerName = "",
            CreatedAt = deal.createdAt,
            UpdatedAt = deal.updatedAt,
            Tags = GetTags(deal)
        };
    }

    /// <summary>
    /// Get priority based on close date and amount
    /// </summary>
    private string GetPriority(DateTime? closeDate, decimal? amount)
    {
        if (amount > 50_000_000) return "high";
        if (amount > 10_000_000) return "medium";
        return "low";
    }

    /// <summary>
    /// Get tags for deal
    /// </summary>
    private List<string> GetTags(PicoPlus.Models.CRM.Objects.Deal.GetBatch.Response.Result deal)
    {
        var tags = new List<string>();

        if (deal.properties?.amount != null && decimal.TryParse(deal.properties.amount, out var amount))
        {
            if (amount > 50_000_000) tags.Add("??? ????");
            else if (amount < 5_000_000) tags.Add("??? ?????");
        }

        return tags;
    }

    /// <summary>
    /// Get color for stage
    /// </summary>
    private string GetStageColor(string stageName)
    {
        var name = stageName.ToLower();

        if (name.Contains("qualified") || name.Contains("???? ?????")) return "#0d6efd";
        if (name.Contains("proposal") || name.Contains("???????")) return "#6f42c1";
        if (name.Contains("negotiation") || name.Contains("??????")) return "#fd7e14";
        if (name.Contains("closed won") || name.Contains("????")) return "#198754";
        if (name.Contains("closed lost") || name.Contains("??????")) return "#dc3545";
        
        return "#6c757d";
    }

    /// <summary>
    /// Get available pipelines
    /// </summary>
    public async Task<List<(string Id, string Name)>> GetPipelinesAsync()
    {
        try
        {
            var response = await _pipelineService.GetAllAsync();
            if (response?.results == null) return new List<(string, string)>();

            return response.results
                .Select(p => (p.id, p.label))
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pipelines");
            return new List<(string, string)>();
        }
    }
}
