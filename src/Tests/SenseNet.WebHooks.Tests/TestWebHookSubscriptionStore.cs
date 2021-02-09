using System.Collections.Generic;
using System.Linq;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Search;
using SenseNet.Events;

namespace SenseNet.WebHooks.Tests
{
    internal class TestWebHookSubscriptionStore : IWebHookSubscriptionStore
    {
        /// <summary>
        /// Hardcoded subscription for items in the /Root/Content subtree.
        /// </summary>
        private IEnumerable<WebHookSubscription> Subscriptions { get; } = new List<WebHookSubscription>(new[]
        {
            new WebHookSubscription(Repository.Root)
            {
                FilterQuery = "+InTree:/Root/Content",
                FilterData = new WebHookFilterData { 
                    Path = "/Root/Content",
                    ContentTypes = new []
                    {
                        new ContentTypeFilterData
                        {
                            Name = "Folder",
                            Events = new []
                            {
                                WebHookEventType.Create, 
                                WebHookEventType.Delete
                            }
                        }, 
                    }},
                Enabled = true
            }
        });

        public IEnumerable<WebHookSubscriptionInfo> GetRelevantSubscriptions(ISnEvent snEvent)
        {
            var pe = new PredicationEngine(Content.Create(snEvent.NodeEventArgs.SourceNode));

            // filter the hardcoded subscription list: return the ones that
            // match the current content
            return Subscriptions.SelectMany(sub =>
            {
                var eventTypes = sub.GetRelevantEventTypes(snEvent);

                // handle multiple relevant event types by adding the subscription multiple times
                return eventTypes.Select(et => new WebHookSubscriptionInfo(sub, et));
            }).Where(si => si != null && 
                           pe.IsTrue(si.Subscription.FilterQuery) &&
                           si.Subscription.Enabled &&
                           si.Subscription.IsValid);
        }
    }
}
