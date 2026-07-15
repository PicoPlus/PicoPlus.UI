namespace NovinCRM.Localization.Abstractions;

/// <summary>
/// Responsible for discovering and loading raw translation dictionaries from the file system.
/// Decoupled from caching and service logic so alternative loaders (DB, CDN) can be plugged in.
/// </summary>
public interface ILanguageLoader
{
    /// <summary>
    /// Loads all translation keys for the given language code.
    /// Returns a flat dictionary of <c>module.key → translation</c> merged across all module files.
    /// </summary>
    /// <param name="languageCode">
    /// Two-letter ISO language code, e.g. "en" or "fa".
    /// Maps to a sub-folder under the <c>Localization/</c> directory.
    /// </param>
    /// <returns>Flat dictionary of key → translated string. Never returns null.</returns>
    Task<IReadOnlyDictionary<string, string>> LoadAsync(string languageCode);

    /// <summary>
    /// Returns all language codes that have a corresponding directory under <c>Localization/</c>.
    /// </summary>
    Task<IReadOnlyList<string>> GetAvailableLanguagesAsync();
}
