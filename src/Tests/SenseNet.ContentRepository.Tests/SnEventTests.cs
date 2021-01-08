using System;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.Storage.Events;
using SenseNet.Diagnostics;
using SenseNet.Extensions.DependencyInjection;
using SenseNet.Tests.Core;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class SnEventTests : TestBase
    {
        private class NodeObserver1 : NodeObserver { }
        private class NodeObserver2 : NodeObserver { }
        private class NodeObserver3 : NodeObserver { }

        [TestMethod]
        public void Event_1()
        {
            Test(builder =>
            {
                builder.EnableNodeObservers(typeof(NodeObserver1), typeof(NodeObserver2), typeof(NodeObserver3));
            }, () =>
            {
                var node = new SystemFolder(Repository.Root) {Name = Guid.NewGuid().ToString()};
                node.Save();

                // ACTION
                node.Index++;
                using (var op = SnTrace.StartOperation("-------- TEST: NODE.SAVE"))
                {
                    node.Save();
                    op.Successful = true;
                }
                Thread.Sleep(2000);

                // ASSERT
                Assert.Inconclusive();
            });
        }
    }
}
