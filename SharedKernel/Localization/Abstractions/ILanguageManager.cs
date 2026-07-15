using System.Globalization;
using NovinCRM.Localization.Models;

namespace NovinCRM.Localization.Abstractions;

/// <summary>
/// Manages the registry of supported languages and maps language codes to
/// <see cref="CultureInfo"/> and <see cref="LanguageInfo"/> metadata.
/// </summary>
public interface ILanguageManager
{
    /// <summary>Returns all registered languages.</summary>
    IReadOnlyList<LanguageInfo> Languages { get; }

    /// <summary>
    /// Returns the <see cref="LanguageInfo"/> for the given code, or <c>null</c> if not found.
    /// </summary>
    LanguageInfo? GetLanguage(string languageCode);

    /// <summary>
    /// Returns the <see cref="CultureInfo"/> for the given language code.
    /// Falls back to <see cref="LocalizationOptions.FallbackLanguage"/> if the code is unknown.
    /// </summary>
    CultureInfo GetCulture(string languageCode);

    /// <summary>
    /// Returns <c>true</c> if the given language code is registered and supported.
    /// </summary>
    bool IsSupported(string languageCode);

    /// <summary>
    /// Returns <c>true</c> if the given language is right-to-left (e.g. fa, ar, he).
    /// </summary>
    bool IsRtl(string languageCode);
}
