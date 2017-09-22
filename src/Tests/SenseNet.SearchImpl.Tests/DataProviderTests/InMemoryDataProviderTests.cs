using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Events;
using SenseNet.ContentRepository.Tests;
using SenseNet.ContentRepository.Tests.Implementations;

namespace SenseNet.SearchImpl.Tests.DataProviderTests
{
    [TestClass]
    public class InMemoryDataProviderTests : TestBase
    {
        [TestMethod]
        public void InMemDb_LoadRootById()
        {
            var node = Test(() => Node.LoadNode(Identifiers.PortalRootId));

            Assert.AreEqual(Identifiers.PortalRootId, node.Id);
            Assert.AreEqual(Identifiers.RootPath, node.Path);
        }
        [TestMethod]
        public void InMemDb_LoadRootByPath()
        {
            var node = Test(() => Node.LoadNode(Identifiers.RootPath));

            Assert.AreEqual(Identifiers.PortalRootId, node.Id);
            Assert.AreEqual(Identifiers.RootPath, node.Path);
        }
        [TestMethod]
        public void InMemDb_Create()
        {
            Node node;
            var result = Test(() =>
            {
                var lastNodeId = ((InMemoryDataProvider)DataProvider.Current).LastNodeId;

                var root = Node.LoadNode(Identifiers.RootPath);
                node = new SystemFolder(root)
                {
                    Name = "Node1",
                    DisplayName = "Node 1"
                };
                foreach (var observer in NodeObserver.GetObserverTypes())
                    node.DisableObserver(observer);
                node.Save();

                node = Node.Load<SystemFolder>(node.Id);
                return new Tuple<int, Node>(lastNodeId, node);

            });
            var lastId = result.Item1;
            node = result.Item2;
            Assert.AreEqual(lastId + 1, node.Id);
            Assert.AreEqual("/Root/Node1", node.Path);
        }
    }
}
