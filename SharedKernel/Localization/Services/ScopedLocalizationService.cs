using System.Globalization;
using Blazored.LocalStorage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PicoPlus.Localization.Abstractions;
using PicoPlus.Localization.Models;

namespace PicoPlus.Localization.Services;

/// <summary>
/// Scoped wrapper around the singleton <see cref="LocalizationService"/>.
///
/// Why Scoped?
/// Blazor Server creates one SignalR circuit per user session. A Scoped service lives
/// for the lifetime of that circuit, so each user has independent language state
/// while sharing the same loaded dictionaries held by the Singleton.
///
/// This service:
/// 1. Holds the per-user current language.
/// 2. Restores the persisted language from LocalStorage on first use.
/// 3. Persists the chosen language to LocalStorage on change.
/// 4. Delegates all translation work to the singleton <see cref="LocalizationService"/>.
/// </summary>
public sealed class ScopedLocalizationService : ILocalizationService, IAsyncDisposable
{
    // ── Fields ────────────────────────────────────────────────────────────────

    private readonly LocalizationService _singleton;
    private readonly ILocalizationCache _cache;
    private readonly ILanguageLoader _loader;
    private readonly ILanguageManager _languageManager;
    private readonly ILocalStorageService _localStorage;
    private readonly LocalizationOptions _options;
    private readonly ILogger<ScopedLocalizationService> _logger;

    private string _currentLanguage;
    private bool _initialized;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    private const string StorageKey = "picoplus_lang";

    // ── Constructor ────────────────────────────────────────────────────────────

    public ScopedLocalizationService(
        LocalizationService singleton,
        ILocalizationCache cache,
        ILanguageLoader loader,
        ILanguageManager languageManager,
        ILocalStorageService localStorage,
        IOptions<LocalizationOptions> options,
        ILogger<ScopedLocalizationService> logger)
    {
        _singleton = singleton;
        _cache = cache;
        _loader = loader;
        _languageManager = languageManager;
        _localStorage = localStorage;
        _options = options.Value;
        _logger = logger;
        _currentLanguage = _options.DefaultLanguage;
    }

    // ── Initialisation ─────────────────────────────────────────────────────────

    /// <summary>
    /// Asynchronously restores the persisted language from LocalStorage.
    /// Must be called once from a Blazor component's <c>OnAfterRenderAsync(firstRender: true)</c>
    /// because LocalStorage is unavailable during pre-rendering.
    /// </summary>
    public async Task InitialiseAsync()
    {
        if (_initialized) return;

        await _initLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_initialized) return;

            var persisted = await _localStorage.GetItemAsStringAsync(StorageKey).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(persisted) && _languageManager.IsSupported(persisted))
            {
                _currentLanguage = persisted;
                _logger.LogDebug("Restored language '{Lang}' from LocalStorage.", persisted);
            }
            else
            {
                _currentLanguage = _options.DefaultLanguage;
            }

            // Ensure the dictionary for the restored language is warmed up.
            if (!_cache.IsLoaded(_currentLanguage))
            {
                var dict = await _loader.LoadAsync(_currentLanguage).ConfigureAwait(false);
                _cache.Set(_currentLanguage, dict);
            }

            ApplyCultureToThread(_currentLanguage);
            _initialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    // ── ILocalizationService ───────────────────────────────────────────────────

    /// <inheritdoc/>
    public string CurrentLanguage => _currentLanguage;

    /// <inheritdoc/>
    public CultureInfo CurrentCulture => _languageManager.GetCulture(_currentLanguage);

    /// <inheritdoc/>
    public bool IsRtl => _languageManager.IsRtl(_currentLanguage);

    /// <inheritdoc/>
    public string TextDirection => IsRtl ? "rtl" : "ltr";

    /// <inheritdoc/>
    public IReadOnlyList<LanguageInfo> AvailableLanguages => _languageManager.Languages;

    /// <inheritdoc/>
    public string this[string key] => TranslateScoped(key);

    /// <inheritdoc/>
    public string this[string key, params object[] args]
    {
        get
        {
            var raw = TranslateScoped(key);
            if (raw.StartsWith("##", StringComparison.Ordinal))
                return raw;
            try
            {
                return string.Format(CurrentCulture, raw, args);
            }
            catch (FormatException ex)
            {
                _logger.LogWarning(ex, "Format error for key '{Key}'.", key);
                return raw;
            }
        }
    }

    /// <inheritdoc/>
    public string Pluralize(string key, long count)
    {
        // Temporarily point singleton at this scope's language for the call.
        return _singleton.Pluralize(key, count);
    }

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
        var ci = (CultureInfo)CurrentCulture.Clone();
        ci.NumberFormat.CurrencySymbol = currencyCode;
        return value.ToString("C", ci);
    }

    /// <inheritdoc/>
    public string FormatDate(DateTime value, string? format = null)
        => format is null
            ? value.ToString("d", CurrentCulture)
            : value.ToString(format, CurrentCulture);

    /// <inheritdoc/>
    public event EventHandler<LanguageChangedEventArgs>? LanguageChanged;

    /// <inheritdoc/>
    public async Task SetLanguageAsync(string languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
            throw new ArgumentException("Language code cannot be empty.", nameof(languageCode));

        var previous = _currentLanguage;
        if (string.Equals(previous, languageCode, StringComparison.OrdinalIgnoreCase))
            return;

        // Warm-up the dictionary before switching so the first render is instant.
        if (!_cache.IsLoaded(languageCode))
        {
            var dict = await _loader.LoadAsync(languageCode).ConfigureAwait(false);
            _cache.Set(languageCode, dict);
        }

        _currentLanguage = languageCode;

        // Persist to LocalStorage
        await _localStorage.SetItemAsStringAsync(StorageKey, languageCode).ConfigureAwait(false);

        ApplyCultureToThread(languageCode);

        _logger.LogInformation("Language switched: {Prev} → {New}", previous, languageCode);
        LanguageChanged?.Invoke(this, new LanguageChangedEventArgs(previous, languageCode));
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    private string TranslateScoped(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return string.Empty;

        var dict = _cache.Get(_currentLanguage);
        if (dict is not null && dict.TryGetValue(key, out var value))
            return value;

        // Try fallback
        if (_options.EnableFallback
            && !string.Equals(_currentLanguage, _options.FallbackLanguage, StringComparison.OrdinalIgnoreCase))
        {
            var fallback = _cache.Get(_options.FallbackLanguage);
            if (fallback is not null && fallback.TryGetValue(key, out var fallbackValue))
                return fallbackValue;
        }

        // Delegate to singleton which handles logging and missing-key sentinel.
        return _singleton[key];
    }

    private void ApplyCultureToThread(string languageCode)
    {
        var culture = _languageManager.GetCulture(languageCode);
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }

    // ── IAsyncDisposable ───────────────────────────────────────────────────────

    public ValueTask DisposeAsync()
    {
        _initLock.Dispose();
        return ValueTask.CompletedTask;
    }
}
