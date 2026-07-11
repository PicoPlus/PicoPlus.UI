using System.Collections.Concurrent;
using System.Collections.Frozen;
using PicoPlus.Localization.Abstractions;

namespace PicoPlus.Localization.Services;

/// <summary>
/// Thread-safe, in-memory implementation of <see cref="ILocalizationCache"/>.
///
/// Design:
/// • <see cref="FrozenDictionary{TKey,TValue}"/> is used for cached dictionaries —
///   it is allocated once per language and gives the fastest possible lookup (O(1), no locks).
/// • A <see cref="ConcurrentDictionary{TKey,TValue}"/> holds the outer map so that
///   multiple languages can be cached concurrently without blocking each other.
/// </summary>
public sealed class LocalizationCache : ILocalizationCache
{
    private readonly ConcurrentDictionary<string, IReadOnlyDictionary<string, string>> _store
        = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, string>? Get(string languageCode)
    {
        _store.TryGetValue(languageCode, out var dict);
        return dict;
    }

    /// <inheritdoc/>
    public void Set(string languageCode, IReadOnlyDictionary<string, string> dictionary)
    {
        // Materialise to FrozenDictionary for maximum read throughput.
        // ToFrozenDictionary is a .NET 8+ BCL method — no extra package needed.
        var frozen = dictionary.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
        _store[languageCode] = frozen;
    }

    /// <inheritdoc/>
    public void Invalidate(string languageCode)
        => _store.TryRemove(languageCode, out _);

    /// <inheritdoc/>
    public void InvalidateAll()
        => _store.Clear();

    /// <inheritdoc/>
    public bool IsLoaded(string languageCode)
        => _store.ContainsKey(languageCode);
}
