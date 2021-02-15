using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;
using SenseNet.Events;

namespace SenseNet.WebHooks
{
    /// <summary>
    /// Event processor implementation for sending webhooks. Gets relevant subscriptions
    /// for an event from the configured subscription store and sends request using
    /// the configured webhook client.
    /// </summary>
    public class LocalWebHookProcessor : IEventProcessor
    {
        private readonly ILogger<LocalWebHookProcessor> _logger;
        private readonly IWebHookClient _webHookClient;
        private readonly IWebHookSubscriptionStore _subscriptionStore;

        public LocalWebHookProcessor(IWebHookSubscriptionStore subscriptionStore, IWebHookClient webHookClient, ILogger<LocalWebHookProcessor> logger)
        {
            _subscriptionStore = subscriptionStore;
            _webHookClient = webHookClient;
            _logger = logger;
        }

        public async Task ProcessEventAsync(ISnEvent snEvent, CancellationToken cancel)
        {
            var node = snEvent.NodeEventArgs.SourceNode;
            var subscriptions = _subscriptionStore.GetRelevantSubscriptions(snEvent)
                .Where(si => si.Subscription.Enabled && si.Subscription.IsValid)
                .ToArray();

            if (subscriptions.Any())
                _logger?.LogTrace($"Sending webhook events for subscriptions: " +
                                  $"{string.Join(", ", subscriptions.Select(si => si.Subscription.Name + " (" + si.EventType + ")"))}");

            //TODO: extend webhook request payload with event-specific info

            var sendingTasks = subscriptions
                .Select(si => _webHookClient.SendAsync(
                si.Subscription.Url,
                si.Subscription.HttpMethod,
                new
                {
                    nodeId = node.Id,
                    path = node.Path,
                    name = node.Name,
                    displayName = node.DisplayName,
                    eventName = si.EventType.ToString(),
                    subscriptionId = si.Subscription.Id,
                    sentTime = DateTime.UtcNow
                },
                si.Subscription.HttpHeaders,
                cancel));

            //TODO: handle responses: webhook statistics implementation

            await sendingTasks.WhenAll().ConfigureAwait(false);
        }
    }
}
