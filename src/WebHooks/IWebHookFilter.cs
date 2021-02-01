using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SenseNet.Events;

namespace SenseNet.WebHooks
{
    public interface IWebHookFilter
    {
        Task<IEnumerable<WebHookSubscription>> GetRelevantSubscriptionsAsync(ISnEvent snEvent);
    }

    public class NullWebHookFilter : IWebHookFilter
    {
        public Task<IEnumerable<WebHookSubscription>> GetRelevantSubscriptionsAsync(ISnEvent snEvent)
        {
            return Task.FromResult((IEnumerable<WebHookSubscription>)Array.Empty<WebHookSubscription>());
        }
    }
}
