#nullable enable

namespace PicoPlus.Infrastructure.Plugins;

/// <summary>
/// Metadata information about a plugin.
/// </summary>
public class PluginMetadata
{
    /// <summary>
    /// Unique identifier for the plugin.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Display name of the plugin.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Version of the plugin (e.g., "1.0.0").
    /// </summary>
    public required string Version { get; init; }

    /// <summary>
    /// Description of what the plugin does.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Author or organization that created the plugin.
    /// </summary>
    public string? Author { get; init; }

    /// <summary>
    /// Website URL for the plugin or author.
    /// </summary>
    public string? Website { get; init; }

    /// <summary>
    /// License information (e.g., "MIT", "Apache 2.0").
    /// </summary>
    public string? License { get; init; }

    /// <summary>
    /// Tags or categories for the plugin.
    /// </summary>
    public string[]? Tags { get; init; }

    /// <summary>
    /// Minimum required version of PicoPlus.
    /// </summary>
    public string? MinimumPicoPlusVersion { get; init; }

    /// <summary>
    /// Dependencies on other plugins (plugin IDs).
    /// </summary>
    public string[]? Dependencies { get; init; }
}
