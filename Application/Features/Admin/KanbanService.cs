using NovinCRM.Models.Admin;
using DealSvc          = NovinCRM.Services.CRM.Objects.Deal;
using ContactSvc       = NovinCRM.Services.CRM.Objects.Contact;
using PipelinesService = NovinCRM.Services.CRM.Pipelines;

namespace NovinCRM.Services.Admin;

/// <summary>
/// Service for Kanban board operations
/// </summary>
public class KanbanService
{
    private readonly DealSvc          _dealService;
    private readonly PipelinesService _pipelineService;
    private readonly ContactSvc       _contactService;
    private readonly ILogger<KanbanService> _logger;

    public KanbanService(
        DealSvc          dealService,
        PipelinesService pipelineService,
        ContactSvc       contactService,
        ILogger<KanbanService> logger)
    {
        _dealService     = dealService;
        _pipelineService = pipelineService;
        _contactService  = contactService;
        _logger          = logger;
    }

    public async Task<List<KanbanColumn>> GetKanbanBoardAsync(string? ownerId = null, string? pipelineId = null)
    {
        try
        {
            var pipelinesResponse = await _pipelineService.GetAllAsync();
            if (pipelinesResponse?.results == null || !pipelinesResponse.results.Any())
            {
                _logger.LogWarning("No pipelines found");
                return new List<KanbanColumn>();
            }

            var pipeline = string.IsNullOrEmpty(pipelineId)
                ? pipelinesResponse.results.FirstOrDefault()
                : pipelinesResponse.results.FirstOrDefault(p => p.id == pipelineId);

            if (pipeline?.stages == null)
            {
                _logger.LogWarning("No stages found in pipeline");
                return new List<KanbanColumn>();
            }

            var deals = await _dealService.GetBatchAsync(limit: 1000);

            if (!string.IsNullOrEmpty(ownerId))
                deals = deals.Where(d => PropertyHelpers.GetOwnerId(d.properties) == ownerId).ToList();

            var columns = new List<KanbanColumn>();

            foreach (var stage in pipeline.stages.OrderBy(s => s.displayOrder))
            {
                var stageDeals = deals.Where(d => d.properties?.dealstage == stage.id).ToList();

                columns.Add(new KanbanColumn
                {
                    Id       = stage.id,
                    Name     = stage.label,
                    Color    = GetStageColor(stage.label),
                    Position = stage.displayOrder,
                    Cards    = stageDeals.Select(MapDealToCard).ToList()
                });
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

    public async Task<bool> MoveCardAsync(string dealId, string newStageId)
    {
        try
        {
            return await _dealService.UpdateStageAsync(dealId, newStageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error moving card {DealId} to stage {StageId}", dealId, newStageId);
            return false;
        }
    }

    private KanbanCard MapDealToCard(NovinCRM.Models.CRM.Objects.Deal.GetBatch.Response.Result deal)
    {
        var amount = decimal.TryParse(deal.properties?.amount, out var amt) ? amt : (decimal?)null;

        return new KanbanCard
        {
            Id          = deal.id,
            Title       = deal.properties?.dealname ?? "بدون نام",
            Description = PropertyHelpers.GetDescription(deal.properties),
            Amount      = amount,
            CloseDate   = null,
            ContactName = PropertyHelpers.GetContactName(deal.properties),
            CompanyName = PropertyHelpers.GetCompanyName(deal.properties),
            Priority    = GetPriority(amount),
            OwnerId     = PropertyHelpers.GetOwnerId(deal.properties),
            OwnerName   = "",
            CreatedAt   = deal.createdAt,
            UpdatedAt   = deal.updatedAt,
            Tags        = GetTags(deal)
        };
    }

    private static string GetPriority(decimal? amount)
    {
        if (amount > 50_000_000) return "high";
        if (amount > 10_000_000) return "medium";
        return "low";
    }

    private static List<string> GetTags(NovinCRM.Models.CRM.Objects.Deal.GetBatch.Response.Result deal)
    {
        var tags = new List<string>();
        if (deal.properties?.amount != null && decimal.TryParse(deal.properties.amount, out var amount))
        {
            if (amount > 50_000_000) tags.Add("ارزش بالا");
            else if (amount < 5_000_000) tags.Add("ارزش پایین");
        }
        return tags;
    }

    private static string GetStageColor(string stageName)
    {
        var name = stageName.ToLower();
        if (name.Contains("qualified"))   return "#0d6efd";
        if (name.Contains("proposal"))    return "#6f42c1";
        if (name.Contains("negotiation")) return "#fd7e14";
        if (name.Contains("closed won"))  return "#198754";
        if (name.Contains("closed lost")) return "#dc3545";
        return "#6c757d";
    }

    public async Task<List<(string Id, string Name)>> GetPipelinesAsync()
    {
        try
        {
            var response = await _pipelineService.GetAllAsync();
            if (response?.results == null) return new List<(string, string)>();
            return response.results.Select(p => (p.id, p.label)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pipelines");
            return new List<(string, string)>();
        }
    }
}
