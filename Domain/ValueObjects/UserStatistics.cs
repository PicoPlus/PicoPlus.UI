#nullable enable

namespace NovinCRM.Domain.ValueObjects;

/// <summary>
/// Immutable value object holding aggregated statistics for a single user.
/// Calculated by the Application layer from deal data; carried as a unit
/// so callers never work with loose numerics.
/// </summary>
public sealed record UserStatistics
{
    public int TotalDeals { get; init; }
    public int ClosedDeals { get; init; }
    public int OpenDeals { get; init; }
    public decimal TotalRevenue { get; init; }
}
