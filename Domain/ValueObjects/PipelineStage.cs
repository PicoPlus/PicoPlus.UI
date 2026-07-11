#nullable enable

namespace PicoPlus.Domain.ValueObjects;

/// <summary>
/// Immutable value object representing a single stage within a pipeline.
/// <see cref="StageId"/> is the opaque external key (e.g. HubSpot stage ID).
/// </summary>
public sealed record PipelineStage
{
    public required string StageId { get; init; }
    public required string Label { get; init; }
    /// <summary>Display order within the pipeline (lower = earlier).</summary>
    public int DisplayOrder { get; init; }
    /// <summary>Win probability 0–100, if provided by the external system.</summary>
    public decimal? Probability { get; init; }
    /// <summary>True when this stage represents a closed (terminal) state.</summary>
    public bool IsClosed { get; init; }
}
