using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;
using SenseNet.Events;

namespace SenseNet.WebHooks
{
    //UNDONE: webhook docs
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
            var subscriptions = _filter.GetRelevantSubscriptions(snEvent);

            //TODO: extend webhook request payload with event-specific info

            var sendingTasks = subscriptions
                .Where(si => si.Subscription.Enabled && si.Subscription.IsValid)
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
