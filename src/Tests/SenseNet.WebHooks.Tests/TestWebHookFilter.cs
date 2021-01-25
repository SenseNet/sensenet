using SenseNet.ContentRepository.Storage;
using System.Threading.Tasks;

namespace SenseNet.WebHooks.Tests
{
    internal class TestWebHookFilter : IWebHookFilter
    {
        public Task<bool> IsRelevantAsync(Node node, string eventName)
        {
            return Task.FromResult(node.InTree("/Root/Content"));
        }
    }
}
