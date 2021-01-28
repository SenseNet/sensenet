using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Events;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Events;
using System;

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

    internal class TestNodeEventArgs : INodeEventArgs
    {
        public Node SourceNode { get; }

        public IUser User => throw new NotImplementedException();

        public DateTime Time => throw new NotImplementedException();

        public TestNodeEventArgs(Node node)
        {
            SourceNode = node;
        }
    }
}
