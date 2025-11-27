#nullable enable

namespace PicoPlus.Infrastructure.Plugins;

/// <summary>
/// Context information provided to plugins during lifecycle events.
/// </summary>
public class PluginContext
{
    /// <summary>
    /// The application's service provider for resolving dependencies.
    /// </summary>
    public required IServiceProvider ServiceProvider { get; init; }

    /// <summary>
    /// The application's configuration.
    /// </summary>
    public required IConfiguration Configuration { get; init; }

    /// <summary>
    /// The application's environment information.
    /// </summary>
    public required IWebHostEnvironment Environment { get; init; }

    /// <summary>
    /// Logger factory for creating loggers.
    /// </summary>
    public required ILoggerFactory LoggerFactory { get; init; }

    /// <summary>
    /// Path to the plugin's directory.
    /// </summary>
    public required string PluginPath { get; init; }

    /// <summary>
    /// Additional data that can be stored by the plugin.
    /// </summary>
    public Dictionary<string, object> Data { get; init; } = new();
}
