using System;
using System.Collections.Generic;
using SenseNet.Events;

namespace SenseNet.WebHooks
{
    public interface IWebHookFilter
    {
        IEnumerable<WebHookSubscriptionInfo> GetRelevantSubscriptions(ISnEvent snEvent);
    }

    public class NullWebHookFilter : IWebHookFilter
    {
        public IEnumerable<WebHookSubscriptionInfo> GetRelevantSubscriptions(ISnEvent snEvent)
        {
            return Array.Empty<WebHookSubscriptionInfo>();
        }
    }
}
