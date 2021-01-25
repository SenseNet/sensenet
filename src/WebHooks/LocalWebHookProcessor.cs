using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SenseNet.ContentRepository.Storage;

namespace SenseNet.WebHooks
{
    public interface IEventProcessor
    {
        Task ExecuteAsync(Node node, string eventName);
    }

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

        public async Task ExecuteAsync(Node node, string eventName)
        {
            if (!(await _filter.IsRelevantAsync(node, eventName).ConfigureAwait(false)))
                return;

            //var subscriptions = Content.All.Where(...);
            //foreach (var subscription in subscriptions)
            //{
            //}

            await _webHookClient.SendAsync("https://localhost:44362/odata.svc/('Root')/WebHookTest",
                postData: new
                {
                    nodeId = node.Id,
                    path = node.Path,
                    name = node.Name,
                    displayName = node.DisplayName,
                    eventName
                }).ConfigureAwait(false);
        }
    }
}
