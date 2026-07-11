#nullable enable

using MediatR;
using Microsoft.Extensions.Logging;
using PicoPlus.Application.Common.Interfaces;
using PicoPlus.Domain.Events.Contact;
using PicoPlus.Domain.Events.Integration;

namespace PicoPlus.Application.Features.Auth.EventHandlers;

/// <summary>
/// Handles the <see cref="ContactRegisteredEvent"/> domain event.
///
/// Responsibilities:
///   1. Publish the corresponding integration event so external consumers
///      (e.g. marketing automation, analytics) are notified.
///
/// The welcome SMS is already sent synchronously inside RegisterService.RegisterAsync —
/// it is NOT duplicated here to preserve the existing behaviour.
///
/// This handler is deliberately lightweight. Complex side-effects
/// (enrichment, analytics) belong in their own handlers.
/// </summary>
public sealed class ContactRegisteredHandler
    : INotificationHandler<ContactRegisteredEvent>
{
    private readonly IIntegrationEventPublisher _publisher;
    private readonly ILogger<ContactRegisteredHandler> _logger;

    public ContactRegisteredHandler(
        IIntegrationEventPublisher publisher,
        ILogger<ContactRegisteredHandler> logger)
    {
        _publisher = publisher;
        _logger    = logger;
    }

    public async Task Handle(
        ContactRegisteredEvent notification,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "ContactRegisteredHandler: contact {ContactId} registered — publishing integration event",
            notification.ContactId);

        await _publisher.PublishAsync(new ContactRegisteredIntegrationEvent
        {
            ContactId    = notification.ContactId,
            FirstName    = notification.FirstName,
            LastName     = notification.LastName,
            Phone        = notification.Phone,
            Email        = notification.Email,
            NationalCode = notification.NationalCode,
        }, cancellationToken).ConfigureAwait(false);
    }
}
