using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using NovinCRM.Localization.Abstractions;
using NovinCRM.Localization.Models;
using NovinCRM.Localization.Services;

namespace NovinCRM.Localization.Extensions;

/// <summary>
/// Extension methods for registering the NovinCRM JSON localization system with the DI container.
/// </summary>
public static class LocalizationServiceCollectionExtensions
{
    /// <summary>
    /// Registers all services required by the NovinCRM localization system.
    ///
    /// Lifetime decisions:
    /// <list type="bullet">
    ///   <item>
    ///     <term><see cref="LocalizationCache"/> — Singleton</term>
    ///     <description>Translation dictionaries are immutable once loaded and shared globally.</description>
    ///   </item>
    ///   <item>
    ///     <term><see cref="LanguageManager"/> — Singleton</term>
    ///     <description>Language registry is read-only after startup; safe to share.</description>
    ///   </item>
    ///   <item>
    ///     <term><see cref="JsonLanguageLoader"/> — Singleton</term>
    ///     <description>Holds the FileSystemWatcher; must be singleton to persist across requests.</description>
    ///   </item>
    ///   <item>
    ///     <term><see cref="LocalizationService"/> — Singleton</term>
    ///     <description>Shared dictionary access, thread-safe via FrozenDictionary.</description>
    ///   </item>
    ///   <item>
    ///     <term><see cref="ScopedLocalizationService"/> — Scoped</term>
    ///     <description>
    ///       Per-circuit language state. Each Blazor Server circuit (user session) has its own
    ///       language selection, persisted to/restored from LocalStorage. Delegates translation
    ///       lookups to the singleton layer, so memory cost is minimal.
    ///     </description>
    ///   </item>
    /// </list>
    ///
    /// Usage in Razor:
    /// <code>
    /// @inject ILocalizationService L
    ///
    /// &lt;h1&gt;@L["DashboardTitle"]&lt;/h1&gt;
    /// &lt;label&gt;@L["LabelFirstName"]&lt;/label&gt;
    /// &lt;button&gt;@L["Save"]&lt;/button&gt;
    /// &lt;p&gt;@L["WelcomeUser", username]&lt;/p&gt;
    /// </code>
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional delegate to override default <see cref="LocalizationOptions"/>.</param>
    public static IServiceCollection AddNovinCRMLocalization(
        this IServiceCollection services,
        Action<LocalizationOptions>? configure = null)
    {
        // ── Options ──────────────────────────────────────────────────────────
        services.AddOptions<LocalizationOptions>()
            .BindConfiguration(LocalizationOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        if (configure is not null)
            services.Configure(configure);

        // ── Cache (Singleton) ─────────────────────────────────────────────────
        services.TryAddSingleton<ILocalizationCache, LocalizationCache>();

        // ── Language Manager (Singleton) ──────────────────────────────────────
        services.TryAddSingleton<LanguageManager>();
        services.TryAddSingleton<ILanguageManager>(sp => sp.GetRequiredService<LanguageManager>());

        // ── Language Loader (Singleton) ────────────────────────────────────────
        services.TryAddSingleton<ILanguageLoader, JsonLanguageLoader>();

        // ── Core Localization Service (Singleton — shared dictionaries) ───────
        services.TryAddSingleton<LocalizationService>();

        // ── Scoped wrapper (per Blazor Server circuit) ─────────────────────────
        // ILocalizationService resolves to ScopedLocalizationService so each
        // user session has its own language state while sharing the loaded data.
        services.TryAddScoped<ScopedLocalizationService>();
        services.TryAddScoped<ILocalizationService>(sp
            => sp.GetRequiredService<ScopedLocalizationService>());

        return services;
    }

    /// <summary>
    /// Enables hot-reload of localization files in the Development environment.
    /// Call this after <see cref="AddNovinCRMLocalization"/> if desired.
    /// </summary>
    public static IServiceCollection EnableLocalizationHotReload(this IServiceCollection services)
    {
        services.Configure<LocalizationOptions>(o => o.EnableHotReload = true);
        return services;
    }
}
