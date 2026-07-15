#nullable enable

using Microsoft.Extensions.DependencyInjection;
using NovinCRM.Application.Common.Interfaces;

namespace NovinCRM.Infrastructure.Webhooks;

/// <summary>
/// Extension methods for registering the complete HubSpot webhook infrastructure.
///
/// Typical Program.cs usage:
/// <code>
///   builder.Services.AddHubSpotWebhooks();
///   builder.Services.AddWebhookHandler&lt;ContactWebhookHandler&gt;();
/// </code>
/// </summary>
public static class WebhookServiceExtensions
{
    /// <summary>
    /// Registers all webhook infrastructure services:
    /// <list type="bullet">
    ///   <item><see cref="HubSpotWebhookOptions"/> bound from "HubSpot" config section.</item>
    ///   <item><see cref="HubSpotSignatureVerifier"/> — HMAC-SHA256 v3 + replay protection.</item>
    ///   <item><see cref="InMemoryWebhookEventQueue"/> as <see cref="IWebhookEventQueue"/> (Singleton).</item>
    ///   <item><see cref="ExponentialBackoffRetryPolicy"/> as <see cref="IRetryPolicy"/> (Singleton).</item>
    ///   <item><see cref="WebhookDispatcherService"/> hosted background service.</item>
    /// </list>
    ///
    /// Prerequisites: <c>builder.Services.AddMemoryCache()</c> must be called first
    /// (already done in Program.cs before this is called).
    /// </summary>
    public static IServiceCollection AddHubSpotWebhooks(this IServiceCollection services)
    {
        // ── Options ───────────────────────────────────────────────────────────
        services
            .AddOptions<HubSpotWebhookOptions>()
            .BindConfiguration(HubSpotWebhookOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // ── Signature verifier — stateless singleton ──────────────────────────
        services.AddSingleton<HubSpotSignatureVerifier>();

        // ── Bounded channel queue — singleton so dispatcher and endpoint share it.
        // Registered under both interfaces so:
        //   • HTTP endpoint injects IWebhookEventQueue  (write-only view)
        //   • Dispatcher injects IRetryableEventQueue   (adds retry channel access)
        // Both resolve the same singleton instance.
        services.AddSingleton<InMemoryWebhookEventQueue>();
        services.AddSingleton<IWebhookEventQueue>(sp =>
            sp.GetRequiredService<InMemoryWebhookEventQueue>());
        services.AddSingleton<IRetryableEventQueue>(sp =>
            sp.GetRequiredService<InMemoryWebhookEventQueue>());

        // ── Retry policy — singleton, uses options resolved at construction time
        services.AddSingleton<IRetryPolicy>(sp =>
        {
            var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<HubSpotWebhookOptions>>().Value;
            return new ExponentialBackoffRetryPolicy(opts);
        });

        // ── Background dispatcher — drains both main and retry channels ───────
        services.AddHostedService<WebhookDispatcherService>();

        return services;
    }

    /// <summary>
    /// Registers an application-layer <see cref="IHubSpotWebhookHandler"/> implementation.
    /// Multiple handlers can be chained — they are called in registration order.
    /// Each handler receives its own DI scope per event dispatch.
    /// </summary>
    public static IServiceCollection AddWebhookHandler<THandler>(
        this IServiceCollection services)
        where THandler : class, IHubSpotWebhookHandler
    {
        // Scoped: each dispatch creates a scope, so scoped services (e.g.
        // IContactRepository backed by HubSpot HTTP clients) are safe here.
        services.AddScoped<IHubSpotWebhookHandler, THandler>();
        return services;
    }
}
