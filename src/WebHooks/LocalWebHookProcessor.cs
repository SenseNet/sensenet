using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Nito.AsyncEx;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Events;
using SenseNet.Events;
using SenseNet.Extensions.DependencyInjection;

namespace SenseNet.WebHooks
{
    internal interface IWebHookEventProcessor : IEventProcessor
    {
        /// <summary>
        /// Test method for firing a webhook directly.
        /// </summary>
        /// <param name="subscription">The webhook instance to fire.</param>
        /// <param name="eventType">Event type</param>
        /// <param name="node">Related content.</param>
        /// <param name="cancel">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        Task FireWebHookAsync(WebHookSubscription subscription, WebHookEventType eventType, Node node, CancellationToken cancel);
    }

    /// <summary>
    /// Event processor implementation for sending webhooks. Gets relevant subscriptions
    /// for an event from the configured subscription store and sends requests using
    /// the configured webhook client.
    /// </summary>
    public class LocalWebHookProcessor : IWebHookEventProcessor
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

            // currently we deal with content-related events only
            if (node == null)
                return;

            var subscriptions = _subscriptionStore.GetRelevantSubscriptions(snEvent)
                .Where(si => si.Subscription.Enabled && si.Subscription.IsValid)
                .ToArray();

            if (subscriptions.Any())
                _logger?.LogTrace($"Sending webhook events for subscriptions: " +
                                  $"{string.Join(", ", subscriptions.Select(si => si.Subscription.Name + " (" + si.EventType + ")"))}");

            //TODO: extend webhook request payload with event-specific info
            var eventArgs = snEvent.NodeEventArgs as NodeEventArgs;
            var previousVersion = eventArgs.GetPreviousVersion();

            var sendingTasks = subscriptions
                .Select(si => _webHookClient.SendAsync(
                si.Subscription.Url,
                si.Subscription.HttpMethod,
                GetPayload(si.Subscription, si.EventType, node, previousVersion),
                si.Subscription.HttpHeaders,
                cancel));

            //TODO: handle responses: webhook statistics implementation

            await sendingTasks.WhenAll().ConfigureAwait(false);
        }

        public Task FireWebHookAsync(WebHookSubscription subscription, WebHookEventType eventType, Node node, CancellationToken cancel)
        {
            return _webHookClient.SendAsync(subscription.Url, subscription.HttpMethod,
                GetPayload(subscription, eventType, node, null), subscription.HttpHeaders, cancel);
        }

        private object GetPayload(WebHookSubscription subscription, WebHookEventType eventType, Node node,
            VersionNumber previousVersion)
        {
            return string.IsNullOrWhiteSpace(subscription.Payload) 
                ? GetDefaultPayload(subscription, eventType, node, previousVersion) 
                : JsonConvert.DeserializeObject(subscription.Payload);
        }

        private object GetDefaultPayload(WebHookSubscription subscription, WebHookEventType eventType, Node node, 
            VersionNumber previousVersion)
        {
            return new
            {
                nodeId = node.Id,
                versionId = node.VersionId,
                version = node.Version?.ToString(),
                previousVersion = previousVersion?.ToString(),
                versionModificationDate = node.VersionModificationDate,
                modifiedBy = node.ModifiedById,
                path = node.Path,
                name = node.Name,
                displayName = node.DisplayName,
                eventName = eventType.ToString(),
                subscriptionId = subscription.Id,
                sentTime = DateTime.UtcNow
            };
        }
    }
}
