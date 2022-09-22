﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Versioning;
using SenseNet.Tests.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class TimestampTests : TestBase
    {
        [TestMethod]
        public void Timestamp_CannotSaveObsolete()
        {
            Test(() =>
            {
                try
                {
                    var testRoot = new SystemFolder(Repository.Root) { Name = "Folder1" };
                    testRoot.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
                    var file = new File(testRoot) { Name = "File1" };
                    file.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
                    var id = file.Id;

                    var node1 = Node.LoadNode(id);
                    var node2 = Node.LoadNode(id);
                    node1.Index = 111;
                    node2.Index = 112;
                    node1.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
                    node2.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
                }
                catch (Exception e)
                {
                    while (e != null)
                    {
                        if (e is NodeIsOutOfDateException)
                            break;
                        e = e.InnerException;
                    }
                    if (e == null)
                        Assert.Fail("The expected NodeIsOutOfDateException was not thrown.");
                }
            });
        }

        [TestMethod]
        public void Timestamp_Growing()
        {
            Test(() =>
            {
                var testRoot = new SystemFolder(Repository.Root) { Name = "Folder1" };
                testRoot.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();

                var content = Content.CreateNew("File", testRoot, "File1");
                var handler = (GenericContent)content.ContentHandler;
                handler.VersioningMode = VersioningType.MajorAndMinor;
                content.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
                var id = content.Id;
                var timestamp = content.ContentHandler.NodeTimestamp;

                content.ContentHandler.Index++;
                content.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
                Assert.IsTrue(content.ContentHandler.NodeTimestamp > timestamp, "Timestamp is not greater after Save");
                timestamp = content.ContentHandler.NodeTimestamp;

                content.CheckOutAsync(CancellationToken.None).GetAwaiter().GetResult();
                Assert.IsTrue(content.ContentHandler.NodeTimestamp > timestamp, "Timestamp is not greater after CheckOut");
                timestamp = content.ContentHandler.NodeTimestamp;

                content.UndoCheckOutAsync(CancellationToken.None).GetAwaiter().GetResult();
                Assert.IsTrue(content.ContentHandler.NodeTimestamp > timestamp, "Timestamp is not greater after UndoCheckOut");
                timestamp = content.ContentHandler.NodeTimestamp;

                content.CheckOutAsync(CancellationToken.None).GetAwaiter().GetResult();
                Assert.IsTrue(content.ContentHandler.NodeTimestamp > timestamp, "Timestamp is not greater after CheckOut #2");
                timestamp = content.ContentHandler.NodeTimestamp;

                content.ContentHandler.Index++;
                content.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
                Assert.IsTrue(content.ContentHandler.NodeTimestamp > timestamp, "Timestamp is not greater after Save #2");
                timestamp = content.ContentHandler.NodeTimestamp;

                content.CheckInAsync(CancellationToken.None).GetAwaiter().GetResult();
                Assert.IsTrue(content.ContentHandler.NodeTimestamp > timestamp, "Timestamp is not greater after CheckIn");
                timestamp = content.ContentHandler.NodeTimestamp;

                content.PublishAsync(CancellationToken.None).GetAwaiter().GetResult();
                Assert.IsTrue(content.ContentHandler.NodeTimestamp > timestamp, "Timestamp is not greater after Publish");
                timestamp = content.ContentHandler.NodeTimestamp;
            });
        }
    }
}
