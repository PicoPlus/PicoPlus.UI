#nullable enable

namespace PicoPlus.Infrastructure.Extensions;

/// <summary>
/// Configuration options for the extension system.
/// </summary>
public class ExtensionOptions
{
    /// <summary>
    /// List of extension IDs that should be explicitly enabled.
    /// </summary>
    public HashSet<string> EnabledExtensions { get; set; } = new();

    /// <summary>
    /// List of extension IDs that should be disabled.
    /// </summary>
    public HashSet<string> DisabledExtensions { get; set; } = new();

    /// <summary>
    /// Whether to enable extension discovery.
    /// </summary>
    public bool EnableDiscovery { get; set; } = true;
}
