using System;
using System.Collections.Generic;
using SenseNet.Events;

namespace SenseNet.WebHooks
{
    /// <summary>
    /// Defines methods for loading subscriptions that correspond to an event.
    /// </summary>
    public interface IWebHookSubscriptionStore
    {
        /// <summary>
        /// Returns relevant subscriptions for an event. A subscription may appear
        /// multiple times in the result set if multiple event types are monitored
        /// by the subscription (e.g. Modify and Publish).
        /// </summary>
        /// <param name="snEvent">The event to check for relevancy.</param>
        IEnumerable<WebHookSubscriptionInfo> GetRelevantSubscriptions(ISnEvent snEvent);
    }

    /// <summary>
    /// Empty subscription store.
    /// </summary>
    public class NullWebHookSubscriptionStore : IWebHookSubscriptionStore
    {
        public IEnumerable<WebHookSubscriptionInfo> GetRelevantSubscriptions(ISnEvent snEvent)
        {
            return Array.Empty<WebHookSubscriptionInfo>();
        }
    }
}
