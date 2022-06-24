using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.InMemory;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Search;
using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.DataModel;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;
using SenseNet.Security;
using SenseNet.Security.Data;
using SenseNet.Testing;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.Tests.Core.Tests
{
    [TestClass]
    public class InitialDataTests : TestBase
    {
        //[TestMethod]
        public async Task InitialData_Core_RebuildIndex()
        {
            if (SnTrace.SnTracers.Count != 2)
                SnTrace.SnTracers.Add(new SnDebugViewTracer());

            await Test(async () =>
            {
                using (var op = SnTrace.Test.StartOperation("@@ ========= Populate"))
                {
                    try
                    {
                        await Providers.Instance.SearchManager.GetIndexPopulator().RebuildIndexDirectlyAsync("/Root",
                            CancellationToken.None, IndexRebuildLevel.DatabaseAndIndex).ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
                        Assert.Fail();
                    }
                    op.Successful = true;
                }

                InitialData.Save(@"D:\_InitialData", null,
                    () => ((InMemoryDataProvider)Providers.Instance.DataStore.DataProvider).DB.Nodes.Select(x => x.NodeId));

                var index = ((InMemorySearchEngine)Providers.Instance.SearchEngine).Index;
                index.Save(@"D:\_InitialData\index.txt");
            });
        }

        //[TestMethod]
        public void InitialData_Core_Create()
        {
            Test(() =>
            {
                InitialData.Save(@"D:\_InitialData", null,
                    () => ((InMemoryDataProvider)Providers.Instance.DataStore.DataProvider).DB.Nodes.Select(x => x.NodeId));

                var index = ((InMemorySearchEngine)Providers.Instance.SearchEngine).Index;
                index.Save(@"D:\index.txt");
            });
            Assert.Inconclusive();
        }
        //[TestMethod]
        public void InitialData_Core_Parse()
        {
            var initialData = InitialData.Load(@"D:\_InitialData");
            
            Assert.IsTrue(initialData.Nodes.Any());
            Assert.Inconclusive();
        }
        //[TestMethod]
        public void InitialData_Core_LoadIndex()
        {
            Test(() =>
            {
                var index = ((InMemorySearchEngine)Providers.Instance.SearchEngine).Index;
                index.Save(@"D:\_InitialData\index.txt");

                var loaded = new InMemoryIndex();
                loaded.Load(@"D:\index.txt");

                loaded.Save(@"D:\_InitialData\index1.txt");
            });
            Assert.Inconclusive();
        }
        //[TestMethod]
        public void InitialData_Core_CtdLoad()
        {
            InitialDataTest(() =>
            {
                var fileContentType = ContentType.GetByName("File");
                var ctd = RepositoryTools.GetStreamString(fileContentType.Binary.GetStream());
                Assert.IsNotNull(ctd);
                Assert.IsTrue(ctd.Length > 10);
            });
        }

        [TestMethod]
        public void InitialData_Core_Permissions_Parse()
        {
            var src = new List<string>
            {
                "+2|Normal|+1:_____________________________________________+++++++++++++++++++",
                // break is irrelevant because this is the second entry.
                "-2|Normal|+7:_____________________________________________+++++++++++++++++++",
                "+6|Normal|-6:_______________________________________________________________+",
                // break is relevant because this is the first entry.
                "-1000|Normal|+6:_______________________________________________________________+",
            };
            InitialDataTest(() =>
            {
                var actions = SecurityInstaller.ParseInitialPermissions(
                    Providers.Instance.SecurityHandler.SecurityContext, src).ToArray();

                Assert.AreEqual(3, actions.Length);

                Assert.IsFalse(actions[0].Break);
                Assert.IsFalse(actions[0].Unbreak);
                Assert.AreEqual(2, actions[0].Entries.Count);
                Assert.AreEqual(2, actions[0].Entries[0].EntityId);
                Assert.AreEqual(1, actions[0].Entries[0].IdentityId);
                Assert.AreEqual(0x07FFFFUL, actions[0].Entries[0].AllowBits);
                Assert.AreEqual(0UL, actions[0].Entries[0].DenyBits);
                Assert.IsFalse(actions[0].Entries[0].LocalOnly);
                Assert.AreEqual(2, actions[0].Entries[1].EntityId);
                Assert.AreEqual(7, actions[0].Entries[1].IdentityId);
                Assert.AreEqual(0x07FFFFUL, actions[0].Entries[1].AllowBits);
                Assert.AreEqual(0UL, actions[0].Entries[1].DenyBits);
                Assert.IsFalse(actions[0].Entries[1].LocalOnly);

                Assert.IsFalse(actions[1].Break);
                Assert.IsFalse(actions[1].Unbreak);
                Assert.AreEqual(1, actions[1].Entries.Count);
                Assert.AreEqual(6, actions[1].Entries[0].EntityId);
                Assert.AreEqual(6, actions[1].Entries[0].IdentityId);
                Assert.AreEqual(1UL, actions[1].Entries[0].AllowBits);
                Assert.AreEqual(0UL, actions[1].Entries[0].DenyBits);
                Assert.IsTrue(actions[1].Entries[0].LocalOnly);

                Assert.IsTrue(actions[2].Break);
                Assert.IsFalse(actions[2].Unbreak);
                Assert.AreEqual(1, actions[2].Entries.Count);
                Assert.AreEqual(1000, actions[2].Entries[0].EntityId);
                Assert.AreEqual(6, actions[2].Entries[0].IdentityId);
                Assert.AreEqual(1UL, actions[2].Entries[0].AllowBits);
                Assert.AreEqual(0UL, actions[2].Entries[0].DenyBits);
                Assert.IsFalse(actions[2].Entries[0].LocalOnly);
            });
        }
        [TestMethod]
        public void InitialData_Core_Permissions_Apply()
        {
            var initialData = new InitialData
            {
                Permissions = new List<string>
                {
                    "+6|Normal|-6:_______________________________________________________________+",
                    "-1000|Normal|+6:_______________________________________________________________+",
                }
            };

            InitialSecurityDataTest(() =>
            {
                var securityHandler = Providers.Instance.SecurityHandler;

                var mask = PermissionType.GetPermissionMask(PermissionType.BuiltInPermissionTypes);
                Providers.Instance.SecurityHandler.CreateAclEditor()
                    .RemoveExplicitEntries(2)
                    .RemoveExplicitEntries(6)
                    .RemoveExplicitEntries(1000)
                    .Set(2, 7, false, mask, 0UL)
                    .Apply();

                // PRECHECKS
                // Administrators group has 1 entry on the Root.
                Assert.AreEqual(1, Providers.Instance.SecurityHandler.GetExplicitEntries(2, new[] { 7 }).Count);
                // Visitor has no any permission.
                Assert.IsFalse(securityHandler.HasPermission(User.Visitor, 6, PermissionType.See));
                Assert.IsFalse(securityHandler.HasPermission(User.Visitor, 1000, PermissionType.See));
                // There is no break.
                Assert.IsTrue(securityHandler.IsEntityInherited(6));
                Assert.IsTrue(securityHandler.IsEntityInherited(1000));

                // ACTION
                new SecurityInstaller(Providers.Instance.SecurityHandler, Providers.Instance.StorageSchema,
                    Providers.Instance.DataStore).InstallDefaultSecurityStructure(initialData);

                // ASSERT
                // Administrators group has an entry on the Root.
                Assert.AreEqual(1 , Providers.Instance.SecurityHandler.GetExplicitEntries(2, new[] { 7 }).Count);
                // Visitor has See permission on both contents.
                Assert.IsTrue(securityHandler.HasPermission(User.Visitor, 6, PermissionType.See));
                Assert.IsTrue(securityHandler.HasPermission(User.Visitor, 1000, PermissionType.See));
                // The second content is not inherited.
                Assert.IsTrue(securityHandler.IsEntityInherited(6));
                Assert.IsFalse(securityHandler.IsEntityInherited(1000));
            });
        }

        private void InitialSecurityDataTest(Action callback)
        {
            InitialDataTestPrivate(callback, false);
        }
        private void InitialDataTest(Action callback)
        {
            InitialDataTestPrivate(callback, true);
        }
        private void InitialDataTestPrivate(Action callback, bool withSecurity)
        {
            var builder = CreateRepositoryBuilderForTest();

            Indexing.IsOuterSearchEngineEnabled = true;

            Cache.Reset();
            ContentTypeManager.Reset();

            using (Repository.Start(builder))
            {
                using (new SystemAccount())
                {
                    if (withSecurity)
                    {
                        var securityDataProvider = Providers.Instance.SecurityDataProvider;
                        var sdbp = new ObjectAccessor(securityDataProvider);
                        var db = (DatabaseStorage)sdbp.GetFieldOrProperty("Storage");
                        db.Aces.Clear();

                        Providers.Instance.SecurityHandler.CreateAclEditor()
                            .Allow(Identifiers.PortalRootId, Identifiers.AdministratorsGroupId, false,
                                PermissionType.BuiltInPermissionTypes)
                            .Allow(Identifiers.PortalRootId, Identifiers.AdministratorUserId, false,
                                PermissionType.BuiltInPermissionTypes)
                            .Apply();
                    }
                    
                    callback();
                }
            }
        }

        /* ==================================================================================== */

        [TestMethod]
        public void InitialData_Core_GetBlobBytes_Null()
        {
            var path = "/Root/Anything";
            var propertyName = "Binary";
            var data = new InitialData {Blobs = new Dictionary<string, string>()};

            // ACTION
            var bytes = data.GetBlobBytes(path, propertyName);

            // ASSERT
            Assert.IsNotNull(bytes);
            Assert.AreEqual(0, bytes.Length);
        }
        [TestMethod]
        public void InitialData_Core_GetBlobBytes_HexDump_WithoutHeader()
        {
            var path = "/Root/Anything";
            var propertyName = "Binary";
            var data = new InitialData { Blobs = new Dictionary<string, string>() };
            var buffer = new byte[] { 0xFF, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07 };
            var stream = new MemoryStream(buffer);
            var dump = InitialData.GetHexDump(stream);
            data.Blobs.Add($"{propertyName}:{path}", dump);

            // ACTION
            var bytes = data.GetBlobBytes(path, propertyName);

            // ASSERT
            // Without header the hex-dump need to be parsed as text instead of bytes
            Assert.IsNotNull(bytes);
            var expected = InitialData.GetHexDump(buffer);
            var actual = RepositoryTools.GetStreamString(new MemoryStream(bytes));
            Assert.AreEqual(expected, actual);
        }
        [TestMethod, TestCategory("Services")]
        public void InitialData_Core_GetBlobBytes_HexDump_WithHeader_CSrv()
        {
            var path = "/Root/Anything";
            var propertyName = "Binary";
            var data = new InitialData { Blobs = new Dictionary<string, string>() };
            var buffer = new byte[] { 0xFF, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07 };
            var stream = new MemoryStream(buffer);
            var dump = "[bytes]:\r\n" + InitialData.GetHexDump(stream);
            data.Blobs.Add($"{propertyName}:{path}", dump);

            // ACTION
            var bytes = data.GetBlobBytes(path, propertyName);

            // ASSERT
            Assert.IsNotNull(bytes);
            AssertSequenceEqual(buffer, bytes);
        }
        [TestMethod]
        public void InitialData_Core_GetBlobBytes_HexDump_WithHeaderAndBom()
        {
            var path = "/Root/Anything";
            var propertyName = "Binary";
            var data = new InitialData { Blobs = new Dictionary<string, string>() };
            var buffer = new byte[] { 0xFF, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07 };
            var stream = new MemoryStream(buffer);
            var dump = GetBomAsString() + "[bytes]:\r\n" + InitialData.GetHexDump(stream);
            data.Blobs.Add($"{propertyName}:{path}", dump);

            // ACTION
            var bytes = data.GetBlobBytes(path, propertyName);

            // ASSERT
            Assert.IsNotNull(bytes);
            AssertSequenceEqual(buffer, bytes);
        }
        [TestMethod]
        public void InitialData_Core_GetBlobBytes_HexDump_WithHeaderAndNewline()
        {
            var path = "/Root/Anything";
            var propertyName = "Binary";
            var data = new InitialData { Blobs = new Dictionary<string, string>() };
            var buffer = new byte[] { 0xFF, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07 };
            var stream = new MemoryStream(buffer);
            var dump = "[bytes]:\n" + InitialData.GetHexDump(stream);
            data.Blobs.Add($"{propertyName}:{path}", dump);

            // ACTION
            var bytes = data.GetBlobBytes(path, propertyName);

            // ASSERT
            Assert.IsNotNull(bytes);
            AssertSequenceEqual(buffer, bytes);
        }

        [TestMethod]
        public void InitialData_Core_GetBlobBytes_Text_WithoutHeader()
        {
            var path = "/Root/Anything";
            var propertyName = "Binary";
            var data = new InitialData { Blobs = new Dictionary<string, string>() };
            var text = "text content";
            data.Blobs.Add($"{propertyName}:{path}", text);

            // ACTION
            var bytes = data.GetBlobBytes(path, propertyName);

            // ASSERT
            Assert.IsNotNull(bytes);
            var expected = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(text)).ToArray();
            AssertSequenceEqual(expected, bytes);
        }
        [TestMethod]
        public void InitialData_Core_GetBlobBytes_Text_WithHeader()
        {
            var path = "/Root/Anything";
            var propertyName = "Binary";
            var data = new InitialData { Blobs = new Dictionary<string, string>() };
            var text = "text content";
            data.Blobs.Add($"{propertyName}:{path}", "[text]:\r\n" + text);

            // ACTION
            var bytes = data.GetBlobBytes(path, propertyName);

            // ASSERT
            Assert.IsNotNull(bytes);
            var expected = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(text)).ToArray();
            AssertSequenceEqual(expected, bytes);
        }
        [TestMethod]
        public void InitialData_Core_GetBlobBytes_Text_WithHeaderAndBom()
        {
            var path = "/Root/Anything";
            var propertyName = "Binary";
            var data = new InitialData { Blobs = new Dictionary<string, string>() };
            var text = "text content";
            data.Blobs.Add($"{propertyName}:{path}", GetBomAsString() + "[text]:\r\n" + text);

            // ACTION
            var bytes = data.GetBlobBytes(path, propertyName);

            // ASSERT
            Assert.IsNotNull(bytes);
            var expected = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(text)).ToArray();
            AssertSequenceEqual(expected, bytes);
        }
        [TestMethod]
        public void InitialData_Core_GetBlobBytes_Text_WithHeaderAndNewline()
        {
            var path = "/Root/Anything";
            var propertyName = "Binary";
            var data = new InitialData { Blobs = new Dictionary<string, string>() };
            var text = "text content";
            data.Blobs.Add($"{propertyName}:{path}", "[text]:\n"+text);

            // ACTION
            var bytes = data.GetBlobBytes(path, propertyName);

            // ASSERT
            Assert.IsNotNull(bytes);
            var expected = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(text)).ToArray();
            AssertSequenceEqual(expected, bytes);
        }

        private string GetBomAsString()
        {
            return new string(Encoding.UTF8.GetPreamble().Select(x=>(char)x).ToArray());
        }
    }
}
