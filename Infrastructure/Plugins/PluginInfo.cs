#nullable enable

using System.Reflection;

namespace PicoPlus.Infrastructure.Plugins;

/// <summary>
/// Information about a loaded plugin instance.
/// </summary>
public class PluginInfo
{
    /// <summary>
    /// The plugin instance.
    /// </summary>
    public required IPlugin Plugin { get; init; }

    /// <summary>
    /// Path to the plugin assembly file.
    /// </summary>
    public required string AssemblyPath { get; init; }

    /// <summary>
    /// The plugin's assembly.
    /// </summary>
    public required Assembly Assembly { get; init; }

    /// <summary>
    /// The load context for this plugin.
    /// </summary>
    public required PluginLoadContext LoadContext { get; init; }

    /// <summary>
    /// Whether the plugin is currently enabled.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// When the plugin was loaded.
    /// </summary>
    public DateTime LoadedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Any errors that occurred during plugin operations.
    /// </summary>
    public List<string> Errors { get; init; } = new();
}
