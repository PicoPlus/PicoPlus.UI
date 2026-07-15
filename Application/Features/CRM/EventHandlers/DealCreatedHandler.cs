#nullable enable

using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NovinCRM.Application.Common.Interfaces;
using NovinCRM.Domain.Enums;
using NovinCRM.Domain.Events.Deal;
using NovinCRM.Domain.Events.Integration;

namespace NovinCRM.Application.Features.CRM.EventHandlers;

/// <summary>
/// Handles the <see cref="DealCreatedEvent"/> domain event.
///
/// Responsibilities:
///   1. Invalidate the user-panel cache for the owning contact.
///   2. Publish a <see cref="DealCreatedIntegrationEvent"/> for external consumers.
///   3. Auto-create a HubSpot Invoice for the deal (non-fatal if it fails).
///   4. If the deal is created directly at ClosedWon, also publish a
///      <see cref="DealClosedIntegrationEvent"/> so the SMS handler fires —
///      because DealClosedEvent is only raised on stage transitions, not on creation.
/// </summary>
public sealed class DealCreatedHandler : INotificationHandler<DealCreatedEvent>
{
    private readonly IIntegrationEventPublisher  _publisher;
    private readonly IContactRepository          _contactRepo;
    private readonly IInvoiceService             _invoiceService;
    private readonly IMemoryCache                _cache;
    private readonly ILogger<DealCreatedHandler> _logger;

    public DealCreatedHandler(
        IIntegrationEventPublisher  publisher,
        IContactRepository          contactRepo,
        IInvoiceService             invoiceService,
        IMemoryCache                cache,
        ILogger<DealCreatedHandler> logger)
    {
        _publisher      = publisher;
        _contactRepo    = contactRepo;
        _invoiceService = invoiceService;
        _cache          = cache;
        _logger         = logger;
    }

    public async Task Handle(
        DealCreatedEvent notification,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "DealCreatedHandler: deal {DealId} created stage={Stage} (contact={ContactId})",
            notification.DealId, notification.Stage, notification.ContactId ?? "none");

        // Invalidate user-panel cache for the owning contact.
        if (!string.IsNullOrEmpty(notification.ContactId))
        {
            _cache.Remove(NovinCRM.Application.Common.CacheKeys.UserPanel(notification.ContactId));
            _logger.LogDebug(
                "DealCreatedHandler: invalidated cache for contact {ContactId}",
                notification.ContactId);
        }

        await _publisher.PublishAsync(new DealCreatedIntegrationEvent
        {
            DealId    = notification.DealId,
            DealName  = notification.DealName,
            Amount    = notification.Amount,
            Pipeline  = notification.Pipeline,
            ContactId = notification.ContactId,
        }, cancellationToken).ConfigureAwait(false);

        // Auto-create HubSpot Invoice — non-fatal; deal is never rolled back on failure.
        try
        {
            var invoiceId = await _invoiceService.CreateForDealAsync(
                notification.DealId,
                notification.DealName,
                closeDate: null,
                cancellationToken);

            _logger.LogInformation(
                "DealCreatedHandler: auto-invoice {InvoiceId} created for deal {DealId}",
                invoiceId, notification.DealId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "DealCreatedHandler: invoice creation failed for deal {DealId} — continuing",
                notification.DealId);
        }

        // If deal was created directly at ClosedWon, trigger the closed-deal SMS
        // path — DealClosedEvent is only raised on transitions, not on creation.
        if (notification.Stage == DealStage.ClosedWon && !string.IsNullOrEmpty(notification.ContactId))
        {
            string? phone = null, firstName = null, lastName = null;
            try
            {
                var contact = await _contactRepo.GetByIdAsync(notification.ContactId);
                phone     = contact?.Phone;
                firstName = contact?.FirstName;
                lastName  = contact?.LastName;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "DealCreatedHandler: could not load contact {ContactId} for ClosedWon SMS",
                    notification.ContactId);
            }

            if (!string.IsNullOrEmpty(phone))
            {
                _logger.LogInformation(
                    "DealCreatedHandler: deal {DealId} created at ClosedWon — publishing DealClosedIntegrationEvent",
                    notification.DealId);

                await _publisher.PublishAsync(new DealClosedIntegrationEvent
                {
                    DealId           = notification.DealId,
                    DealName         = notification.DealName,
                    Stage            = DealStage.ClosedWon.ToString(),
                    Amount           = notification.Amount,
                    ContactId        = notification.ContactId,
                    ContactPhone     = phone,
                    ContactFirstName = firstName,
                    ContactLastName  = lastName,
                }, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                _logger.LogWarning(
                    "DealCreatedHandler: deal {DealId} ClosedWon but contact {ContactId} has no phone — SMS skipped",
                    notification.DealId, notification.ContactId);
            }
        }
    }
}
