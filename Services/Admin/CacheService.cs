using Microsoft.Extensions.Caching.Memory;
using PicoPlus.Models.Admin;

namespace PicoPlus.Services.Admin;

/// <summary>
/// High-performance caching service for admin dashboard data
/// </summary>
public class CacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<CacheService> _logger;
    
    // Cache key prefixes
    private const string DASHBOARD_STATS_KEY = "admin_dashboard_stats";
    private const string RECENT_ACTIVITIES_KEY = "admin_recent_activities";
    private const string KANBAN_BOARD_KEY = "admin_kanban_board";
    private const string PIPELINE_DATA_KEY = "admin_pipeline_data";
    private const string OWNERS_LIST_KEY = "admin_owners_list";
    
    // Cache durations
    private static readonly TimeSpan ShortCacheDuration = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan MediumCacheDuration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan LongCacheDuration = TimeSpan.FromMinutes(15);

    public CacheService(IMemoryCache cache, ILogger<CacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Get or create cached dashboard statistics
    /// </summary>
    public async Task<DashboardStatistics> GetOrCreateDashboardStatsAsync(
        string? ownerId,
        Func<Task<DashboardStatistics>> factory)
    {
        var key = GetOwnerSpecificKey(DASHBOARD_STATS_KEY, ownerId);
        
        if (!_cache.TryGetValue(key, out DashboardStatistics? stats))
        {
            _logger.LogInformation("Cache miss for dashboard stats. Owner: {OwnerId}", ownerId ?? "All");
            stats = await factory();
            
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(MediumCacheDuration)
                .SetSlidingExpiration(ShortCacheDuration)
                .SetPriority(CacheItemPriority.High);
            
            _cache.Set(key, stats, cacheOptions);
        }
        else
        {
            _logger.LogDebug("Cache hit for dashboard stats. Owner: {OwnerId}", ownerId ?? "All");
        }
        
        return stats!;
    }

    /// <summary>
    /// Get or create cached recent activities
    /// </summary>
    public async Task<List<RecentActivity>> GetOrCreateRecentActivitiesAsync(
        string? ownerId,
        Func<Task<List<RecentActivity>>> factory)
    {
        var key = GetOwnerSpecificKey(RECENT_ACTIVITIES_KEY, ownerId);
        
        if (!_cache.TryGetValue(key, out List<RecentActivity>? activities))
        {
            _logger.LogInformation("Cache miss for recent activities. Owner: {OwnerId}", ownerId ?? "All");
            activities = await factory();
            
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(ShortCacheDuration)
                .SetPriority(CacheItemPriority.Normal);
            
            _cache.Set(key, activities, cacheOptions);
        }
        
        return activities!;
    }

    /// <summary>
    /// Get or create cached kanban board data
    /// </summary>
    public async Task<List<KanbanColumn>> GetOrCreateKanbanBoardAsync(
        string? ownerId,
        string? pipelineId,
        Func<Task<List<KanbanColumn>>> factory)
    {
        var key = $"{GetOwnerSpecificKey(KANBAN_BOARD_KEY, ownerId)}_{pipelineId ?? "default"}";
        
        if (!_cache.TryGetValue(key, out List<KanbanColumn>? columns))
        {
            _logger.LogInformation("Cache miss for kanban board. Owner: {OwnerId}, Pipeline: {Pipeline}", 
                ownerId ?? "All", pipelineId ?? "Default");
            columns = await factory();
            
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(ShortCacheDuration)
                .SetPriority(CacheItemPriority.Normal);
            
            _cache.Set(key, columns, cacheOptions);
        }
        
        return columns!;
    }

    /// <summary>
    /// Get or create cached pipeline data
    /// </summary>
    public async Task<List<(string Id, string Name)>> GetOrCreatePipelinesAsync(
        Func<Task<List<(string Id, string Name)>>> factory)
    {
        if (!_cache.TryGetValue(PIPELINE_DATA_KEY, out List<(string Id, string Name)>? pipelines))
        {
            _logger.LogInformation("Cache miss for pipeline data");
            pipelines = await factory();
            
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(LongCacheDuration)
                .SetPriority(CacheItemPriority.High);
            
            _cache.Set(PIPELINE_DATA_KEY, pipelines, cacheOptions);
        }
        
        return pipelines!;
    }

    /// <summary>
    /// Get or create cached owners list
    /// </summary>
    public async Task<List<HubSpotOwner>> GetOrCreateOwnersListAsync(
        Func<Task<List<HubSpotOwner>>> factory)
    {
        if (!_cache.TryGetValue(OWNERS_LIST_KEY, out List<HubSpotOwner>? owners))
        {
            _logger.LogInformation("Cache miss for owners list");
            owners = await factory();
            
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(LongCacheDuration)
                .SetPriority(CacheItemPriority.High);
            
            _cache.Set(OWNERS_LIST_KEY, owners, cacheOptions);
        }
        
        return owners!;
    }

    /// <summary>
    /// Invalidate all cache entries for a specific owner
    /// </summary>
    public void InvalidateOwnerCache(string? ownerId)
    {
        _logger.LogInformation("Invalidating cache for owner: {OwnerId}", ownerId ?? "All");
        
        _cache.Remove(GetOwnerSpecificKey(DASHBOARD_STATS_KEY, ownerId));
        _cache.Remove(GetOwnerSpecificKey(RECENT_ACTIVITIES_KEY, ownerId));
        
        // Also invalidate kanban (multiple possible pipeline combinations)
        _cache.Remove(GetOwnerSpecificKey(KANBAN_BOARD_KEY, ownerId));
    }

    /// <summary>
    /// Invalidate dashboard statistics cache
    /// </summary>
    public void InvalidateDashboardStats(string? ownerId = null)
    {
        _cache.Remove(GetOwnerSpecificKey(DASHBOARD_STATS_KEY, ownerId));
        _logger.LogDebug("Dashboard stats cache invalidated for owner: {OwnerId}", ownerId ?? "All");
    }

    /// <summary>
    /// Invalidate kanban board cache
    /// </summary>
    public void InvalidateKanbanBoard(string? ownerId = null, string? pipelineId = null)
    {
        var key = $"{GetOwnerSpecificKey(KANBAN_BOARD_KEY, ownerId)}_{pipelineId ?? "default"}";
        _cache.Remove(key);
        _logger.LogDebug("Kanban board cache invalidated");
    }

    /// <summary>
    /// Invalidate all admin caches
    /// </summary>
    public void InvalidateAll()
    {
        _logger.LogInformation("Invalidating all admin caches");
        // Clear owner-specific caches for common patterns
        InvalidateDashboardStats(null);
        _cache.Remove(PIPELINE_DATA_KEY);
        _cache.Remove(OWNERS_LIST_KEY);
    }

    /// <summary>
    /// Get performance metrics for the cache
    /// </summary>
    public CacheMetrics GetMetrics()
    {
        // Basic metrics - in production, use a more detailed implementation
        return new CacheMetrics
        {
            CacheProvider = "InMemory",
            Status = "Healthy",
            LastUpdated = DateTime.UtcNow
        };
    }

    private static string GetOwnerSpecificKey(string baseKey, string? ownerId)
    {
        return string.IsNullOrEmpty(ownerId) 
            ? $"{baseKey}_all" 
            : $"{baseKey}_{ownerId}";
    }
}

/// <summary>
/// Cache performance metrics
/// </summary>
public class CacheMetrics
{
    public string CacheProvider { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; }
    public long TotalHits { get; set; }
    public long TotalMisses { get; set; }
    public double HitRate => TotalHits + TotalMisses > 0 
        ? (double)TotalHits / (TotalHits + TotalMisses) * 100 
        : 0;
}
