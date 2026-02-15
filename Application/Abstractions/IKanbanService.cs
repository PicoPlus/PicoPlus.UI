using PicoPlus.Domain.Admin;

namespace PicoPlus.Application.Abstractions;

public interface IKanbanService
{
    Task<List<KanbanColumn>> GetKanbanBoardAsync(string? ownerId = null, string? pipelineId = null);
    Task<bool> MoveCardAsync(string dealId, string newStageId);
    Task<List<(string Id, string Name)>> GetPipelinesAsync();
}
