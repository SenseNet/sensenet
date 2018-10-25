using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Linq;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.ContentRepository.Sharing;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Portal.OData.Metadata;
using SenseNet.Search;
using SenseNet.Search.Indexing;
using SenseNet.Search.Querying;
using SenseNet.Search.Querying.Parser.Predicates;
using SenseNet.Security;
using SenseNet.Tests;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class SharingTests : TestBase
    {
        [TestMethod]
        public void Sharing_Indexing_Tokenizer()
        {
            var data = new SharingData
            {
                Token = "ABC1@eXample.com",
                Identity = 0,
                CreatorId = 1,
                Level = SharingLevel.Edit,
                Mode = SharingMode.Public,
                ShareDate = DateTime.UtcNow.AddHours(-1),
            };

            // ACTION
            var tokenizer = SharingDataTokenizer.Tokenize(data);

            // ASSERT
            Assert.AreEqual("Tabc1@example.com", tokenizer.Token);
            Assert.AreEqual("I0", tokenizer.Identity);
            Assert.AreEqual("C1", tokenizer.CreatorId);
            Assert.AreEqual("L1", tokenizer.Level);
            Assert.AreEqual("M0", tokenizer.Mode);
        }
        [TestMethod]
        public void Sharing_Indexing_Combinator()
        {
            var data = new SharingData
            {
                Token = "ABC1@eXample.com",
                Identity = 0,
                CreatorId = 1,
                Level = SharingLevel.Edit,
                Mode = SharingMode.Public,
                ShareDate = DateTime.UtcNow.AddHours(-1),
            };
            var tokenizer = SharingDataTokenizer.Tokenize(data);

            // ACTION
            var cmbinations = tokenizer.GetCombinations();

            var expected = GetAvailableCombinations(data).OrderBy(x => x);
            var actual = cmbinations.OrderBy(x => x);
            AssertSequenceEqual(expected, actual);
        }
        [TestMethod]
        public void Sharing_Indexing_IndexFields()
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
                CreatorId = 2,
                Level = SharingLevel.Edit,
                Mode = SharingMode.Private,
                ShareDate = DateTime.UtcNow.AddHours(-1),
            };

            var sharingItems = new List<SharingData> { sd1, sd2 };
            var indexHandler = new SharingIndexHandler { OwnerIndexingInfo = new PerFieldIndexingInfo() };

            // ACTION
            var indexFields = indexHandler.GetIndexFields("Sharing", sharingItems);

            // ASSERT
            var indexField = indexFields.Single();
            Assert.AreEqual("Sharing", indexField.Name);

            var expected = GetAvailableCombinations(sd1)
                .Union(GetAvailableCombinations(sd2)).OrderBy(x => x);
            var values = indexField.StringArrayValue.OrderBy(x => x);
            AssertSequenceEqual(expected, values);
        }
        private string[] GetAvailableCombinations(SharingData data)
        {
            var tokenizer = SharingDataTokenizer.Tokenize(data);

            var a = tokenizer.Token;
            var b = tokenizer.Identity;
            var c = tokenizer.CreatorId;
            var d = tokenizer.Mode;
            var e = tokenizer.Level;

            return new[]
            {
                a, b, c, d, e,

                $"{a},{b}", $"{a},{c}", $"{a},{d}", $"{a},{e}",
                $"{b},{c}", $"{b},{d}", $"{b},{e}",
                $"{c},{d}", $"{c},{e}",
                $"{d},{e}",

                $"{a},{b},{c}",$"{a},{b},{d}",$"{a},{b},{e}",$"{a},{c},{d}",$"{a},{c},{e}",$"{a},{d},{e}",
                $"{b},{c},{d}",$"{b},{c},{e}",$"{b},{d},{e}",
                $"{c},{d},{e}",

                $"{a},{b},{c},{d}",$"{a},{b},{c},{e}",$"{a},{b},{d},{e}",$"{a},{c},{d},{e}",$"{b},{c},{d},{e}",

                $"{a},{b},{c},{d},{e}"
            };
        }
        [TestMethod]
        public void Sharing_Indexing_CheckByRawQuery()
        {
            var sd1 = new SharingData
            {
                Token = "abc1@example.com",
                Identity = 0,
                Mode = SharingMode.Public,
                Level = SharingLevel.Edit,
                ShareDate = DateTime.UtcNow.AddHours(-1),
                CreatorId = 1
            };

            var sharingItems = new List<SharingData> { sd1 };

            Test(() =>
            {
                ReInstallGenericContentCtd();
                var root = CreateTestRoot();

                var content = Content.CreateNew(nameof(GenericContent), root, "Document-1");
                var gc = (GenericContent)content.ContentHandler;
                gc.SharingData = SharingHandler.Serialize(new[] { sd1 });
                content.Save();
                var id1 = content.Id;

                // TESTS
                Assert.AreEqual($"{id1}", GetQueryResult($"+InTree:{root.Path} +Sharing:Tabc1@example.com"));
                Assert.AreEqual($"{id1}", GetQueryResult($"+InTree:{root.Path} +Sharing:I0"));
                Assert.AreEqual($"{id1}", GetQueryResult($"+InTree:{root.Path} +Sharing:C1"));
                Assert.AreEqual($"{id1}", GetQueryResult($"+InTree:{root.Path} +Sharing:M0"));
                Assert.AreEqual($"{id1}", GetQueryResult($"+InTree:{root.Path} +Sharing:L1"));

                Assert.AreEqual($"{id1}", GetQueryResult($"+InTree:{root.Path} +Sharing:tabc1@example.com,i0"));
                Assert.AreEqual($"{id1}", GetQueryResult($"+InTree:{root.Path} +Sharing:tabc1@example.com,i0,c1"));
                Assert.AreEqual($"{id1}", GetQueryResult($"+InTree:{root.Path} +Sharing:tabc1@example.com,i0,c1,m0"));
                Assert.AreEqual($"{id1}", GetQueryResult($"+InTree:{root.Path} +Sharing:tabc1@example.com,i0,c1,m0,l1"));
            });

        }

        [TestMethod]
        public void Sharing_Query_Tokenize()
        {
            Test(() =>
            {
                ReInstallGenericContentCtd();

                var user = new User(Node.LoadNode("/Root/IMS/BuiltIn/Portal"))
                {
                    Name = "User-1",
                    Enabled = true,
                    Email = "user1@example.com"
                };
                user.Save();
                var qctx = new SnQueryContext(QuerySettings.Default, User.Current.Id);

                var queries = new[]
                {
                    SnQuery.Parse("+SharedWith:user1@example.com", qctx),
                    SnQuery.Parse($"+SharedWith:{user.Id}", qctx),
                    SnQuery.Parse("+SharedBy:admin", qctx),
                    SnQuery.Parse("+SharingMode:Private", qctx),
                    SnQuery.Parse("+SharingLevel:Edit", qctx),
                };
                var expected = new []
                {
                    "+SharedWith:Tuser1@example.com",
                    $"+SharedWith:{SharingDataTokenizer.TokenizeIdentity(user.Id)}",
                    "+SharedBy:C1",
                    "+SharingMode:M2",
                    "+SharingLevel:L1"
                };

                for (var i = 0; i < queries.Length; i++)
                    Assert.AreEqual(expected[i], queries[i].ToString());


                //var visitor = new SharingVisitor();
                //var rewritten = SnQuery.Create(visitor.Visit(snQuery.QueryTree));
                //Assert.AreEqual("+Sharing:M2,L1", rewritten.ToString());
            });
        }
        [TestMethod]
        public void Sharing_Query_RewriteOne()
        {
            Test(() =>
            {
                ReInstallGenericContentCtd();

                var user = new User(Node.LoadNode("/Root/IMS/BuiltIn/Portal"))
                {
                    Name = "User-1",
                    Enabled = true,
                    Email = "user1@example.com"
                };
                user.Save();

                RewritingTest("+SharedWith:user1@example.com", "+Sharing:Tuser1@example.com");
                RewritingTest($"+SharedWith:{user.Id}", $"+Sharing:{SharingDataTokenizer.TokenizeIdentity(user.Id)}");
                RewritingTest("+SharedBy:admin", "+Sharing:C1");
                RewritingTest("+SharingMode:Private", "+Sharing:M2");
                RewritingTest("+SharingLevel:Edit", "+Sharing:L1");
            });
        }
        [TestMethod]
        public void Sharing_Query_RewriteOnlyOneLevelMust()
        {
            Test(() =>
            {
                ReInstallGenericContentCtd();

                var user = new User(Node.LoadNode("/Root/IMS/BuiltIn/Portal"))
                {
                    Name = "User-1",
                    Enabled = true,
                    Email = "user1@example.com"
                };
                user.Save();

                var qA = "SharedWith:user1@example.com";
                var qB = $"SharedWith:{user.Id}";
                var qC = "SharedBy:admin";
                var qD = "SharingMode:Private";
                var qE = "SharingLevel:Edit";

                var tA = "Tuser1@example.com";
                var tB = SharingDataTokenizer.TokenizeIdentity(user.Id);
                var tC = "C1";
                var tD = "M2";
                var tE = "L1";

                RewritingTest($"+TypeIs:file +{qA} +{qB}", $"+TypeIs:file +Sharing:{tA},{tB}");

                RewritingTest($"+{qA} +{qB}", $"+Sharing:{tA},{tB}");
                RewritingTest($"+{qA} +{qC}", $"+Sharing:{tA},{tC}");
                RewritingTest($"+{qA} +{qD}", $"+Sharing:{tA},{tD}");
                RewritingTest($"+{qA} +{qE}", $"+Sharing:{tA},{tE}");
                RewritingTest($"+{qB} +{qC}", $"+Sharing:{tB},{tC}");
                RewritingTest($"+{qB} +{qD}", $"+Sharing:{tB},{tD}");
                RewritingTest($"+{qB} +{qE}", $"+Sharing:{tB},{tE}");
                RewritingTest($"+{qC} +{qD}", $"+Sharing:{tC},{tD}");
                RewritingTest($"+{qC} +{qE}", $"+Sharing:{tC},{tE}");
                RewritingTest($"+{qD} +{qE}", $"+Sharing:{tD},{tE}");
            });
        }
        private void RewritingTest(string queryText, string expectedRewritten)
        {
            var qctx = new SnQueryContext(QuerySettings.Default, User.Current.Id);
            var query = SnQuery.Parse(queryText, qctx);
            var visitor = new SharingVisitor();
            var rewritten = SnQuery.Create(visitor.Visit(query.QueryTree));
            Assert.AreEqual(expectedRewritten, rewritten.ToString());
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
                AssertSharingDataAreEqual(sharingItems[i], items[i]);
            }
        }

        [TestMethod]
        public void Sharing_Query_Searchability()
        {
            var levels = Enum.GetValues(typeof(SharingLevel)).Cast<SharingLevel>().ToArray();
            var modes = Enum.GetValues(typeof(SharingMode)).Cast<SharingMode>().ToArray();

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
                
                Assert.AreEqual($"{id1}", GetQueryResult($"+InTree:{root.Path} +SharingMode:{modes[0]} +SharingLevel:{levels[0]}"));

                Assert.AreEqual($"{id1}"/*   */, GetQueryResult($"+InTree:{root.Path} +SharedWith:{sd[0].Identity}"));
                Assert.AreEqual($"{id2}"/*   */, GetQueryResult($"+InTree:{root.Path} +SharedWith:{sd[2].Identity}"));
            });
        }

        [TestMethod]
        public void Sharing_AddSharing()
        {
            Test(() =>
            {
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

                AssertSharingDataAreEqual(sd1, items[0]);
                AssertSharingDataAreEqual(sd2, items[1]);
            });
        }

        [TestMethod]
        public void Sharing_Delete_User()
        {
            // we need the sharing observer for this feature
            Test(builder => { builder.EnableNodeObservers(typeof(SharingNodeObserver)); }, () =>
            {
                ReInstallGenericContentCtd();
                var root = CreateTestRoot();

                var user = new User(Node.LoadNode("/Root/IMS/BuiltIn/Portal"))
                {
                    Name = "User-1",
                    Enabled = true,
                    Email = "user1@example.com"
                };
                user.Save();

                root.Sharing.Share("user1@example.com", SharingLevel.Open, SharingMode.Private, false);
                root.Sharing.Share("user2@example.com", SharingLevel.Open, SharingMode.Public, false); // external user

                var items = root.Sharing.Items.ToArray();

                Assert.AreEqual(2, items.Length);
                // internal user
                Assert.IsNotNull(items.Single(sd => sd.Token == "user1@example.com" && sd.Identity == user.Id));
                // external user
                Assert.IsNotNull(items.Single(sd => sd.Token == "user2@example.com" && sd.Identity == 0));

                // ACTION: sharing records that belong to the deleted identity should be removed.
                user.ForceDelete();

                // reload the shared content to refresh the sharing list
                root = Node.Load<GenericContent>(root.Id);
                items = root.Sharing.Items.ToArray();

                Assert.AreEqual(1, items.Length);
                // internal user
                Assert.IsNull(items.FirstOrDefault(sd => sd.Token == "user1@example.com" || sd.Identity == user.Id));
                // external user
                Assert.IsNotNull(items.Single(sd => sd.Token == "user2@example.com" && sd.Identity == 0));
            });
        }
        [TestMethod]
        public void Sharing_Delete_Group()
        {
            // we need the sharing observer for this feature
            Test(builder => { builder.EnableNodeObservers(typeof(SharingNodeObserver)); }, () =>
            {
                ReInstallGenericContentCtd();
                var root = CreateTestRoot();

                var user = new User(Node.LoadNode("/Root/IMS/BuiltIn/Portal"))
                {
                    Name = "User-1",
                    Enabled = true,
                    Email = "user1@example.com"
                };
                user.Save();
                var group = new Group(Node.LoadNode("/Root/IMS/BuiltIn/Portal"))
                {
                    Name = "Group-1"
                };
                group.Save();

                // add user sharing the official way
                root.Sharing.Share("user1@example.com", SharingLevel.Open, SharingMode.Private, false);

                // add group sharing manually, because the current api does not support group sharing
                var items = new List<SharingData>(root.Sharing.Items)
                {
                    new SharingData
                    {
                        Id = Guid.NewGuid().ToString(),
                        Token = string.Empty,
                        Identity = group.Id,
                        Level = SharingLevel.Edit,
                        Mode = SharingMode.Authenticated,
                        CreatorId = 1
                    }
                };

                root.SharingData = SharingHandler.Serialize(items);
                root.Save(SavingMode.KeepVersion);

                items = root.Sharing.Items.ToList();

                Assert.AreEqual(2, items.Count);
                Assert.IsNotNull(items.Single(sd => sd.Identity == user.Id));
                Assert.IsNotNull(items.Single(sd => sd.Identity == group.Id));

                // ACTION: sharing records that belong to the deleted identity should be removed.
                group.ForceDelete();

                // reload the shared content to refresh the sharing list
                root = Node.Load<GenericContent>(root.Id);
                items = root.Sharing.Items.ToList();

                // user record still exists, the group record should be deleted
                Assert.AreEqual(1, items.Count);
                Assert.IsNotNull(items.Single(sd => sd.Identity == user.Id));
                Assert.IsNull(items.FirstOrDefault(sd => sd.Identity == group.Id));
            });
        }

        [TestMethod]
        public void Sharing_Permissions_OpenBitmasks()
        {
            var levels = Enum.GetValues(typeof(SharingLevel)).Cast<SharingLevel>().ToArray();
            var bitmasks = levels.ToDictionary(x => x, SharingHandler.GetEffectiveBitmask);

            Assert.AreEqual(0x1Ful, bitmasks[SharingLevel.Open]);
            Assert.AreEqual(0x7Ful, bitmasks[SharingLevel.Edit]);
        }
        [TestMethod]
        public void Sharing_Permissions_AddSharing()
        {
            Test(true, () =>
            {
                PrepareForPermissionTest(out var gc, out var user1);

                // ACTION
                gc.Sharing.Share("abc1@example.com", SharingLevel.Open, SharingMode.Public);
                var entries1 = gc.Sharing.GetExplicitEntries();

                gc.Sharing.Share("abc2@example.com", SharingLevel.Open, SharingMode.Authenticated);
                var entries2 = gc.Sharing.GetExplicitEntries();

                gc.Sharing.Share(user1.Email, SharingLevel.Edit, SharingMode.Private);
                var entries3 = gc.Sharing.GetExplicitEntries().OrderBy(e => e.IdentityId).ToList();

                // ASSERT
                Assert.AreEqual(0, entries1.Count);

                Assert.AreEqual(1, entries2.Count);
                var entry = entries2.Single();
                Assert.AreEqual(EntryType.Sharing, entry.EntryType);
                Assert.AreEqual(Group.Everyone.Id, entry.IdentityId);
                Assert.AreEqual(SharingHandler.GetEffectiveBitmask(SharingLevel.Open), entry.AllowBits);
                Assert.AreEqual(0ul, entry.DenyBits);

                Assert.AreEqual(2, entries3.Count);
                entry = entries3[1];
                Assert.AreEqual(EntryType.Sharing, entry.EntryType);
                Assert.AreEqual(user1.Id, entry.IdentityId);
                Assert.AreEqual(SharingHandler.GetEffectiveBitmask(SharingLevel.Edit), entry.AllowBits);
                Assert.AreEqual(0ul, entry.DenyBits);
            });
        }
        [TestMethod]
        public void Sharing_Permissions_MoreSharing()
        {
            Test(true, () =>
            {
                PrepareForPermissionTest(out var gc, out var user1);

                // ACTION
                gc.Sharing.Share(user1.Email, SharingLevel.Open, SharingMode.Private);
                var entries1 = gc.Sharing.GetExplicitEntries().OrderBy(e => e.IdentityId).ToList();
                gc.Sharing.Share(user1.Email, SharingLevel.Edit, SharingMode.Private);
                var entries2 = gc.Sharing.GetExplicitEntries().OrderBy(e => e.IdentityId).ToList();

                // ASSERT
                Assert.AreEqual(1, entries1.Count);
                var entry = entries1.Single();
                Assert.AreEqual(EntryType.Sharing, entry.EntryType);
                Assert.AreEqual(user1.Id, entry.IdentityId);
                Assert.AreEqual(SharingHandler.GetEffectiveBitmask(SharingLevel.Open), entry.AllowBits);
                Assert.AreEqual(0ul, entry.DenyBits);

                Assert.AreEqual(1, entries2.Count);
                entry = entries2.Single();
                Assert.AreEqual(EntryType.Sharing, entry.EntryType);
                Assert.AreEqual(user1.Id, entry.IdentityId);
                Assert.AreEqual(SharingHandler.GetEffectiveBitmask(SharingLevel.Edit), entry.AllowBits);
                Assert.AreEqual(0ul, entry.DenyBits);
            });
        }
        [TestMethod]
        public void Sharing_Permissions_DeleteSharing()
        {
            Test(true, () =>
            {
                PrepareForPermissionTest(out var gc, out var user1);
                var sharing = new[]
                {
                    gc.Sharing.Share(user1.Email, SharingLevel.Open, SharingMode.Private), // 0
                    gc.Sharing.Share("user2@example.com", SharingLevel.Open, SharingMode.Authenticated), // 1
                    gc.Sharing.Share("user3@example.com", SharingLevel.Open, SharingMode.Authenticated), // 2
                    gc.Sharing.Share("user4@example.com", SharingLevel.Edit, SharingMode.Authenticated), // 3
                    gc.Sharing.Share("user5@example.com", SharingLevel.Edit, SharingMode.Authenticated), // 4
                };

                // ------------------------------------ ACTION-1
                gc.Sharing.RemoveSharing(sharing[4].Id);

                // ASSERT-1 everyone's entry allows edit
                AssertSequenceEqual(
                    sharing.Take(4).Select(x => x.Token),
                    gc.Sharing.Items.OrderBy(x => x.Token).Select(x => x.Token));

                var entries = gc.Sharing.GetExplicitEntries().OrderBy(e => e.IdentityId).ToArray();
                Assert.AreEqual(2, entries.Length);
                // everyone group
                Assert.AreEqual(Identifiers.EveryoneGroupId, entries[0].IdentityId);
                Assert.AreEqual(SharingHandler.GetEffectiveBitmask(SharingLevel.Edit), entries[0].AllowBits);
                // user
                Assert.AreEqual(user1.Id, entries[1].IdentityId);
                Assert.AreEqual(SharingHandler.GetEffectiveBitmask(SharingLevel.Open), entries[1].AllowBits);

                // ------------------------------------ ACTION-2
                gc.Sharing.RemoveSharing(sharing[3].Id);

                // ASSERT-2 everyone's entry allows open
                AssertSequenceEqual(
                    sharing.Take(3).Select(x => x.Token),
                    gc.Sharing.Items.OrderBy(x => x.Token).Select(x => x.Token));

                entries = gc.Sharing.GetExplicitEntries().OrderBy(e => e.IdentityId).ToArray();
                Assert.AreEqual(2, entries.Length);
                // everyone group
                Assert.AreEqual(Identifiers.EveryoneGroupId, entries[0].IdentityId);
                Assert.AreEqual(SharingHandler.GetEffectiveBitmask(SharingLevel.Open), entries[0].AllowBits);
                // user
                Assert.AreEqual(user1.Id, entries[1].IdentityId);
                Assert.AreEqual(SharingHandler.GetEffectiveBitmask(SharingLevel.Open), entries[1].AllowBits);

                // ------------------------------------ ACTION-3
                gc.Sharing.RemoveSharing(sharing[2].Id);

                // ASSERT-3 everyone's entry allows open
                AssertSequenceEqual(
                    sharing.Take(2).Select(x => x.Token),
                    gc.Sharing.Items.OrderBy(x => x.Token).Select(x => x.Token));

                entries = gc.Sharing.GetExplicitEntries().OrderBy(e => e.IdentityId).ToArray();
                Assert.AreEqual(2, entries.Length);
                // everyone group
                Assert.AreEqual(Identifiers.EveryoneGroupId, entries[0].IdentityId);
                Assert.AreEqual(SharingHandler.GetEffectiveBitmask(SharingLevel.Open), entries[0].AllowBits);
                // user
                Assert.AreEqual(user1.Id, entries[1].IdentityId);
                Assert.AreEqual(SharingHandler.GetEffectiveBitmask(SharingLevel.Open), entries[1].AllowBits);

                // ------------------------------------ ACTION-4
                gc.Sharing.RemoveSharing(sharing[1].Id);

                // ASSERT-4 everyone's entry removed
                AssertSequenceEqual(
                    sharing.Take(1).Select(x => x.Token),
                    gc.Sharing.Items.OrderBy(x => x.Token).Select(x => x.Token));

                entries = gc.Sharing.GetExplicitEntries().ToArray();
                Assert.AreEqual(1, entries.Length);
                // user
                Assert.AreEqual(user1.Id, entries[0].IdentityId);
                Assert.AreEqual(SharingHandler.GetEffectiveBitmask(SharingLevel.Open), entries[0].AllowBits);
            });
        }

        [TestMethod]
        public void Sharing_Permissions_Explicit()
        {
            Test(() => {
                PrepareForPermissionTest(out var gc, out var user1);

                // set some security entries on focused document
                SnSecurityContext.Create().CreateAclEditor()
                    .Allow(gc.Id, user1.Id, false, PermissionType.Preview)
                    .Allow(gc.Id, Group.Everyone.Id, false, PermissionType.Preview)
                    .Apply();

                // create some sharing
                var sharing = new[]
                {
                    gc.Sharing.Share(user1.Email, SharingLevel.Open, SharingMode.Private),
                    gc.Sharing.Share("user2@example.com", SharingLevel.Open, SharingMode.Authenticated),
                    gc.Sharing.Share("user3@example.com", SharingLevel.Edit, SharingMode.Authenticated)
                };

                // ACTION
                var securityEntries = gc.Security.GetExplicitEntries().OrderBy(e=>e.IdentityId).Select(e => e.ToString());
                var sharingEntries = gc.Sharing.GetExplicitEntries().OrderBy(e => e.IdentityId).Select(e => e.ToString());

                // ASSERT
                AssertSequenceEqual(new[]
                {
                    "Normal|+(8):______________________________________________________________++",
                    "Normal|+(1239):______________________________________________________________++"
                }, securityEntries);
                AssertSequenceEqual(new[]
                {
                    "Sharing|+(8):_________________________________________________________+++++++",
                    "Sharing|+(1239):___________________________________________________________+++++"
                }, sharingEntries);
            });
        }
        [TestMethod]
        public void Sharing_Permissions_Effective()
        {
            Test(() =>
            {
                PrepareForPermissionTest(out var gc, out var user1);

                // set some security entries on the parent chain
                SnSecurityContext.Create().CreateAclEditor()
                    .Allow(Identifiers.PortalRootId, user1.Id, false, PermissionType.Preview)
                    .Allow(gc.ParentId, Group.Everyone.Id, false, PermissionType.Preview)
                    .Apply();

                // create some sharing
                var sharing = new[]
                {
                    gc.Sharing.Share(user1.Email, SharingLevel.Open, SharingMode.Private), // 0
                    gc.Sharing.Share("user2@example.com", SharingLevel.Open, SharingMode.Authenticated),
                    gc.Sharing.Share("user3@example.com", SharingLevel.Edit, SharingMode.Authenticated)
                };

                // ACTION
                var securityEntries = gc.Security.GetEffectiveEntries().OrderBy(e => e.IdentityId).Select(e=>e.ToString());
                var sharingEntries = gc.Sharing.GetEffectiveEntries().OrderBy(e => e.IdentityId).Select(e => e.ToString());

                // ASSERT
                AssertSequenceEqual(new[]
                {
                    "Normal|+(1):_____________________________________________+++++++++++++++++++",
                    "Normal|+(8):______________________________________________________________++",
                    "Normal|+(1239):______________________________________________________________++"
                }, securityEntries);
                AssertSequenceEqual(new[]
                {
                    "Sharing|+(8):_________________________________________________________+++++++",
                    "Sharing|+(1239):___________________________________________________________+++++"
                }, sharingEntries);
            });
        }

        private void PrepareForPermissionTest(out GenericContent gc, out User user)
        {
            using (new SystemAccount())
                SnSecurityContext.Create().CreateAclEditor()
                    .Allow(2, 1, false, PermissionType.BuiltInPermissionTypes)
                    .Apply();

            ReInstallGenericContentCtd();
            user = new User(Node.LoadNode("/Root/IMS/BuiltIn/Portal"))
            {
                Name = "User-1",
                Enabled = true,
                Email = "user1@example.com"
            };
            user.Save();

            var root = CreateTestRoot();

            var content = Content.CreateNew(nameof(GenericContent), root, "Document-1");
            content.Save();
            gc = (GenericContent)content.ContentHandler;
        }

        private void ReInstallGenericContentCtd()
        {
            //UNDONE: update genericcontent CTD in the test structure and delete this method.
            // ...so we do not have to update it here manually

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

        private static void AssertSharingDataAreEqual(SharingData expected, SharingData actual)
        {
            Assert.AreEqual(expected.Id, actual.Id);
            Assert.AreEqual(expected.Token, actual.Token);
            Assert.AreEqual(expected.Identity, actual.Identity);
            Assert.AreEqual(expected.CreatorId, actual.CreatorId);
            Assert.AreEqual(expected.Level, actual.Level);
            Assert.AreEqual(expected.Mode, actual.Mode);
            Assert.AreEqual(expected.ShareDate, actual.ShareDate);
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
