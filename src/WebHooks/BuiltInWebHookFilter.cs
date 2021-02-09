using System;
using System.Collections.Generic;
using System.Linq;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Search;
using SenseNet.Events;

namespace SenseNet.WebHooks
{
    public class BuiltInWebHookFilter : IWebHookFilter
    {
        public IEnumerable<WebHookSubscriptionInfo> GetRelevantSubscriptions(ISnEvent snEvent)
        {
            //TODO: implement a subscription cache that is invalidated when a subscription changes
            // Do NOT cache nodes, their data is already cached. Cache only ids, paths, or trees.
            var allSubs = Content.All.DisableAutofilters().Where(c =>
                c.InTree("/Root/System/WebHooks") &&
                c.ContentHandler is WebHookSubscription &&
                (bool)c["Enabled"] == true)
                .AsEnumerable()
                .Select(c => c.ContentHandler as WebHookSubscription)
                .Select(sub => {
                    // prefilter: check if this event is relevant for the subscription
                    var et = sub?.GetRelevantEventType(snEvent);
                    return et.HasValue ? new WebHookSubscriptionInfo(sub, et.Value) : null;
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
