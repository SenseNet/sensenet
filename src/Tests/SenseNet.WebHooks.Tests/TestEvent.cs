using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Events;
using SenseNet.Events;

namespace SenseNet.WebHooks.Tests
{
    internal class TestEvent1 : ISnEvent
    {
        public INodeEventArgs NodeEventArgs { get; set; }

        public TestEvent1(INodeEventArgs e)
        {
            NodeEventArgs = e;
        }
    }

    internal class TestNodeEventArgs : NodeEventArgs
    {
        public TestNodeEventArgs(Node node, NodeEvent eventType) : base(node, eventType, null) { }
    }
}
