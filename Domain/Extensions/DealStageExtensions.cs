#nullable enable

using NovinCRM.Domain.Enums;

namespace NovinCRM.Domain.Extensions;

/// <summary>
/// Extension methods that encode deal-stage business rules.
/// Lives in the Domain layer because the mappings (closed, open,
/// display names, badge colours) are pure business knowledge with
/// no external dependencies.
/// </summary>
public static class DealStageExtensions
{
    private static readonly Dictionary<string, DealStage> StageMapping =
        new(StringComparer.OrdinalIgnoreCase)
        {
            // HubSpot numeric stage IDs
            ["5439468763"]              = DealStage.Started,
            ["5439468764"]              = DealStage.InProgress,
            ["5439468765"]              = DealStage.Completed,
            ["5439468766"]              = DealStage.Finalized,
            // HubSpot text stage IDs
            ["closedwon"]               = DealStage.ClosedWon,
            ["closedlost"]              = DealStage.ClosedLost
        };

    private static readonly Dictionary<DealStage, string> DisplayNames = new()
    {
        [DealStage.Started]             = "شروع شده",
        [DealStage.InProgress]          = "در حال انجام",
        [DealStage.Completed]           = "انجام شده",
        [DealStage.Finalized]           = "نهایی شده",
        [DealStage.ClosedWon]           = "بسته شده",
        [DealStage.ClosedLost]          = "از دست رفته",
        [DealStage.Unknown]             = "نامشخص"
    };

    private static readonly Dictionary<DealStage, string> BadgeClasses = new()
    {
        [DealStage.Started]             = "warning",
        [DealStage.InProgress]          = "info",
        [DealStage.Completed]           = "success",
        [DealStage.Finalized]           = "primary",
        [DealStage.ClosedWon]           = "success",
        [DealStage.ClosedLost]          = "danger",
        [DealStage.Unknown]             = "secondary"
    };

    /// <summary>
    /// Parses a HubSpot deal-stage string into the domain <see cref="DealStage"/> enum.
    /// Normalises whitespace, underscores, and hyphens before matching.
    /// </summary>
    public static DealStage ParseDealStage(this string? dealStageString)
    {
        if (string.IsNullOrWhiteSpace(dealStageString))
            return DealStage.Unknown;

        var normalized = dealStageString
            .Replace(" ", "")
            .Replace("_", "")
            .Replace("-", "");

        if (StageMapping.TryGetValue(normalized, out var stage))
            return stage;

        foreach (var kvp in StageMapping)
        {
            if (normalized.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
                return kvp.Value;
        }

        return DealStage.Unknown;
    }

    /// <summary>Returns the Persian display name for a deal stage.</summary>
    public static string GetDisplayName(this DealStage stage)
        => DisplayNames.TryGetValue(stage, out var name) ? name : "نامشخص";

    /// <summary>Returns the Bootstrap badge CSS variant for a deal stage.</summary>
    public static string GetBadgeClass(this DealStage stage)
        => BadgeClasses.TryGetValue(stage, out var css) ? css : "secondary";

    /// <summary>Returns <c>true</c> when the deal is either ClosedWon or ClosedLost.</summary>
    public static bool IsClosed(this DealStage stage)
        => stage == DealStage.ClosedWon || stage == DealStage.ClosedLost;

    /// <summary>Returns <c>true</c> when the deal is active and not Unknown.</summary>
    public static bool IsOpen(this DealStage stage)
        => !stage.IsClosed() && stage != DealStage.Unknown;
}
