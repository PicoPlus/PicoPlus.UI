namespace PicoPlus.Localization.Abstractions;

/// <summary>
/// Thread-safe, in-memory cache for loaded translation dictionaries.
/// Decoupled from persistence so it can be replaced with a distributed cache (Redis, etc.).
/// </summary>
public interface ILocalizationCache
{
    /// <summary>
    /// Returns the cached dictionary for the given language, or <c>null</c> if not yet cached.
    /// </summary>
    IReadOnlyDictionary<string, string>? Get(string languageCode);

    /// <summary>
    /// Stores the dictionary for the given language, replacing any prior entry.
    /// </summary>
    void Set(string languageCode, IReadOnlyDictionary<string, string> dictionary);

    /// <summary>
    /// Removes the cached dictionary for the given language, forcing a reload on next access.
    /// </summary>
    void Invalidate(string languageCode);

    /// <summary>
    /// Removes all cached dictionaries.
    /// </summary>
    void InvalidateAll();

    /// <summary>
    /// Returns <c>true</c> if the given language has a loaded entry in cache.
    /// </summary>
    bool IsLoaded(string languageCode);
}
