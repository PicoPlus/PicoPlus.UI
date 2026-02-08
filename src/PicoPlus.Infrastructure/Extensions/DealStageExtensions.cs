#nullable enable

using PicoPlus.Application.Dto.UserPanel;

namespace PicoPlus.Extensions;

/// <summary>
/// Extension methods for deal stage parsing and display
/// </summary>
public static class DealStageExtensions
{
    private static readonly Dictionary<string, DealStage> StageMapping = new(StringComparer.OrdinalIgnoreCase)
    {
        ["closedwon"] = DealStage.ClosedWon,
        ["closedlost"] = DealStage.ClosedLost,
        ["appointmentscheduled"] = DealStage.AppointmentScheduled,
        ["qualifiedtobuy"] = DealStage.QualifiedToBuy,
        ["presentationscheduled"] = DealStage.PresentationScheduled,
        ["decisionmakerboughtin"] = DealStage.DecisionMakerBoughtIn,
        ["contractsent"] = DealStage.ContractSent
    };

    private static readonly Dictionary<DealStage, string> DisplayNames = new()
    {
        [DealStage.ClosedWon] = "???? ???",
        [DealStage.ClosedLost] = "?? ???",
        [DealStage.AppointmentScheduled] = "?? ??????",
        [DealStage.QualifiedToBuy] = "???? ?????",
        [DealStage.PresentationScheduled] = "????? ???",
        [DealStage.DecisionMakerBoughtIn] = "?? ??? ?????",
        [DealStage.ContractSent] = "??????? ????? ???",
        [DealStage.Unknown] = "??????"
    };

    private static readonly Dictionary<DealStage, string> BadgeClasses = new()
    {
        [DealStage.ClosedWon] = "success",
        [DealStage.ClosedLost] = "danger",
        [DealStage.AppointmentScheduled] = "warning",
        [DealStage.QualifiedToBuy] = "info",
        [DealStage.PresentationScheduled] = "info",
        [DealStage.DecisionMakerBoughtIn] = "primary",
        [DealStage.ContractSent] = "primary",
        [DealStage.Unknown] = "secondary"
    };

    /// <summary>
    /// Parse HubSpot deal stage string to enum
    /// </summary>
    public static DealStage ParseDealStage(this string? dealStageString)
    {
        if (string.IsNullOrWhiteSpace(dealStageString))
        {
            return DealStage.Unknown;
        }

        // Remove spaces and special characters for matching
        var normalized = dealStageString.Replace(" ", "").Replace("_", "").Replace("-", "");

        // Try exact match first
        if (StageMapping.TryGetValue(normalized, out var stage))
        {
            return stage;
        }

        // Try contains match for partial strings
        foreach (var kvp in StageMapping)
        {
            if (normalized.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
            {
                return kvp.Value;
            }
        }

        return DealStage.Unknown;
    }

    /// <summary>
    /// Get Persian display name for deal stage
    /// </summary>
    public static string GetDisplayName(this DealStage stage)
    {
        return DisplayNames.TryGetValue(stage, out var displayName) ? displayName : "??????";
    }

    /// <summary>
    /// Get Bootstrap badge class for deal stage
    /// </summary>
    public static string GetBadgeClass(this DealStage stage)
    {
        return BadgeClasses.TryGetValue(stage, out var badgeClass) ? badgeClass : "secondary";
    }

    /// <summary>
    /// Check if deal is closed (won or lost)
    /// </summary>
    public static bool IsClosed(this DealStage stage)
    {
        return stage == DealStage.ClosedWon || stage == DealStage.ClosedLost;
    }

    /// <summary>
    /// Check if deal is open (not closed)
    /// </summary>
    public static bool IsOpen(this DealStage stage)
    {
        return !stage.IsClosed() && stage != DealStage.Unknown;
    }
}
