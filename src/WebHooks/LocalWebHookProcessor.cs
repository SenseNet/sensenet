using System.Linq;
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

        public async Task ProcessEventAsync(ISnEvent snEvent)
        {
            var node = snEvent.NodeEventArgs.SourceNode;
            var subscriptions = await _filter.GetRelevantSubscriptionsAsync(snEvent).ConfigureAwait(false);

            //UNDONE: finalize webhook request payload
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
                    subscriptionId = sub.Id
                },
                sub.GetHeaders()));

            await sendingTasks.WhenAll().ConfigureAwait(false);
        }
    }
}
