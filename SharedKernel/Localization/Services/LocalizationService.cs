using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PicoPlus.Localization.Abstractions;
using PicoPlus.Localization.Models;

namespace PicoPlus.Localization.Services;

/// <summary>
/// Singleton core implementation of <see cref="ILocalizationService"/>.
/// 
/// Design rationale — Singleton lifetime:
/// • Translation dictionaries are heavy-weight, immutable after loading, and shared
///   across all users and circuits. A singleton avoids the cost of re-loading per-scope.
/// • Language state is intentionally stored per-circuit through a Scoped wrapper
///   (<see cref="ScopedLocalizationService"/>) that delegates lookup to this singleton.
/// • Thread safety is guaranteed via <see cref="FrozenDictionary{TKey,TValue}"/> for reads
///   and <see cref="ReaderWriterLockSlim"/> for the rare language-load/invalidate write path.
/// </summary>
public sealed class LocalizationService : ILocalizationService, IDisposable
{
    // ── Fields ────────────────────────────────────────────────────────────────

    private readonly ILanguageLoader _loader;
    private readonly ILocalizationCache _cache;
    private readonly ILanguageManager _languageManager;
    private readonly LocalizationOptions _options;
    private readonly ILogger<LocalizationService> _logger;

    /// <summary>Protects writes to <see cref="_currentLanguage"/>.</summary>
    private readonly ReaderWriterLockSlim _langLock = new(LockRecursionPolicy.NoRecursion);

    /// <summary>Tracks keys that have already been logged as missing to avoid log spam.</summary>
    private readonly ConcurrentDictionary<string, byte> _loggedMissing = new(StringComparer.OrdinalIgnoreCase);

    private string _currentLanguage;

    // ── Constructor ────────────────────────────────────────────────────────────

    public LocalizationService(
        ILanguageLoader loader,
        ILocalizationCache cache,
        ILanguageManager languageManager,
        IOptions<LocalizationOptions> options,
        ILogger<LocalizationService> logger)
    {
        _loader = loader;
        _cache = cache;
        _languageManager = languageManager;
        _options = options.Value;
        _logger = logger;
        _currentLanguage = _options.DefaultLanguage;
    }

    // ── ILocalizationService: Language state ───────────────────────────────────

    /// <inheritdoc/>
    public string CurrentLanguage
    {
        get
        {
            _langLock.EnterReadLock();
            try { return _currentLanguage; }
            finally { _langLock.ExitReadLock(); }
        }
    }

    /// <inheritdoc/>
    public CultureInfo CurrentCulture => _languageManager.GetCulture(CurrentLanguage);

    /// <inheritdoc/>
    public bool IsRtl => _languageManager.IsRtl(CurrentLanguage);

    /// <inheritdoc/>
    public string TextDirection => IsRtl ? "rtl" : "ltr";

    /// <inheritdoc/>
    public IReadOnlyList<LanguageInfo> AvailableLanguages => _languageManager.Languages;

    // ── ILocalizationService: Indexers ─────────────────────────────────────────

    /// <inheritdoc/>
    public string this[string key] => Translate(key);

    /// <inheritdoc/>
    public string this[string key, params object[] args]
    {
        get
        {
            var raw = Translate(key);
            // Do not format the ##MissingKey## sentinel value.
            if (raw.StartsWith("##", StringComparison.Ordinal))
                return raw;

            try
            {
                return string.Format(CurrentCulture, raw, args);
            }
            catch (FormatException ex)
            {
                _logger.LogWarning(ex,
                    "Localization format error for key '{Key}' in language '{Lang}'. Raw: {Raw}",
                    key, CurrentLanguage, raw);
                return raw;
            }
        }
    }

    // ── ILocalizationService: Pluralization ─────────────────────────────────────

    /// <inheritdoc/>
    public string Pluralize(string key, long count)
    {
        // Convention: Key_One, Key_Other (extend with Key_Few, Key_Many for Arabic/Russian)
        var specificKey = count == 1 ? $"{key}_One" : $"{key}_Other";
        var dict = GetOrLoadDictionary(CurrentLanguage);

        if (dict.TryGetValue(specificKey, out var specific))
            return string.Format(CurrentCulture, specific, count);

        // Graceful degradation: fall back to base key with count injected
        var baseValue = Translate(key);
        if (!baseValue.StartsWith("##", StringComparison.Ordinal))
            return string.Format(CurrentCulture, baseValue, count);

        return baseValue;
    }

