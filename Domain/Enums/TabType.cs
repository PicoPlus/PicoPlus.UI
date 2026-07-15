#nullable enable

namespace NovinCRM.Domain.Enums;

/// <summary>
/// Tab type enumeration for user panel navigation.
/// Kept in the domain layer so Application use-cases can carry
/// the active-tab concept without a UI dependency.
/// </summary>
public enum TabType
{
    Profile,
    Deals
}
