using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.ContentRepository.Sharing;
using SenseNet.Search.Indexing;
using SenseNet.Tests;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class SharingTests : TestBase
    {
        [TestMethod]
        public void Sharing_IndexFields()
        {
            // ARRANGE
            var sd1 = new SharingData
            {
                Token = "abc1@example.com",
                Identity = 0,
                Level = SharingLevel.Open,
                Mode = SharingMode.Public,
                ShareDate = DateTime.UtcNow.AddHours(-1),
                CreatorId = 1
            };
            var sd2 = new SharingData
            {
                Token = "abc2@example.com",
                Identity = 42,
                Level = SharingLevel.Edit,
                Mode = SharingMode.Private,
                ShareDate = DateTime.UtcNow.AddHours(-1),
                CreatorId = 2
            };

            var sharingItems = new List<SharingData> { sd1, sd2 };
            var indexHandler = new SharingIndexHandler { OwnerIndexingInfo = new PerFieldIndexingInfo() };

            // ACTION
            var fieldsSharedWith = indexHandler.GetIndexFields("SharedWith", sharingItems).ToArray();
            var fieldsSharedBy = indexHandler.GetIndexFields("SharedBy", sharingItems).ToArray();
            var fieldsSharingMode = indexHandler.GetIndexFields("SharingMode", sharingItems).ToArray();
            var fieldsSharingLevel = indexHandler.GetIndexFields("SharingLevel", sharingItems).ToArray();

            // ASSERT
            var values = string.Join(", ", fieldsSharedWith.Single().StringArrayValue.OrderBy(x => x));
            Assert.AreEqual("0, 42, abc1@example.com, abc2@example.com", values);
            values = string.Join(", ", fieldsSharedBy.Single().StringArrayValue.OrderBy(x => x));
            Assert.AreEqual("1, 2", values);
            values = string.Join(", ", fieldsSharingMode.Single().StringArrayValue.OrderBy(x => x));
            Assert.AreEqual("private, public", values);
            values = string.Join(", ", fieldsSharingLevel.Single().StringArrayValue.OrderBy(x => x));
            Assert.AreEqual("edit, open", values);
        }

        [TestMethod]
        public void Sharing_Serialization()
        {
            var sd1 = new SharingData
            {
                Token = "abc1@example.com",
                Identity = 0,
                Level = SharingLevel.Open,
                Mode = SharingMode.Public,
                ShareDate = new DateTime(2018, 10, 16, 0, 40, 15, DateTimeKind.Utc),
                CreatorId = 1
            };
            var sd2 = new SharingData
            {
                Token = "abc2@example.com",
                Identity = 42,
                Level = SharingLevel.Edit,
                Mode = SharingMode.Private,
                ShareDate = new DateTime(2018, 10, 16, 0, 40, 16, DateTimeKind.Utc),
                CreatorId = 2
            };

            var sharingItems = new List<SharingData> { sd1, sd2 };

            // ACTION-1
            var serialized = SharingHandler.Serialize(sharingItems);

            // ASSERT-1
            var expected = @"[
  {
    ""Id"": """ + sd1.Id + @""",
    ""Token"": ""abc1@example.com"",
    ""Identity"": 0,
    ""Mode"": ""Public"",
    ""Level"": ""Open"",
    ""CreatorId"": 1,
    ""ShareDate"": ""2018-10-16T00:40:15Z""
  },
  {
    ""Id"": """ + sd2.Id + @""",
    ""Token"": ""abc2@example.com"",
    ""Identity"": 42,
    ""Mode"": ""Private"",
    ""Level"": ""Edit"",
    ""CreatorId"": 2,
    ""ShareDate"": ""2018-10-16T00:40:16Z""
  }
]";
            Assert.AreEqual(expected, serialized);


            // ACTION-2
            var deserialized = SharingHandler.Deserialize(serialized);

            // ASSERT-2
            var items = deserialized.OrderBy(x => x.Token).ToArray();
            Assert.AreEqual(2, items.Length);
            for (var i = 0; i < items.Length; i++)
            {
                AssertSharingData(sharingItems[i], items[i]);
            }
        }

        [TestMethod]
        public void Sharing_Searchability()
        {
            var levels = new[] { SharingLevel.Open, SharingLevel.Edit };
            var modes = new[] { SharingMode.Public, SharingMode.Authenticated, SharingMode.Private };

            var sd = new SharingData[4];
            for (var i = 0; i < sd.Length; i++)
            {
                sd[i] = new SharingData
                {
                    Token = $"abc{i}@example.com",
                    Identity = i,
                    Level = levels[i % levels.Length],
                    Mode = modes[i % modes.Length],
                    ShareDate = new DateTime(2018, 10, 16, 0, 40, i, DateTimeKind.Utc),
                    CreatorId = i
                };
            }

            /*
              	        level	mode	content identity
                sd1	    O		Pu		c1      0
                sd2	    E		A		c1      1
                sd3	    O		Pr		c2      2
                sd4	    E		Pu		c2      3
             */

            Test(() =>
            {
                //UNDONE: update genericcontent CTD in the test structure
                // ...so we do not have to update it here manually
                ReInstallGenericContentCtd();
                var root = CreateTestRoot();

                var content = Content.CreateNew(nameof(GenericContent), root, "Document-1");
                var gc = (GenericContent)content.ContentHandler;
                gc.SharingData = SharingHandler.Serialize(new[] { sd[0], sd[1] });
                content.Save();
                var id1 = content.Id;

                content = Content.CreateNew(nameof(GenericContent), root, "Document-2");
                gc = (GenericContent)content.ContentHandler;
                gc.SharingData = SharingHandler.Serialize(new[] { sd[2], sd[3] });
                content.Save();
                var id2 = content.Id;

                //SaveIndex(@"D:\_index");

                // TESTS
                Assert.AreEqual($"{id1}, {id2}", GetQueryResult($"+InTree:{root.Path} +SharingMode:{modes[0]}"));
                Assert.AreEqual($"{id1}"/*   */, GetQueryResult($"+InTree:{root.Path} +SharingMode:{modes[1]}"));
                Assert.AreEqual($"{id2}"/*   */, GetQueryResult($"+InTree:{root.Path} +SharingMode:{modes[2]}"));

                Assert.AreEqual($"{id1}, {id2}", GetQueryResult($"+InTree:{root.Path} +SharingLevel:{levels[0]}"));
                Assert.AreEqual($"{id1}, {id2}", GetQueryResult($"+InTree:{root.Path} +SharingLevel:{levels[1]}"));

                //FAIL!!!!!!!!!!!!!!!!
                Assert.AreEqual($"{id1}", GetQueryResult($"+InTree:{root.Path} +SharingMode:{modes[0]} +SharingLevel:{levels[0]}"));
            });
        }

        [TestMethod]
        public void Sharing_AddSharing()
        {
            Test(() =>
            {
                //UNDONE: update genericcontent CTD in the test structure
                // ...so we do not have to update it here manually
                ReInstallGenericContentCtd();
                var root = CreateTestRoot();

                var content = Content.CreateNew(nameof(GenericContent), root, "Document-1");
                content.Save();

                var gc = (GenericContent)content.ContentHandler;

                gc.Sharing.Share("abc1@example.com", SharingLevel.Open, SharingMode.Public);
                gc.Sharing.Share("abc2@example.com", SharingLevel.Edit, SharingMode.Private);

                var id1 = content.Id;

                Assert.AreEqual($"{id1}", GetQueryResult($"+InTree:{root.Path} +SharedWith:abc1@example.com"));
                Assert.AreEqual($"{id1}", GetQueryResult($"+InTree:{root.Path} +SharedWith:abc2@example.com"));
            });
        }
        [TestMethod]
        public void Sharing_RemoveSharing()
        {
            Test(() =>
            {
                //UNDONE: update genericcontent CTD in the test structure
                // ...so we do not have to update it here manually
                ReInstallGenericContentCtd();
                var root = CreateTestRoot();

                var content = Content.CreateNew(nameof(GenericContent), root, "Document-1");
                content.Save();

                var gc = (GenericContent)content.ContentHandler;

                var sd1 = gc.Sharing.Share("abc1@example.com", SharingLevel.Open, SharingMode.Public);

                Assert.AreEqual($"{content.Id}", GetQueryResult($"+InTree:{root.Path} +SharedWith:abc1@example.com"));

                gc.Sharing.RemoveSharing(sd1.Id);

                Assert.AreEqual(string.Empty, GetQueryResult($"+InTree:{root.Path} +SharedWith:abc1@example.com"));
            });
        }
        [TestMethod]
        public void Sharing_GetSharing()
        {
            Test(() =>
            {
                //UNDONE: update genericcontent CTD in the test structure
                // ...so we do not have to update it here manually
                ReInstallGenericContentCtd();
                var root = CreateTestRoot();

                var content = Content.CreateNew(nameof(GenericContent), root, "Document-1");
                content.Save();

                var gc = (GenericContent)content.ContentHandler;

                var sd1 = gc.Sharing.Share("abc1@example.com", SharingLevel.Open, SharingMode.Public);
                var sd2 = gc.Sharing.Share("abc2@example.com", SharingLevel.Edit, SharingMode.Private);

                // order items to make asserts simpler
                var items = gc.Sharing.Items.OrderBy(sd => sd.Token).ToArray();

                Assert.AreEqual(2, items.Length);

                AssertSharingData(sd1, items[0]);
                AssertSharingData(sd2, items[1]);
            });
        }

        private void ReInstallGenericContentCtd()
        {
            var path = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                @"..\..\..\..\nuget\snadmin\install-services\import\System\Schema\ContentTypes\GenericContentCtd.xml"));
            using (var stream = new FileStream(path, FileMode.Open))
                ContentTypeInstaller.InstallContentType(stream);
        }

        private string GetQueryResult(string cql)
        {
            var result = string.Join(", ",
                CreateSafeContentQuery(cql)
                .Execute()
                .Identifiers
                .Select(x => x.ToString())
                .ToArray());
            return result;
        }

        private static void AssertSharingData(SharingData sd1, SharingData sd2)
        {
            Assert.AreEqual(sd1.Id, sd2.Id);
            Assert.AreEqual(sd1.Token, sd2.Token);
            Assert.AreEqual(sd1.Identity, sd2.Identity);
            Assert.AreEqual(sd1.CreatorId, sd2.CreatorId);
            Assert.AreEqual(sd1.Level, sd2.Level);
            Assert.AreEqual(sd1.Mode, sd2.Mode);
            Assert.AreEqual(sd1.ShareDate, sd2.ShareDate);
        }

        #region /* ================================================================================================ Tools */

        private GenericContent CreateTestRoot()
        {
            var node = new SystemFolder(Repository.Root) { Name = "TestRoot" };
            node.Save();
            return node;
        }

        #endregion
    }
}
