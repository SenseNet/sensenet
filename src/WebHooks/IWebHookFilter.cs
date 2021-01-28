using System.Threading.Tasks;
using SenseNet.Events;

namespace SenseNet.WebHooks
{
    public interface IWebHookFilter
    {
        Task<bool> IsRelevantAsync(ISnEvent snEvent);
    }

    public class NullWebHookFilter : IWebHookFilter
    {
        public Task<bool> IsRelevantAsync(ISnEvent snEvent)
        {
            return Task.FromResult(false);
        }
    }
}
