#nullable enable

namespace PicoPlus.State.UserPanel;

/// <summary>
/// Immutable DTO for deal summary information
/// </summary>
public sealed record DealSummary
{
    public required string Id { get; init; }
    public required string DealName { get; init; }
    public decimal Amount { get; init; }
    public DealStage Stage { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public DateTime? CloseDate { get; init; }
    public string? Pipeline { get; init; }
}
