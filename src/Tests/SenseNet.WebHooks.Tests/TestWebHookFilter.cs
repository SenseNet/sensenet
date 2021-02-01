using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Search;
using SenseNet.Events;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.WebHooks.Tests
{
    internal class TestWebHookFilter : IWebHookFilter
    {
        /// <summary>
        /// Hardcoded subscription for all items in the /Root/Content subtree.
        /// </summary>
        private IEnumerable<WebHookSubscription> Subscriptions { get; } = new List<WebHookSubscription>(new[]
        {
            new WebHookSubscription(Repository.Root)
            {
                Filter = "+InTree:/Root/Content"
            }
        });

        public Task<IEnumerable<WebHookSubscription>> GetRelevantSubscriptionsAsync(ISnEvent snEvent)
        {
            var pe = new PredicationEngine(Content.Create(snEvent.NodeEventArgs.SourceNode));

            // filter the hardcoded subscription list: return the ones that
            // match the current content
            var relevantSubs = Subscriptions.Where(sub => pe.IsTrue(sub.Filter));

            return Task.FromResult(relevantSubs);
        }
    }
}
