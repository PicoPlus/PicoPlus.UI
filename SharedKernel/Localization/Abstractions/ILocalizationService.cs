using System.Globalization;
using NovinCRM.Localization.Models;

namespace NovinCRM.Localization.Abstractions;

/// <summary>
/// Primary localization service. Inject this into Razor components and services.
/// Supports indexed access via <c>L["Key"]</c>, placeholder formatting, pluralization,
/// culture-aware number/date/currency formatting, and RTL detection.
/// </summary>
public interface ILocalizationService
{
    /// <summary>
    /// Gets the translation for the given key using the current language.
    /// Returns <c>##Key##</c> if the key is not found. Never throws.
    /// </summary>
    /// <param name="key">The localization key.</param>
    string this[string key] { get; }

    /// <summary>
    /// Gets the translation for the given key and formats it with the provided arguments.
    /// Supports .NET composite format strings: <c>L["WelcomeUser", username]</c> → "Welcome Karim".
    /// </summary>
    /// <param name="key">The localization key.</param>
    /// <param name="args">Format arguments.</param>
    string this[string key, params object[] args] { get; }

    /// <summary>The currently active language code (e.g. "en", "fa").</summary>
    string CurrentLanguage { get; }

    /// <summary>The currently active culture (e.g. en-US, fa-IR).</summary>
    CultureInfo CurrentCulture { get; }

    /// <summary>Whether the current language is right-to-left.</summary>
    bool IsRtl { get; }

    /// <summary>
    /// Returns the HTML dir attribute value: "rtl" or "ltr".
    /// </summary>
    string TextDirection { get; }

    /// <summary>
    /// Formats a number according to the current culture.
    /// </summary>
    string FormatNumber(double value, string? format = null);

    /// <summary>
    /// Formats a decimal as currency according to the current culture.
    /// </summary>
    string FormatCurrency(decimal value, string? currencyCode = null);

    /// <summary>
    /// Formats a date/time according to the current culture.
    /// </summary>
    string FormatDate(DateTime value, string? format = null);

    /// <summary>
    /// Returns the plural form of a key based on count.
    /// Looks for "{Key}_One" for count=1, "{Key}_Other" for count≠1.
    /// Falls back to "{Key}" if plural forms are not defined.
    /// </summary>
    /// <param name="key">Base localization key.</param>
    /// <param name="count">The count that determines plurality.</param>
    string Pluralize(string key, long count);

    /// <summary>
    /// Returns the list of all available languages.
    /// </summary>
    IReadOnlyList<LanguageInfo> AvailableLanguages { get; }

    /// <summary>
    /// Switches the active language at runtime.
    /// Raises <see cref="LanguageChanged"/> after the switch.
    /// </summary>
    Task SetLanguageAsync(string languageCode);

    /// <summary>
    /// Fired after a runtime language switch. Subscribe in components to trigger re-render.
    /// </summary>
    event EventHandler<LanguageChangedEventArgs>? LanguageChanged;
}
