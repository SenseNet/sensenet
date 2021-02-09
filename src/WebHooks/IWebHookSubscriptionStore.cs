using System;
using System.Collections.Generic;
using SenseNet.Events;

namespace SenseNet.WebHooks
{
    public interface IWebHookSubscriptionStore
    {
        IEnumerable<WebHookSubscriptionInfo> GetRelevantSubscriptions(ISnEvent snEvent);
    }

    public class NullWebHookSubscriptionStore : IWebHookSubscriptionStore
    {
        public IEnumerable<WebHookSubscriptionInfo> GetRelevantSubscriptions(ISnEvent snEvent)
        {
            return Array.Empty<WebHookSubscriptionInfo>();
        }
    }
}
