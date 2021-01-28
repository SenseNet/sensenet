using System.Threading.Tasks;
using SenseNet.Events;

namespace SenseNet.WebHooks.Tests
{
    internal class TestWebHookFilter : IWebHookFilter
    {
        public Task<bool> IsRelevantAsync(ISnEvent snEvent)
        {
            return Task.FromResult(snEvent.NodeEventArgs.SourceNode.InTree("/Root/Content"));
        }
    }
}
