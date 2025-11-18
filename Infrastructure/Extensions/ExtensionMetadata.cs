#nullable enable

namespace PicoPlus.Infrastructure.Extensions;

/// <summary>
/// Metadata information about an extension.
/// </summary>
public class ExtensionMetadata
{
    /// <summary>
    /// Unique identifier for the extension.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Display name of the extension.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Description of what the extension does.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Version of the extension.
    /// </summary>
    public string Version { get; init; } = "1.0.0";

    /// <summary>
    /// Author of the extension.
    /// </summary>
    public string Author { get; init; } = string.Empty;

    /// <summary>
    /// Dependencies required by this extension (other extension IDs).
    /// </summary>
    public IReadOnlyList<string> Dependencies { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Whether the extension is enabled by default.
    /// </summary>
    public bool EnabledByDefault { get; init; } = true;
}
