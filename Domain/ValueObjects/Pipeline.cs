#nullable enable

namespace NovinCRM.Domain.ValueObjects;

/// <summary>
/// Immutable value object representing a deal pipeline.
/// Carries the ordered list of stages that deals progress through.
/// </summary>
public sealed record Pipeline
{
    public required string PipelineId { get; init; }
    public required string Label { get; init; }
    public int DisplayOrder { get; init; }
    public IReadOnlyList<PipelineStage> Stages { get; init; } = Array.Empty<PipelineStage>();
}
