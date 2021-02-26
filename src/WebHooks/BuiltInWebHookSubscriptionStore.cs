using System;
using System.Collections.Generic;
using System.Linq;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Search;
using SenseNet.Events;

namespace SenseNet.WebHooks
{
    /// <summary>
    /// Default subscription store that loads subscription content from the System/WebHooks
    /// folder in the repository. Only enabled and valid subscriptions are returned.
    /// </summary>
    public class BuiltInWebHookSubscriptionStore : IWebHookSubscriptionStore
    {
        /// <inheritdoc/>
        public IEnumerable<WebHookSubscriptionInfo> GetRelevantSubscriptions(ISnEvent snEvent)
        {
            //TODO: implement a subscription cache that is invalidated when a subscription changes
            // Do NOT cache nodes, their data is already cached. Cache only ids, paths, or trees.

            // ReSharper disable once RedundantBoolCompare
            var allSubs = Content.All.DisableAutofilters().Where(c =>
                c.InTree("/Root/System/WebHooks") &&
                c.ContentHandler is WebHookSubscription &&
                (bool)c["Enabled"] == true)
                .AsEnumerable()
                .Select(c => (WebHookSubscription)c.ContentHandler)
                .SelectMany(sub => {
                    // prefilter: check if this event is relevant for the subscription
                    var eventTypes = sub.GetRelevantEventTypes(snEvent);

                    // handle multiple relevant event types by adding the subscription multiple times
                    return eventTypes.Select(et => new WebHookSubscriptionInfo(sub, et));
                })
                .Where(si => si != null && si.Subscription.IsValid)
                .ToList();

            if (!allSubs.Any())
                return Array.Empty<WebHookSubscriptionInfo>();

            // use the already constructed Content instance if possible
            var content = snEvent.NodeEventArgs.SourceNode is GenericContent gc
                ? gc.Content
                : Content.Create(snEvent.NodeEventArgs.SourceNode);

            // filter by the query defined by the subscriber
            var pe = new PredicationEngine(content);
            return allSubs.Where(sub => pe.IsTrue(sub.Subscription.FilterQuery)).ToList();
        }
    }
}
