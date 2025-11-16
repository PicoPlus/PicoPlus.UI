#nullable enable

namespace PicoPlus.State.UserPanel;

/// <summary>
/// Strongly-typed enumeration for deal stages
/// Eliminates magic strings throughout the application
/// </summary>
public enum DealStage
{
    Unknown,
    AppointmentScheduled,
    QualifiedToBuy,
    PresentationScheduled,
    DecisionMakerBoughtIn,
    ContractSent,
    ClosedWon,
    ClosedLost
}

/// <summary>
/// Tab type enumeration for user panel
/// </summary>
public enum TabType
{
    Profile,
    Deals
}