    // ── ILocalizationService: Formatting ──────────────────────────────────────

    /// <inheritdoc/>
    public string FormatNumber(double value, string? format = null)
        => format is null
            ? value.ToString("N", CurrentCulture)
            : value.ToString(format, CurrentCulture);

    /// <inheritdoc/>
    public string FormatCurrency(decimal value, string? currencyCode = null)
    {
        if (currencyCode is null)
            return value.ToString("C", CurrentCulture);

        // Use a cloned culture with an overridden currency symbol if an explicit code is given.
        var ci = (CultureInfo)CurrentCulture.Clone();
        ci.NumberFormat.CurrencySymbol = currencyCode;
        return value.ToString("C", ci);
    }

    /// <inheritdoc/>
    public string FormatDate(DateTime value, string? format = null)
        => format is null
            ? value.ToString("d", CurrentCulture)
            : value.ToString(format, CurrentCulture);

    // ── ILocalizationService: Language switching ───────────────────────────────

    /// <inheritdoc/>
    public event EventHandler<LanguageChangedEventArgs>? LanguageChanged;

    /// <inheritdoc/>
    public async Task SetLanguageAsync(string languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
            throw new ArgumentException("Language code cannot be empty.", nameof(languageCode));

        string previous;

        _langLock.EnterWriteLock();
        try
        {
            previous = _currentLanguage;
            if (string.Equals(previous, languageCode, StringComparison.OrdinalIgnoreCase))
                return; // No-op

            _currentLanguage = languageCode;
        }
        finally
        {
            _langLock.ExitWriteLock();
        }

        // Eagerly load the new language so the first render is instant.
        await EnsureLoadedAsync(languageCode).ConfigureAwait(false);

        _logger.LogInformation("Localization language switched: {Previous} → {New}", previous, languageCode);

        // Synchronise Thread culture so .NET date/number formatting agrees.
        var culture = _languageManager.GetCulture(languageCode);
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;

        LanguageChanged?.Invoke(this, new LanguageChangedEventArgs(previous, languageCode));
    }

    // ── Internal: Dictionary lookup ────────────────────────────────────────────

    private string Translate(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return string.Empty;

        var lang = CurrentLanguage;
        var dict = GetOrLoadDictionary(lang);

        if (dict.TryGetValue(key, out var value))
            return value;

        // Fallback to default/English if enabled
        if (_options.EnableFallback && !string.Equals(lang, _options.FallbackLanguage, StringComparison.OrdinalIgnoreCase))
        {
            var fallback = GetOrLoadDictionary(_options.FallbackLanguage);
            if (fallback.TryGetValue(key, out var fallbackValue))
                return fallbackValue;
        }

        RecordMiss(key, lang);
        return _options.MissingKeyTemplate.Replace("{KEY}", key, StringComparison.OrdinalIgnoreCase);
    }

    private IReadOnlyDictionary<string, string> GetOrLoadDictionary(string languageCode)
    {
        var cached = _cache.Get(languageCode);
        if (cached is not null)
            return cached;

        // Synchronous load — only on first access per language. Subsequent calls hit the cache.
        return EnsureLoadedAsync(languageCode).GetAwaiter().GetResult();
    }

    private async Task<IReadOnlyDictionary<string, string>> EnsureLoadedAsync(string languageCode)
    {
        var cached = _cache.Get(languageCode);
        if (cached is not null)
            return cached;

        _logger.LogDebug("Loading localization dictionary for language '{Lang}'", languageCode);
        var dict = await _loader.LoadAsync(languageCode).ConfigureAwait(false);
        _cache.Set(languageCode, dict);
        _logger.LogInformation("Localization dictionary loaded: language='{Lang}', keys={Count}", languageCode, dict.Count);
        return dict;
    }

    private void RecordMiss(string key, string languageCode)
    {
        if (!_options.LogMissingKeys)
            return;

        // Log each unique key only once to prevent log flooding.
        var missKey = $"{languageCode}:{key}";
        if (_loggedMissing.TryAdd(missKey, 0))
        {
            _logger.LogWarning(
                "Localization key not found. Key='{Key}', Language='{Lang}', FallbackEnabled={Fallback}",
                key, languageCode, _options.EnableFallback);
        }
    }

    // ── IDisposable ────────────────────────────────────────────────────────────

    public void Dispose()
    {
        _langLock.Dispose();
    }
}
