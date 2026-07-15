#nullable enable

using NovinCRM.Application.Common.Interfaces;
using NovinCRM.Domain.ValueObjects;

namespace NovinCRM.Services.CRM;

/// <summary>
/// Implements IPipelineRepository by delegating to the existing Pipelines service.
/// </summary>
public class PipelineRepository : IPipelineRepository
{
    private readonly Pipelines _pipelines;
    public PipelineRepository(Pipelines pipelines) => _pipelines = pipelines;

    public async Task<IReadOnlyList<Pipeline>> GetAllAsync(string objectType = "deals")
    {
        var resp = await _pipelines.GetPipelines(objectType);
        return resp?.results?.Select(p => new Pipeline
        {
            PipelineId   = p.id ?? string.Empty,
            Label        = p.label ?? string.Empty,
            DisplayOrder = p.displayOrder,
            Stages       = p.stages?.Select(s => new PipelineStage
            {
                StageId      = s.id ?? string.Empty,
                Label        = s.label ?? string.Empty,
                DisplayOrder = s.displayOrder,
                IsClosed     = bool.TryParse(s.metadata?.isClosed, out var c) && c
            }).ToList() ?? (IReadOnlyList<PipelineStage>)Array.Empty<PipelineStage>()
        }).ToList() ?? (IReadOnlyList<Pipeline>)Array.Empty<Pipeline>();
    }

    public async Task<PipelineStage?> GetStageByIdAsync(string objectType, string stageId)
    {
        var all = await GetAllAsync(objectType);
        return all.SelectMany(p => p.Stages).FirstOrDefault(s => s.StageId == stageId);
    }
}
