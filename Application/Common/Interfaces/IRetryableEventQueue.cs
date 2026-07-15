#nullable enable

using System.Threading.Channels;
using NovinCRM.Application.Common.Interfaces;
using NovinCRM.Infrastructure.Webhooks;

namespace NovinCRM.Infrastructure.Webhooks;

/// <summary>
/// Extends <see cref="IWebhookEventQueue"/> with the retry channel surfaces
/// needed by <c>WebhookDispatcherService</c>. Kept in Infrastructure (not Application)
/// because <see cref="EventEnvelope"/> is an Infrastructure-layer type.
/// </summary>
public interface IRetryableEventQueue : IWebhookEventQueue
{
    /// <summary>Writer end of the retry channel.</summary>
    ChannelWriter<EventEnvelope> RetryWriter { get; }

    /// <summary>Reader end of the retry channel.</summary>
    ChannelReader<EventEnvelope> RetryReader { get; }
}
