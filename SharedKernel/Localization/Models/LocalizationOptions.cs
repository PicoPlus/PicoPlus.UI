namespace NovinCRM.Localization.Models;

/// <summary>
/// Configuration options for the JSON-based localization system.
/// Bind from <c>appsettings.json</c> section <c>"Localization"</c> or configure in code.
/// </summary>
public sealed class LocalizationOptions
{
    /// <summary>Configuration section name used with <c>IConfiguration</c>.</summary>
    public const string SectionName = "Localization";

    /// <summary>
    /// Root directory (relative to <c>ContentRootPath</c>) where language sub-folders live.
    /// Default: <c>"Localization"</c>.
    /// </summary>
    public string LocaizationRoot { get; set; } = "Localization";

    /// <summary>
    /// Two-letter code of the language used as ultimate fallback when a key is missing.
    /// Default: <c>"en"</c>.
    /// </summary>
    public string FallbackLanguage { get; set; } = "en";

    /// <summary>
    /// Default language served to users who have no persisted preference.
    /// Default: <c>"fa"</c> (Persian, matching the project's primary audience).
    /// </summary>
    public string DefaultLanguage { get; set; } = "fa";

    /// <summary>
    /// String returned when a key does not exist in any language.
    /// The substring <c>{KEY}</c> is replaced with the actual key name.
    /// Default: <c>"##{KEY}##"</c>.
    /// </summary>
    public string MissingKeyTemplate { get; set; } = "##{KEY}##";

    /// <summary>
    /// When <c>true</c>, missing keys are logged at Warning level.
    /// Disable in unit tests to suppress noise.
    /// Default: <c>true</c>.
    /// </summary>
    public bool LogMissingKeys { get; set; } = true;

    /// <summary>
    /// When <c>true</c>, the language loader will also look for keys in the fallback language
    /// when a key is absent from the current language dictionary.
    /// Default: <c>true</c>.
    /// </summary>
    public bool EnableFallback { get; set; } = true;

    /// <summary>
    /// Separator used between module name and key when keys are stored flat.
    /// Example: <c>"Common.Save"</c> with separator <c>"."</c>.
    /// Default: <c>"."</c>.
    /// </summary>
    public string KeySeparator { get; set; } = ".";

    /// <summary>
    /// When <c>true</c>, the file system is monitored for changes and dictionaries are
    /// hot-reloaded automatically (development-friendly).
    /// Default: <c>false</c> (set to <c>true</c> in Development environment).
    /// </summary>
    public bool EnableHotReload { get; set; } = false;

    /// <summary>
    /// List of explicitly supported language codes.
    /// If empty, all sub-directories under <see cref="LocaizationRoot"/> are used.
    /// </summary>
    public List<string> SupportedLanguages { get; set; } = new();
}
