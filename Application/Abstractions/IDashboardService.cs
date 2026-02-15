using PicoPlus.Domain.Admin;

namespace PicoPlus.Application.Abstractions;

public interface IDashboardService
{
    Task<DashboardStatistics> GetDashboardStatisticsAsync(string? ownerId = null);
    Task<List<RecentActivity>> GetRecentActivitiesAsync(string? ownerId = null, int limit = 10);
}
