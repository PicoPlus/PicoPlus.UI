using System.Diagnostics;
using System.Collections.Concurrent;

namespace PicoPlus.Services.Admin;

/// <summary>
/// High-performance monitoring service for tracking admin dashboard metrics
/// </summary>
public class PerformanceMonitorService
{
    private readonly ILogger<PerformanceMonitorService> _logger;
    private readonly ConcurrentDictionary<string, List<PerformanceEntry>> _metrics = new();
    private readonly ConcurrentDictionary<string, long> _counters = new();
    private readonly int _maxEntriesPerMetric = 100;

    public PerformanceMonitorService(ILogger<PerformanceMonitorService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Track an operation's execution time
    /// </summary>
    public IDisposable TrackOperation(string operationName)
    {
        return new OperationTracker(this, operationName);
    }

    /// <summary>
    /// Record a performance metric
    /// </summary>
    public void RecordMetric(string metricName, double value, Dictionary<string, string>? tags = null)
    {
        var entry = new PerformanceEntry
        {
            Timestamp = DateTime.UtcNow,
            Value = value,
            Tags = tags ?? new()
        };

        _metrics.AddOrUpdate(
            metricName,
            _ => new List<PerformanceEntry> { entry },
            (_, list) =>
            {
                lock (list)
                {
                    list.Add(entry);
                    // Keep only recent entries - use efficient removal
                    if (list.Count > _maxEntriesPerMetric)
                    {
                        var removeCount = list.Count - _maxEntriesPerMetric;
                        list.RemoveRange(0, removeCount);
                    }
                }
                return list;
            });

        _logger.LogDebug("Recorded metric {Metric}: {Value}", metricName, value);
    }

    /// <summary>
    /// Increment a counter
    /// </summary>
    public void IncrementCounter(string counterName)
    {
        _counters.AddOrUpdate(counterName, 1, (_, value) => value + 1);
    }

    /// <summary>
    /// Get aggregated metrics for a specific operation
    /// </summary>
    public OperationMetrics GetOperationMetrics(string operationName)
    {
        if (!_metrics.TryGetValue(operationName, out var entries))
        {
            return new OperationMetrics { OperationName = operationName };
        }

        lock (entries)
        {
            if (entries.Count == 0)
            {
                return new OperationMetrics { OperationName = operationName };
            }
            
            var values = entries.Select(e => e.Value).ToList();
            return new OperationMetrics
            {
                OperationName = operationName,
                TotalCalls = entries.Count,
                AverageMs = values.Average(),
                MinMs = values.Min(),
                MaxMs = values.Max(),
                P95Ms = CalculatePercentile(values, 95),
                P99Ms = CalculatePercentile(values, 99),
                LastUpdated = entries.LastOrDefault()?.Timestamp ?? DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// Get all performance metrics summary
    /// </summary>
    public PerformanceSummary GetSummary()
    {
        var summary = new PerformanceSummary
        {
            Timestamp = DateTime.UtcNow,
            Operations = new Dictionary<string, OperationMetrics>(),
            Counters = new Dictionary<string, long>(_counters)
        };

        foreach (var kvp in _metrics)
        {
            summary.Operations[kvp.Key] = GetOperationMetrics(kvp.Key);
        }

        // Calculate overall health
        summary.HealthStatus = CalculateHealthStatus(summary.Operations);

        return summary;
    }

    /// <summary>
    /// Get RED metrics (Rate, Errors, Duration) for dashboard
    /// </summary>
    public RedMetrics GetRedMetrics()
    {
        var summary = GetSummary();
        var apiOps = summary.Operations
            .Where(o => o.Key.StartsWith("api_"))
            .ToList();

        var totalCalls = apiOps.Sum(o => o.Value.TotalCalls);
        var errorCount = _counters.GetValueOrDefault("errors_total", 0);

        return new RedMetrics
        {
            RequestRate = totalCalls,
            ErrorRate = totalCalls > 0 ? (double)errorCount / totalCalls * 100 : 0,
            AverageLatencyMs = apiOps.Count > 0 
                ? apiOps.Average(o => o.Value.AverageMs) 
                : 0,
            P95LatencyMs = apiOps.Count > 0 
                ? apiOps.Max(o => o.Value.P95Ms) 
                : 0
        };
    }

    /// <summary>
    /// Clear all metrics
    /// </summary>
    public void ClearMetrics()
    {
        _metrics.Clear();
        _counters.Clear();
        _logger.LogInformation("Performance metrics cleared");
    }

    private static double CalculatePercentile(List<double> values, int percentile)
    {
        if (values.Count == 0) return 0;
        
        var sorted = values.OrderBy(v => v).ToList();
        var index = (int)Math.Ceiling(percentile / 100.0 * sorted.Count) - 1;
        return sorted[Math.Max(0, index)];
    }

    private static string CalculateHealthStatus(Dictionary<string, OperationMetrics> operations)
    {
        if (operations.Count == 0) return "Unknown";

        var avgLatency = operations.Values.Average(o => o.AverageMs);
        var maxP95 = operations.Values.Max(o => o.P95Ms);

        if (maxP95 > 1000 || avgLatency > 500)
            return "Degraded";
        if (maxP95 > 500 || avgLatency > 200)
            return "Warning";
        
        return "Healthy";
    }

    /// <summary>
    /// Helper class to track operation execution time
    /// </summary>
    private class OperationTracker : IDisposable
    {
        private readonly PerformanceMonitorService _monitor;
        private readonly string _operationName;
        private readonly Stopwatch _stopwatch;

        public OperationTracker(PerformanceMonitorService monitor, string operationName)
        {
            _monitor = monitor;
            _operationName = operationName;
            _stopwatch = Stopwatch.StartNew();
        }

        public void Dispose()
        {
            _stopwatch.Stop();
            _monitor.RecordMetric(_operationName, _stopwatch.ElapsedMilliseconds);
        }
    }
}

/// <summary>
/// Individual performance entry
/// </summary>
public class PerformanceEntry
{
    public DateTime Timestamp { get; set; }
    public double Value { get; set; }
    public Dictionary<string, string> Tags { get; set; } = new();
}

/// <summary>
/// Aggregated metrics for a single operation
/// </summary>
public class OperationMetrics
{
    public string OperationName { get; set; } = string.Empty;
    public int TotalCalls { get; set; }
    public double AverageMs { get; set; }
    public double MinMs { get; set; }
    public double MaxMs { get; set; }
    public double P95Ms { get; set; }
    public double P99Ms { get; set; }
    public DateTime LastUpdated { get; set; }
}

/// <summary>
/// Overall performance summary
/// </summary>
public class PerformanceSummary
{
    public DateTime Timestamp { get; set; }
    public string HealthStatus { get; set; } = "Unknown";
    public Dictionary<string, OperationMetrics> Operations { get; set; } = new();
    public Dictionary<string, long> Counters { get; set; } = new();
}

/// <summary>
/// RED metrics (Rate, Errors, Duration)
/// </summary>
public class RedMetrics
{
    public long RequestRate { get; set; }
    public double ErrorRate { get; set; }
    public double AverageLatencyMs { get; set; }
    public double P95LatencyMs { get; set; }
}
