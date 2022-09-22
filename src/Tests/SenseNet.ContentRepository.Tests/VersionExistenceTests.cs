﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Versioning;
using SenseNet.Testing;
using SenseNet.Tests.Core;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class VersionExistenceTests : TestBase
    {
        private IDataStore DataStore => Providers.Instance.DataStore;

        [TestMethod]
        public void Versioning_LostVersion_NodeDataIsNull()
        {
            Test(() =>
            {
                //-- Preparing
                var file = CreateTestFile(save: false);

                file.ApprovingMode = ApprovingType.False;
                file.VersioningMode = VersioningType.None;
                file.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
                var contentId = file.Id;

                //-- Thread #1
                file.CheckOutAsync(CancellationToken.None).GetAwaiter().GetResult();

                //-- Thread #2
                var head = DataStore.LoadNodeHeadAsync(contentId, CancellationToken.None).GetAwaiter().GetResult();

                //-- Thread #1
                file.CheckInAsync(CancellationToken.None).GetAwaiter().GetResult();

                //-- Thread #2
                var data = DataStore.LoadNodeAsync(head, head.LastMinorVersionId, CancellationToken.None)
                    .GetAwaiter().GetResult().NodeData;
                Assert.IsNull(data);
            });
        }
        [TestMethod]
        public void Versioning_LostVersion_NodeIsNotNull()
        {
            Test(() =>
            {
                //-- Preparing
                var file = CreateTestFile(save: false);
                file.ApprovingMode = ApprovingType.False;
                file.VersioningMode = VersioningType.None;
                file.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
                var contentId = file.Id;

                //-- Thread #1
                file.CheckOutAsync(CancellationToken.None).GetAwaiter().GetResult();

                //-- Thread #2
                var head = DataStore.LoadNodeHeadAsync(contentId, CancellationToken.None).GetAwaiter().GetResult();

                //-- Thread #1
                file.CheckInAsync(CancellationToken.None).GetAwaiter().GetResult();

                //-- Thread #2
                var node = LoadNode(head, VersionNumber.LastAccessible);
                Assert.IsNotNull(node);
            });
        }
        [TestMethod]
        public void Versioning_LostVersion_NodeIsDeleted()
        {
            Test(() =>
            {
                //-- Preparing
                var file = CreateTestFile(save: false);
                file.ApprovingMode = ApprovingType.False;
                file.VersioningMode = VersioningType.None;
                file.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
                var contentId = file.Id;

                //-- Thread #1
                file.CheckOutAsync(CancellationToken.None).GetAwaiter().GetResult();

                //-- Thread #2
                var head = DataStore.LoadNodeHeadAsync(contentId, CancellationToken.None).GetAwaiter().GetResult();

                //-- Thread #1
                file.ForceDelete();

                //-- Thread #2
                var node = LoadNode(head, VersionNumber.LastAccessible);
                Assert.IsNull(node);
            });
        }

        [TestMethod]
        public void Versioning_LostVersions_AllNodesAreExist()
        {
            Test(() =>
            {
                var files = new File[5];
                var ids = new int[5];
                var versionids = new int[5];
                var root = CreateTestRoot();
                for (int i = 0; i < files.Length; i++)
                {
                    var file = CreateTestFile(save: false, parent: root);
                    file.ApprovingMode = ApprovingType.False;
                    file.VersioningMode = VersioningType.None;
                    file.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();

                    files[i] = file;
                    ids[i] = file.Id;
                    versionids[i] = file.VersionId;
                }

                //-- Thread #1
                files[1].CheckOutAsync(CancellationToken.None).GetAwaiter().GetResult();
                files[3].CheckOutAsync(CancellationToken.None).GetAwaiter().GetResult();

                //-- Thread #2
                var heads = DataStore.LoadNodeHeadsAsync(ids, CancellationToken.None).GetAwaiter().GetResult();

                //-- Thread #1
                files[1].CheckInAsync(CancellationToken.None).GetAwaiter().GetResult();
                files[3].CheckInAsync(CancellationToken.None).GetAwaiter().GetResult();

                //-- Thread #2
                var nodes = LoadNodes(heads, VersionNumber.LastAccessible);
                var v2 = nodes.Select(c => c.VersionId).ToArray();

                Assert.IsTrue(!versionids.Except(v2).Any());
            });
        }
        [TestMethod]
        public void Versioning_LostVersions_TwoNodesAreDeleted()
        {
            Test(() =>
            {
                //-- Preparing
                var root = CreateTestRoot();
                var gcontents = Enumerable.Repeat(typeof(int), 5).Select((x) =>
                {
                    var file = CreateTestFile(root, null, false);
                    file.ApprovingMode = ApprovingType.False;
                    file.VersioningMode = VersioningType.None;
                    file.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
                    return file;
                }).ToArray();
                var ids = gcontents.Select(c => c.Id).ToArray();
                // reload
                gcontents = Node.LoadNodes(ids).Cast<File>().ToArray();
                var versionids = gcontents.Select(c => c.VersionId).ToArray();

                //-- Thread #1
                gcontents[1].CheckOutAsync(CancellationToken.None).GetAwaiter().GetResult();
                gcontents[3].CheckOutAsync(CancellationToken.None).GetAwaiter().GetResult();

                //-- Thread #2
                var heads = DataStore.LoadNodeHeadsAsync(ids, CancellationToken.None).GetAwaiter().GetResult();

                //-- Thread #1
                gcontents[1].CheckInAsync(CancellationToken.None).GetAwaiter().GetResult();
                gcontents[3].ForceDelete();
                gcontents[2].ForceDelete();

                //-- Thread #2
                var nodes = LoadNodes(heads, VersionNumber.LastAccessible);
                var v2 = nodes.Select(c => c.VersionId).ToArray();

                var diff = versionids.Except(v2).ToArray();
                Assert.IsTrue(diff.Count() == 2);
                Assert.IsTrue(diff.Contains(versionids[2]));
                Assert.IsTrue(diff.Contains(versionids[3]));
            });
        }

        /* ============================================================================= helpers */

        private Node LoadNode(NodeHead head, VersionNumber version)
        {
            var nodeAcc = new TypeAccessor(typeof(Node));
            var node = (Node)nodeAcc.InvokeStatic("LoadNode", head, version);
            return node;
        }
        private List<Node> LoadNodes(IEnumerable<NodeHead> heads, VersionNumber version)
        {
            var nodeAcc = new TypeAccessor(typeof(Node));
            var nodes = (List<Node>)nodeAcc.InvokeStatic("LoadNodes", heads, version);
            return nodes;
        }

        private GenericContent CreateTestRoot(bool save = true)
        {
            var node = new SystemFolder(Repository.Root) { Name = Guid.NewGuid().ToString() };
            if (save)
                node.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
            return node;
        }

        /// <summary>
        /// Creates a file without binary. Name is a GUID if not passed. Parent is a newly created SystemFolder.
        /// </summary>
        private File CreateTestFile(string name = null, bool save = true)
        {
            return CreateTestFile(CreateTestRoot(), name ?? Guid.NewGuid().ToString(), save);
        }

        /// <summary>
        /// Creates a file without binary under the given parent node.
        /// </summary>
        private static File CreateTestFile(Node parent, string name = null, bool save = true)
        {
            var file = new File(parent) { Name = name ?? Guid.NewGuid().ToString() };
            if (save)
                file.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
            return file;
        }

    }
}
