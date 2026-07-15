using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NovinCRM.Localization.Abstractions;
using NovinCRM.Localization.Models;

namespace NovinCRM.Localization.Services;

/// <summary>
/// Singleton registry of supported languages.
/// Maps two-letter language codes to <see cref="LanguageInfo"/> and <see cref="CultureInfo"/>.
/// </summary>
public sealed class LanguageManager : ILanguageManager
{
    private readonly Dictionary<string, LanguageInfo> _registry;
    private readonly LocalizationOptions _options;
    private readonly ILogger<LanguageManager> _logger;

    public LanguageManager(
        IOptions<LocalizationOptions> options,
        ILogger<LanguageManager> logger)
    {
        _options = options.Value;
        _logger = logger;

        // Seed the registry with built-in well-known languages.
        // Custom languages discovered on disk will be registered dynamically by the loader.
        _registry = new Dictionary<string, LanguageInfo>(StringComparer.OrdinalIgnoreCase)
        {
            [LanguageInfo.English.Code]  = LanguageInfo.English,
            [LanguageInfo.Persian.Code]  = LanguageInfo.Persian,
            // Extend here or via AddLanguage() for additional locales.
        };
    }

    /// <inheritdoc/>
    public IReadOnlyList<LanguageInfo> Languages => _registry.Values.ToList().AsReadOnly();

    /// <inheritdoc/>
    public LanguageInfo? GetLanguage(string languageCode)
    {
        _registry.TryGetValue(languageCode, out var info);
        return info;
    }

    /// <inheritdoc/>
    public CultureInfo GetCulture(string languageCode)
    {
        if (_registry.TryGetValue(languageCode, out var info))
        {
            try
            {
                return info.ToCultureInfo();
            }
            catch (CultureNotFoundException ex)
            {
                _logger.LogWarning(ex,
                    "CultureInfo not found for '{CultureCode}'. Falling back to invariant.",
                    info.CultureCode);
            }
        }

        // Attempt fallback culture
        var fallback = _options.FallbackLanguage;
        if (!string.Equals(languageCode, fallback, StringComparison.OrdinalIgnoreCase)
            && _registry.TryGetValue(fallback, out var fallbackInfo))
        {
            try { return fallbackInfo.ToCultureInfo(); }
            catch (CultureNotFoundException) { /* ignore */ }
        }

        return CultureInfo.InvariantCulture;
    }

    /// <inheritdoc/>
    public bool IsSupported(string languageCode)
        => _registry.ContainsKey(languageCode);

    /// <inheritdoc/>
    public bool IsRtl(string languageCode)
        => _registry.TryGetValue(languageCode, out var info) && info.IsRtl;

    /// <summary>
    /// Dynamically registers a language not present in the seed registry.
    /// Called by the language loader when it discovers a new directory.
    /// </summary>
    internal void EnsureRegistered(string languageCode)
    {
        if (_registry.ContainsKey(languageCode))
            return;

        // Best-effort: try to build LanguageInfo from CultureInfo.
        try
        {
            var ci = new CultureInfo(languageCode);
            var info = new LanguageInfo(
                Code: languageCode,
                CultureCode: ci.Name,
                NativeName: ci.NativeName,
                EnglishName: ci.EnglishName,
                IsRtl: ci.TextInfo.IsRightToLeft);

            _registry[languageCode] = info;
            _logger.LogInformation("Dynamically registered language '{Code}'.", languageCode);
        }
        catch (CultureNotFoundException)
        {
            _logger.LogWarning(
                "Could not auto-register language '{Code}': unknown CultureInfo. Add it manually.", languageCode);
        }
    }
}
