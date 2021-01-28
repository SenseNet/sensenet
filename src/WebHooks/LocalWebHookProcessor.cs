using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
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
            if (!(await _filter.IsRelevantAsync(snEvent).ConfigureAwait(false)))
                return;

            //var subscriptions = Content.All.Where(...);
            //foreach (var subscription in subscriptions)
            //{
            //}

            var node = snEvent.NodeEventArgs.SourceNode;

            await _webHookClient.SendAsync("https://localhost:44362/odata.svc/('Root')/WebHookTest",
                postData: new
                {
                    nodeId = node.Id,
                    path = node.Path,
                    name = node.Name,
                    displayName = node.DisplayName,
                    eventName = snEvent.GetType().Name
                }).ConfigureAwait(false);
        }
    }
}
