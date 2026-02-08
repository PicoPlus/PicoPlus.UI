#nullable enable

namespace PicoPlus.Application.Dto.UserPanel;

/// <summary>
/// Immutable DTO for user statistics
/// </summary>
public sealed record UserStatistics
{
    public int TotalDeals { get; init; }
    public int ClosedDeals { get; init; }
    public int OpenDeals { get; init; }
    public decimal TotalRevenue { get; init; }
}
