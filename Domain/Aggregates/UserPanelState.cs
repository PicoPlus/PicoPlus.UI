#nullable enable

using NovinCRM.Domain.Entities;
using NovinCRM.Domain.ValueObjects;

namespace NovinCRM.Domain.Aggregates;

/// <summary>
/// Aggregate root that holds the complete, immutable state snapshot
/// of a user's panel session: who they are, their statistics, and
/// their deals list.
///
/// The Application layer is responsible for constructing and caching
/// this aggregate; the Presentation layer consumes it read-only.
/// </summary>
public sealed record UserPanelState
{
    public required Contact Contact { get; init; }
    public required UserStatistics Statistics { get; init; }
    public required IReadOnlyList<Deal> Deals { get; init; }

    /// <summary>
    /// Sentinel value representing a not-yet-loaded panel state.
    /// Avoids null checks at call sites.
    /// </summary>
    public static UserPanelState Empty => new()
    {
        Contact = new Contact
        {
            Id = string.Empty,
            FirstName = "کاربر",
            LastName = "مهمان"
        },
        Statistics = new UserStatistics(),
        Deals = Array.Empty<Deal>()
    };
}
