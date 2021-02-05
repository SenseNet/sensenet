using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;
using SenseNet.Events;

namespace SenseNet.WebHooks
{
    public class LocalWebHookProcessor : IEventProcessor
    {
        private readonly ILogger<LocalWebHookProcessor> _logger;
        private readonly IWebHookClient _webHookClient;
        private readonly IWebHookFilter _filter;

        public LocalWebHookProcessor(IWebHookFilter filter, IWebHookClient webHookClient, ILogger<LocalWebHookProcessor> logger)
        {
            _filter = filter;
            _webHookClient = webHookClient;
            _logger = logger;
        }

        public async Task ProcessEventAsync(ISnEvent snEvent, CancellationToken cancel)
        {
            var node = snEvent.NodeEventArgs.SourceNode;
            var subscriptions = await _filter.GetRelevantSubscriptionsAsync(snEvent).ConfigureAwait(false);

            //TODO: extend webhook request payload with event-specific info
            var sendingTasks = subscriptions.Select(sub => _webHookClient.SendAsync(
                sub.Url,
                sub.HttpMethod,
                new
                {
                    nodeId = node.Id,
                    path = node.Path,
                    name = node.Name,
                    displayName = node.DisplayName,
                    eventName = snEvent.GetType().Name,
                    subscriptionId = sub.Id,
                    sentTime = DateTime.UtcNow
                },
                sub.HttpHeaders,
                cancel));

            await sendingTasks.WhenAll().ConfigureAwait(false);
        }
    }
}
