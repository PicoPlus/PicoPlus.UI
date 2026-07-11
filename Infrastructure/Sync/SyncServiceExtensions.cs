#nullable enable

using Microsoft.Extensions.DependencyInjection;
using PicoPlus.Application.Common.Interfaces;
using PicoPlus.Application.Features.CRM.EventHandlers;
using PicoPlus.Infrastructure.Events;

namespace PicoPlus.Infrastructure.Sync;

/// <summary>
/// Registers the complete EDA + bidirectional sync stack.
/// Call once from Program.cs: <c>builder.Services.AddEventDrivenSync();</c>
/// </summary>
public static class SyncServiceExtensions
{
    public static IServiceCollection AddEventDrivenSync(this IServiceCollection services)
    {
        // ── MediatR ───────────────────────────────────────────────────────────
        // Scans Application and Infrastructure assemblies for INotificationHandlers.
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<
                PicoPlus.Application.Features.Auth.EventHandlers.ContactRegisteredHandler>();
            cfg.RegisterServicesFromAssemblyContaining<
                PicoPlus.Infrastructure.Events.MediatRDomainEventDispatcher>();
        });

        // ── Domain event dispatcher ───────────────────────────────────────────
        services.AddScoped<IDomainEventDispatcher, MediatRDomainEventDispatcher>();

        // ── Integration event publisher ───────────────────────────────────────
        services.AddScoped<IIntegrationEventPublisher, WebhookIntegrationEventPublisher>();

        // ── Sync state (idempotency / duplicate / out-of-order) ───────────────
        // Swap to a database-backed implementation for multi-instance deployments.
        services.AddSingleton<ISyncStateRepository, InMemorySyncStateRepository>();

        // ── Bidirectional sync service ────────────────────────────────────────
        services.AddScoped<BidirectionalSyncService>();

        // ── Webhook handler — routes all HubSpot events to the sync service ──
        services.AddWebhookHandler<HubSpotWebhookSyncHandler>();

        // ── SMS handlers ──────────────────────────────────────────────────────
        // Sends deal-closed SMS when a deal is closed from inside this app.
        services.AddWebhookHandler<DealClosedSmsHandler>();
        // Sends deal-closed SMS when a deal is moved to closedwon directly in HubSpot.
        services.AddWebhookHandler<DealStageWebhookSmsHandler>();

        return services;
    }

    // Forward-declares AddWebhookHandler if called before webhook services are registered.
    // In practice Program.cs calls AddHubSpotWebhooks() before AddEventDrivenSync().
    private static IServiceCollection AddWebhookHandler<THandler>(
        this IServiceCollection services)
        where THandler : class, IHubSpotWebhookHandler
    {
        services.AddScoped<IHubSpotWebhookHandler, THandler>();
        return services;
    }
}
