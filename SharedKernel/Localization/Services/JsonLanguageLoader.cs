using System.Collections.Frozen;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PicoPlus.Localization.Abstractions;
using PicoPlus.Localization.Models;

namespace PicoPlus.Localization.Services;

/// <summary>
/// Loads translation dictionaries from JSON files on the local file system.
///
/// Directory convention:
/// <code>
/// Localization/
///   en/
///     Common.json
///     Login.json
///     Dashboard.json
///     Customer.json
///     Product.json
///     Invoice.json
///     Settings.json
///   fa/
///     Common.json
///     ...
/// </code>
///
/// Key convention:
/// Keys within each JSON file are merged into a single flat dictionary using
/// the separator configured in <see cref="LocalizationOptions.KeySeparator"/>.
///
/// Simple flat keys: <c>{ "Save": "ذخیره" }</c> → key = <c>"Save"</c>
/// Prefixed keys (optional nested):
///   <c>{ "Customer": { "Name": "نام" } }</c> → key = <c>"Customer.Name"</c>
///
/// Hot-reload:
/// When <see cref="LocalizationOptions.EnableHotReload"/> is <c>true</c>, a
/// <see cref="FileSystemWatcher"/> monitors the localization directory and
/// automatically invalidates the cache for the affected language.
/// </summary>
public sealed class JsonLanguageLoader : ILanguageLoader, IDisposable
{
    // ── Fields ────────────────────────────────────────────────────────────────

    private readonly string _rootPath;
    private readonly LocalizationOptions _options;
    private readonly ILocalizationCache _cache;
    private readonly LanguageManager _languageManager;
    private readonly ILogger<JsonLanguageLoader> _logger;
    private readonly FileSystemWatcher? _watcher;

