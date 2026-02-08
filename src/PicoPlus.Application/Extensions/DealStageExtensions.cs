#nullable enable

using PicoPlus.Application.Dto.UserPanel;

namespace PicoPlus.Application.Extensions;

/// <summary>
/// Extension methods for deal stage parsing and display.
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

    public static DealStage ParseDealStage(this string? dealStageString)
    {
        if (string.IsNullOrWhiteSpace(dealStageString))
        {
            return DealStage.Unknown;
        }

        var normalized = dealStageString.Replace(" ", "").Replace("_", "").Replace("-", "");

        if (StageMapping.TryGetValue(normalized, out var stage))
        {
            return stage;
        }

        foreach (var kvp in StageMapping)
        {
            if (normalized.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
            {
                return kvp.Value;
            }
        }

        return DealStage.Unknown;
    }

    public static string GetDisplayName(this DealStage stage)
        => DisplayNames.TryGetValue(stage, out var displayName) ? displayName : "??????";

    public static string GetBadgeClass(this DealStage stage)
        => BadgeClasses.TryGetValue(stage, out var badgeClass) ? badgeClass : "secondary";

    public static bool IsClosed(this DealStage stage)
        => stage == DealStage.ClosedWon || stage == DealStage.ClosedLost;

    public static bool IsOpen(this DealStage stage)
        => !stage.IsClosed() && stage != DealStage.Unknown;
}
