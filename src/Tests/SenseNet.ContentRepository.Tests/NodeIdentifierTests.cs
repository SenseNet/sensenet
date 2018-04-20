using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.Storage;
using SenseNet.Tests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class NodeIdentifierTests : TestBase
    {
        [TestMethod]
        public void NodeIdentifier_Create()
        {
            var identifier = NodeIdentifier.Get(123);
            Assert.AreEqual(123, identifier.Id, "#1 identifier is incorrect.");
            Assert.IsNull(identifier.Path, "#2 identifier is incorrect.");

            identifier = NodeIdentifier.Get("/Root/P1");
            Assert.AreEqual("/Root/P1", identifier.Path, "#3 identifier is incorrect.");
            Assert.AreEqual(0, identifier.Id, "#4 identifier is incorrect.");

            identifier = NodeIdentifier.Get("123");
            Assert.AreEqual(123, identifier.Id, "#5 identifier is incorrect.");
            Assert.IsNull(identifier.Path, "#6 identifier is incorrect.");

            short shortId = 123;
            identifier = NodeIdentifier.Get(shortId);
            Assert.AreEqual(shortId, identifier.Id, "#7 identifier is incorrect.");
            Assert.IsNull(identifier.Path, "#8 identifier is incorrect.");
        }

        [TestMethod]
        public void NodeIdentifier_LoadNodes()
        {
            Test(() =>
            {
                var nodes = CreateSafeContentQuery("+TypeIs:Folder .TOP:3 .AUTOFILTERS:OFF").Execute().Nodes.ToArray();
                var ids = new object[]
                {
                    nodes[0].Id,
                    nodes[1].Path,
                    nodes[2].Id.ToString(),
                    null
                };

                var loadedNodes = Node.LoadNodes(ids.Select(NodeIdentifier.Get)).ToArray();

                Assert.AreEqual(3, loadedNodes.Length);
                Assert.AreEqual(nodes[0].Id, loadedNodes[0].Id);
                Assert.AreEqual(nodes[1].Id, loadedNodes[1].Id);
                Assert.AreEqual(nodes[2].Id, loadedNodes[2].Id);
            });
        }

        //===================================================================================== Not supported id types

        [TestMethod]
        [ExpectedException(typeof(SnNotSupportedException))]
        public void NodeIdentifier_Invalid_1()
        {
            NodeIdentifier.Get(12.34);
        }
        [TestMethod]
        [ExpectedException(typeof(SnNotSupportedException))]
        public void NodeIdentifier_Invalid_2()
        {
            NodeIdentifier.Get(true);
        }
        [TestMethod]
        [ExpectedException(typeof(SnNotSupportedException))]
        public void NodeIdentifier_Invalid_3()
        {
            NodeIdentifier.Get(new byte[] { 1, 2, 3 });
        }
        [TestMethod]
        [ExpectedException(typeof(SnNotSupportedException))]
        public void NodeIdentifier_Invalid_4()
        {
            NodeIdentifier.Get(NodeHead.Get(1));
        }
        [TestMethod]
        [ExpectedException(typeof(SnNotSupportedException))]
        public void NodeIdentifier_Invalid_5()
        {
            NodeIdentifier.Get(new object());
        }
        [TestMethod]
        public void NodeIdentifier_Valid()
        {
            Test(() =>
            {
                NodeIdentifier.Get(User.Administrator);
            });
        }
    }
}
