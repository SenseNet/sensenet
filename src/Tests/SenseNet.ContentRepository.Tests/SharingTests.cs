using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.ContentRepository.Sharing;
using SenseNet.Search;
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
                Level = "Open",
                Mode = "Public",
                ShareDate = DateTime.UtcNow.AddHours(-1),
                CreatorId = 1
            };
            var sd2 = new SharingData
            {
                Token = "abc2@example.com",
                Identity = 42,
                Level = "Edit",
                Mode = "Private",
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
            Assert.AreEqual("Private, Public", values);
            values = string.Join(", ", fieldsSharingLevel.Single().StringArrayValue.OrderBy(x => x));
            Assert.AreEqual("Edit, Open", values);
        }

        [TestMethod]
        public void Sharing_Serialization()
        {
            var sd1 = new SharingData
            {
                Token = "abc1@example.com",
                Identity = 0,
                Level = "Open",
                Mode = "Public",
                ShareDate = new DateTime(2018, 10, 16, 0, 40, 15, DateTimeKind.Utc),
                CreatorId = 1
            };
            var sd2 = new SharingData
            {
                Token = "abc2@example.com",
                Identity = 42,
                Level = "Edit",
                Mode = "Private",
                ShareDate = new DateTime(2018, 10, 16, 0, 40, 16, DateTimeKind.Utc),
                CreatorId = 2
            };

            var sharingItems = new List<SharingData> { sd1, sd2 };

            // ACTION-1
            var serialized = SharingHandler.Serialize(sharingItems);

            // ASSERT-1
            var expected = @"[
  {
    ""Token"": ""abc1@example.com"",
    ""Identity"": 0,
    ""Mode"": ""Public"",
    ""Level"": ""Open"",
    ""CreatorId"": 1,
    ""ShareDate"": ""2018-10-16T00:40:15Z""
  },
  {
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
            for (int i = 0; i < items.Length; i++)
            {
                Assert.AreEqual(sharingItems[i].Token, items[i].Token);
                Assert.AreEqual(sharingItems[i].Identity, items[i].Identity);
                Assert.AreEqual(sharingItems[i].Mode, items[i].Mode);
                Assert.AreEqual(sharingItems[i].Level, items[i].Level);
                Assert.AreEqual(sharingItems[i].CreatorId, items[i].CreatorId);
                Assert.AreEqual(sharingItems[i].ShareDate, items[i].ShareDate);
            }
        }

        [TestMethod]
        public void Sharing_Searchability()
        {
            var levels = new[] { "Open", "Edit" };
            var modes = new[] { "Public", "Authenticated", "Private" };

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
              	        level	mode	content
                sd1	    O		Pu		c1
                sd2	    E		A		c1
                sd3	    O		Pr		c2
                sd4	    E		Pu		c2
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
