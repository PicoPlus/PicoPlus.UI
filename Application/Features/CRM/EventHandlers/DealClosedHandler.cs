#nullable enable

using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NovinCRM.Application.Common.Interfaces;
using NovinCRM.Domain.Events.Deal;
using NovinCRM.Domain.Events.Integration;

namespace NovinCRM.Application.Features.CRM.EventHandlers;

/// <summary>
/// Handles the <see cref="DealClosedEvent"/> domain event.
///
/// Responsibilities:
///   1. Invalidate the user-panel cache.
///   2. Publish a <see cref="DealClosedIntegrationEvent"/>.
///
/// Note: the deal-closed SMS notification is currently sent synchronously
/// by the SmsService where deals are closed. This handler does NOT duplicate
/// that call — it only handles the event-driven side effects.
/// </summary>
public sealed class DealClosedHandler : INotificationHandler<DealClosedEvent>
{
    private readonly IIntegrationEventPublisher  _publisher;
    private readonly IMemoryCache                _cache;
    private readonly ILogger<DealClosedHandler>  _logger;

    public DealClosedHandler(
        IIntegrationEventPublisher  publisher,
        IMemoryCache                cache,
        ILogger<DealClosedHandler>  logger)
    {
        _publisher = publisher;
        _cache     = cache;
        _logger    = logger;
    }

    public async Task Handle(
        DealClosedEvent notification,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "DealClosedHandler: deal {DealId} closed ({Stage}) contact={ContactId}",
            notification.DealId, notification.Stage, notification.ContactId ?? "none");

        if (!string.IsNullOrEmpty(notification.ContactId))
            _cache.Remove(NovinCRM.Application.Common.CacheKeys.UserPanel(notification.ContactId));

        await _publisher.PublishAsync(new DealClosedIntegrationEvent
        {
            DealId             = notification.DealId,
            DealName           = notification.DealName,
            Stage              = notification.Stage.ToString(),
            Amount             = notification.Amount,
            ContactId          = notification.ContactId,
            ContactPhone       = notification.ContactPhone,
            ContactFirstName   = notification.ContactFirstName,
            ContactLastName    = notification.ContactLastName,
        }, cancellationToken).ConfigureAwait(false);
    }
}
