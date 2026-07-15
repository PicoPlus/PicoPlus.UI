using System.Globalization;

namespace NovinCRM.Localization.Models;

/// <summary>
/// Metadata for a registered language / locale.
/// Immutable value object.
/// </summary>
/// <param name="Code">Two-letter ISO code, e.g. "en" or "fa".</param>
/// <param name="CultureCode">BCP-47 culture tag, e.g. "en-US" or "fa-IR".</param>
/// <param name="NativeName">Language name in its own script, e.g. "فارسی" or "English".</param>
/// <param name="EnglishName">Language name in English, e.g. "Persian" or "English".</param>
/// <param name="IsRtl">Whether this language is right-to-left.</param>
/// <param name="FlagIcon">Optional CSS class or emoji for a flag icon.</param>
public sealed record LanguageInfo(
    string Code,
    string CultureCode,
    string NativeName,
    string EnglishName,
    bool IsRtl,
    string? FlagIcon = null)
{
    /// <summary>
    /// Returns the <see cref="CultureInfo"/> for this language.
    /// </summary>
    public CultureInfo ToCultureInfo() => new(CultureCode);

    /// <summary>
    /// Well-known English language definition.
    /// </summary>
    public static readonly LanguageInfo English = new(
        Code: "en",
        CultureCode: "en-US",
        NativeName: "English",
        EnglishName: "English",
        IsRtl: false,
        FlagIcon: "🇺🇸");

    /// <summary>
    /// Well-known Persian (Farsi) language definition.
    /// </summary>
    public static readonly LanguageInfo Persian = new(
        Code: "fa",
        CultureCode: "fa-IR",
        NativeName: "فارسی",
        EnglishName: "Persian",
        IsRtl: true,
        FlagIcon: "🇮🇷");
}
