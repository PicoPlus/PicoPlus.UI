#nullable enable

using NovinCRM.Domain.Entities;

namespace NovinCRM.Application.Common.Interfaces;

/// <summary>
/// Application-layer contract for HubSpot Pipeline operations.
/// </summary>
public interface IPipelineRepository
{
    Task<IReadOnlyList<Domain.ValueObjects.Pipeline>> GetAllAsync(string objectType = "deals");
    Task<Domain.ValueObjects.PipelineStage?> GetStageByIdAsync(string objectType, string stageId);
}
