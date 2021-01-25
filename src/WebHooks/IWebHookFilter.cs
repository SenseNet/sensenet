using System.Threading.Tasks;
using SenseNet.ContentRepository.Storage;

namespace SenseNet.WebHooks
{
    public interface IWebHookFilter
    {
        //UNDONE: add the correct Event parameter to IWebHookFilter
        Task<bool> IsRelevantAsync(Node node, string eventName);
    }

    public class NullWebHookFilter : IWebHookFilter
    {
        public Task<bool> IsRelevantAsync(Node node, string eventName)
        {
            return Task.FromResult(false);
        }
    }
}