    private static readonly JsonDocumentOptions JsonOptions = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip
    };

    // ── Constructor ────────────────────────────────────────────────────────────

    public JsonLanguageLoader(
        IHostEnvironment environment,
        ILocalizationCache cache,
        LanguageManager languageManager,
        IOptions<LocalizationOptions> options,
        ILogger<JsonLanguageLoader> logger)
    {
        _cache = cache;
        _languageManager = languageManager;
        _options = options.Value;
        _logger = logger;

        _rootPath = Path.Combine(environment.ContentRootPath, _options.LocaizationRoot);

        if (_options.EnableHotReload && Directory.Exists(_rootPath))
        {
            _watcher = CreateWatcher(_rootPath);
            _logger.LogInformation("Localization hot-reload enabled. Watching: {Path}", _rootPath);
        }
    }

    // ── ILanguageLoader ────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<IReadOnlyDictionary<string, string>> LoadAsync(string languageCode)
    {
        var languageDir = Path.Combine(_rootPath, languageCode);

        if (!Directory.Exists(languageDir))
        {
            _logger.LogWarning(
                "Localization directory not found for language '{Lang}': {Path}. Returning empty dictionary.",
                languageCode, languageDir);
            return FrozenDictionary<string, string>.Empty;
        }

        // Dynamically register the language with LanguageManager if new.
        _languageManager.EnsureRegistered(languageCode);

        var jsonFiles = Directory.GetFiles(languageDir, "*.json", SearchOption.TopDirectoryOnly);
        var result = new Dictionary<string, string>(512, StringComparer.OrdinalIgnoreCase);

        foreach (var filePath in jsonFiles)
        {
            try
            {
                await LoadFileIntoAsync(filePath, result).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to load localization file '{File}' for language '{Lang}'.",
                    filePath, languageCode);
                // Continue loading other files — partial data beats complete failure.
            }
        }

        _logger.LogDebug(
            "Loaded {Count} keys from {Files} files for language '{Lang}'.",
            result.Count, jsonFiles.Length, languageCode);

        return result.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<string>> GetAvailableLanguagesAsync()
    {
        if (!Directory.Exists(_rootPath))
        {
            _logger.LogWarning("Localization root directory not found: {Path}", _rootPath);
            return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
        }

        var dirs = Directory
            .GetDirectories(_rootPath)
            .Select(Path.GetFileName)
            .Where(d => !string.IsNullOrWhiteSpace(d))
            .Cast<string>()
            .ToList()
            .AsReadOnly();

        return Task.FromResult<IReadOnlyList<string>>(dirs);
    }

    // ── Private: JSON parsing ──────────────────────────────────────────────────

    private async Task LoadFileIntoAsync(string filePath, Dictionary<string, string> target)
    {
        await using var stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 4096,
            useAsync: true);

        using var doc = await JsonDocument.ParseAsync(stream, JsonOptions).ConfigureAwait(false);
        FlattenElement(doc.RootElement, prefix: string.Empty, target);
    }

    /// <summary>
    /// Recursively flattens a <see cref="JsonElement"/> into the target dictionary.
    /// Nested objects produce dot-separated keys: <c>Customer.Name</c>.
    /// </summary>
    private void FlattenElement(JsonElement element, string prefix, Dictionary<string, string> target)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var prop in element.EnumerateObject())
                {
                    var childKey = prefix.Length == 0
                        ? prop.Name
                        : $"{prefix}{_options.KeySeparator}{prop.Name}";
                    FlattenElement(prop.Value, childKey, target);
                }
                break;

            case JsonValueKind.String:
                var strValue = element.GetString();
                if (strValue is not null)
                    target[prefix] = strValue;
                break;

            case JsonValueKind.Number:
                target[prefix] = element.GetRawText();
                break;

            case JsonValueKind.True:
                target[prefix] = "true";
                break;

            case JsonValueKind.False:
                target[prefix] = "false";
                break;

            case JsonValueKind.Null:
                // Null values are intentionally skipped — a missing key sentinel is better.
                break;

            case JsonValueKind.Array:
                // Arrays are joined as comma-separated strings for simple list translations.
                var items = element.EnumerateArray()
                    .Where(e => e.ValueKind == JsonValueKind.String)
                    .Select(e => e.GetString() ?? string.Empty);
                target[prefix] = string.Join(", ", items);
                break;

            default:
                _logger.LogDebug("Unsupported JSON value kind '{Kind}' at key '{Key}'. Skipping.", element.ValueKind, prefix);
                break;
        }
    }

    // ── Hot-reload: FileSystemWatcher ──────────────────────────────────────────

    private FileSystemWatcher CreateWatcher(string path)
    {
        var watcher = new FileSystemWatcher(path, "*.json")
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
            EnableRaisingEvents = true
        };

        watcher.Changed += OnFileChanged;
        watcher.Created += OnFileChanged;
        watcher.Deleted += OnFileChanged;
        watcher.Renamed += OnFileRenamed;

        return watcher;
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        var lang = GetLanguageCodeFromPath(e.FullPath);
        if (lang is not null)
        {
            _logger.LogInformation("Localization file changed — invalidating cache for '{Lang}': {File}", lang, e.FullPath);
            _cache.Invalidate(lang);
        }
    }

    private void OnFileRenamed(object sender, RenamedEventArgs e)
    {
        var lang = GetLanguageCodeFromPath(e.FullPath);
        if (lang is not null)
        {
            _logger.LogInformation("Localization file renamed — invalidating cache for '{Lang}'", lang);
            _cache.Invalidate(lang);
        }
    }

    private string? GetLanguageCodeFromPath(string fullPath)
    {
        // Path structure: <root>/<languageCode>/File.json
        var rel = Path.GetRelativePath(_rootPath, fullPath);
        var parts = rel.Split(Path.DirectorySeparatorChar, 2);
        return parts.Length > 0 && !string.IsNullOrWhiteSpace(parts[0]) ? parts[0] : null;
    }

    // ── IDisposable ────────────────────────────────────────────────────────────

    public void Dispose() => _watcher?.Dispose();
}
