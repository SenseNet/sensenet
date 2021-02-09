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
        /// Hardcoded subscription for all items in the /Root/Content subtree.
        /// </summary>
        private IEnumerable<WebHookSubscription> Subscriptions { get; } = new List<WebHookSubscription>(new[]
        {
            //UNDONE: set other subscription properties in this test implementation
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
            return Subscriptions.Select(sub =>
            {
                var et = sub.GetRelevantEventType(snEvent);
                return et.HasValue ? new WebHookSubscriptionInfo(sub, et.Value) : null;
            }).Where(si => si != null && pe.IsTrue(si.Subscription.FilterQuery));
        }
    }
}
