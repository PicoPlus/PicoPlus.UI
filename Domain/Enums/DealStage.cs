#nullable enable

namespace PicoPlus.Domain.Enums;

/// <summary>
/// Strongly-typed enumeration for deal pipeline stages.
/// Maps 1-to-1 with HubSpot deal stage identifiers;
/// the mapping itself lives in DealStageExtensions (Domain layer).
/// </summary>
public enum DealStage
{
    Unknown,
    Started,              // 5439468763
    InProgress,           // 5439468764
    Completed,            // 5439468765
    Finalized,            // 5439468766
    ClosedWon,            // closedwon
    ClosedLost            // closedlost
}
