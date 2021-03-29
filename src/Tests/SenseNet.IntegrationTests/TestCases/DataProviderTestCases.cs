using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.DataModel;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.Diagnostics;
using SenseNet.IntegrationTests.Infrastructure;
using SenseNet.Tools;
using SR = SenseNet.ContentRepository.SR;

namespace SenseNet.IntegrationTests.TestCases
{
    /// <summary>
    /// Unit test cases for any DataProvider
    /// </summary>
    public class DataProviderTestCases : TestCaseBase
    {
        /*
        public void DP_RefProp_Install(Func<int, int, int[]> getReferencesFromDatabase)
        {
            Cache.Reset();

            IsolatedIntegrationTest(() =>
            {
                var group = Group.Administrators;
                var expectedIds = group.Members.Select(x => x.Id).ToList();
                var propertyType = ActiveSchema.PropertyTypes["Members"];
                var fromDb = getReferencesFromDatabase(group.VersionId, propertyType.Id);

                // ASSERT
                Assert.IsNotNull(fromDb);
                Assert.AreEqual(2, fromDb.Length);
                Assert.AreEqual(string.Join(",", expectedIds.OrderBy(x => x)),
                    string.Join(",", fromDb.OrderBy(x => x)));
            });
        }
        public void DP_RefProp_Insert(Func<int, int, int[]> getReferencesFromDatabase)
        {
            IsolatedIntegrationTest(() =>
            {
                var propertyType = ActiveSchema.PropertyTypes["Members"];
                var expectedIds = new List<int>();

                var user1 = new User(OrganizationalUnit.Portal) {Name = "User-1", Email = "user1@example.com", Enabled = true};
                user1.Save();
                expectedIds.Add(user1.Id);
                var user2 = new User(OrganizationalUnit.Portal) {Name = "User-2", Email = "user2@example.com", Enabled = true};
                user2.Save();
                expectedIds.Add(user2.Id);

                // ACTION
                var group1 = new Group(OrganizationalUnit.Portal) {Name = "Group-1", Members = new[] {user1, user2}};
                group1.Save();

                // ASSERT
                var after = getReferencesFromDatabase(group1.VersionId, propertyType.Id);
                Assert.AreEqual(string.Join(",", expectedIds.OrderBy(x => x)),
                    string.Join(",", after.OrderBy(x => x)));
            });
        }
        public void DP_RefProp_Update(Func<int, int, int[]> getReferencesFromDatabase)
        {
            Cache.Reset();

            IsolatedIntegrationTest(() =>
            {
                var group = Group.Administrators;
                var expectedIds = group.Members.Select(x => x.Id).ToList();
                var propertyType = ActiveSchema.PropertyTypes["Members"];

                var user = new User(OrganizationalUnit.Portal) {Name = "User-1", Email = "user1@example.com", Enabled = true};
                user.Save();
                expectedIds.Add(user.Id);

                // ACTION
                Group.Administrators.AddMember(user);

                // ASSERT
                var after = getReferencesFromDatabase(group.VersionId, propertyType.Id);
                Assert.AreEqual(string.Join(",", expectedIds.OrderBy(x => x)),
                    string.Join(",", after.OrderBy(x => x)));
            });
        }
        public void DP_RefProp_Delete(Func<int, int, int[]> getReferencesFromDatabase)
        {
            IsolatedIntegrationTest(() =>
            {
                var propertyType = ActiveSchema.PropertyTypes["Members"];

                var user1 = new User(OrganizationalUnit.Portal) {Name = "User-1", Email = "user1@example.com", Enabled = true};
                user1.Save();
                var user2 = new User(OrganizationalUnit.Portal) {Name = "User-2", Email = "user2@example.com", Enabled = true};
                user2.Save();
                var group1 = new Group(OrganizationalUnit.Portal) {Name = "Group-1", Members = new[] { user1, user2 }};
                group1.Save();

                // ACTION
                group1 = Node.Load<Group>(group1.Id);
                group1.ClearReference(propertyType);
                group1.Save();

                // ASSERT
                var after = getReferencesFromDatabase(group1.VersionId, propertyType.Id);
                Assert.IsNull(after);
            });
        }
        */

        private static RepositoryBuilder _repositoryBuilder;
        private void DataProviderUnitTest(IEnumerable<int> nodes, IEnumerable<int> versions, Action<IEnumerable<int>, IEnumerable<int>> cleanup, Action callback)
        {
            try
            {
                SnTrace.EnableAll();

                if (_repositoryBuilder == null)
                {
                    SnTrace.Write("------------------------------- CreateRepositoryBuilder");
                    _repositoryBuilder = Platform.CreateRepositoryBuilder();
                }
                TestInitializer?.Invoke(_repositoryBuilder);
                callback();
            }
            finally
            {
                cleanup(nodes, versions);
            }
        }

        private class TestSchema : ISchemaRoot
        {
            public TypeCollection<PropertyType> PropertyTypes { get; }
            public TypeCollection<NodeType> NodeTypes { get; }
            public TypeCollection<ContentListType> ContentListTypes { get; }

            public TestSchema()
            {
                NodeTypes = new TypeCollection<NodeType>(this);
                PropertyTypes = new TypeCollection<PropertyType>(this);
                ContentListTypes = new TypeCollection<ContentListType>(this);
            }

            public void Clear()
            {
            }

            public void Load()
            {
                throw new NotImplementedException();
            }
        }

        public void UT_Node_InsertDraft(Action<IEnumerable<int>, IEnumerable<int>> cleanup)
        {
            var nodeIds = new List<int>();
            var versionIds = new List<int>();
            DataProviderUnitTest(nodeIds, versionIds, cleanup, () =>
            {
                var dp = Providers.Instance.DataProvider;
                var nodeHeadData = CreateNodeHeadData("UT_Node_InsertDraft");
                var versionData = CreateVersionData("1.42.D");
                var dynamicData = new DynamicPropertyData
                {
                    ContentListProperties = new Dictionary<PropertyType, object>(),
                    DynamicProperties = new Dictionary<PropertyType, object>(),
                    ReferenceProperties = new Dictionary<PropertyType, List<int>>()
                };

                // ACTION
                dp.InsertNodeAsync(nodeHeadData, versionData, dynamicData, CancellationToken.None)
                    .ConfigureAwait(false).GetAwaiter().GetResult();
                nodeIds.Add(nodeHeadData.NodeId);
                versionIds.Add(versionData.VersionId);

                // ASSERT
                var loaded = dp.LoadNodeHeadAsync(nodeHeadData.NodeId, CancellationToken.None)
                    .ConfigureAwait(false).GetAwaiter().GetResult();
                Assert.AreEqual(versionData.VersionId, loaded.LastMinorVersionId);
                Assert.AreEqual(0, loaded.LastMajorVersionId);
            });
        }
        public void UT_Node_InsertPublic(Action<IEnumerable<int>, IEnumerable<int>> cleanup)
        {
            var nodeIds = new List<int>();
            var versionIds = new List<int>();
            DataProviderUnitTest(nodeIds, versionIds, cleanup, () =>
            {
                var dp = Providers.Instance.DataProvider;
                var nodeHeadData = CreateNodeHeadData("UT_Node_InsertPublic");
                var versionData = CreateVersionData("42.0.A");
                var dynamicData = new DynamicPropertyData
                {
                    ContentListProperties = new Dictionary<PropertyType, object>(),
                    DynamicProperties = new Dictionary<PropertyType, object>(),
                    ReferenceProperties = new Dictionary<PropertyType, List<int>>()
                };

                // ACTION
                dp.InsertNodeAsync(nodeHeadData, versionData, dynamicData, CancellationToken.None)
                    .ConfigureAwait(false).GetAwaiter().GetResult();
                nodeIds.Add(nodeHeadData.NodeId);
                versionIds.Add(versionData.VersionId);

                // ASSERT
                var loaded = dp.LoadNodeHeadAsync(nodeHeadData.NodeId, CancellationToken.None)
                    .ConfigureAwait(false).GetAwaiter().GetResult();
                Assert.AreEqual(versionData.VersionId, loaded.LastMinorVersionId);
                Assert.AreEqual(versionData.VersionId, loaded.LastMajorVersionId);
            });
        }
        public void UT_Node_UpdateFirstDraft(Action<IEnumerable<int>, IEnumerable<int>> cleanup)
        {
            var nodeIds = new List<int>();
            var versionIds = new List<int>();
            DataProviderUnitTest(nodeIds, versionIds, cleanup, () =>
            {
                var dp = Providers.Instance.DataProvider;
                var nodeHeadData = CreateNodeHeadData("UT_Node_InsertDraft");
                var versionData = CreateVersionData("0.1.D");
                var dynamicData = new DynamicPropertyData
                {
                    ContentListProperties = new Dictionary<PropertyType, object>(),
                    DynamicProperties = new Dictionary<PropertyType, object>(),
                    ReferenceProperties = new Dictionary<PropertyType, List<int>>()
                };

                dp.InsertNodeAsync(nodeHeadData, versionData, dynamicData, CancellationToken.None)
                    .ConfigureAwait(false).GetAwaiter().GetResult();
                nodeIds.Add(nodeHeadData.NodeId);
                versionIds.Add(versionData.VersionId);
                var expectedIndex = nodeHeadData.Index + 1;

                // ACTION
                nodeHeadData.Index++;
//versionData.NodeId = nodeHeadData.NodeId;
                dp.UpdateNodeAsync(nodeHeadData, versionData, dynamicData, null, CancellationToken.None)
                    .ConfigureAwait(false).GetAwaiter().GetResult();

                // ASSERT
                var loaded = dp.LoadNodeHeadAsync(nodeHeadData.NodeId, CancellationToken.None)
                    .ConfigureAwait(false).GetAwaiter().GetResult();
                Assert.AreEqual(expectedIndex, loaded.Index);
            });
        }

        public void UT_RefProp_Insert(Func<int, int, int[]> getReferencesFromDatabase, Action<IEnumerable<int>, IEnumerable<int>> cleanup)
        {
            var nodeIds = new List<int>();
            var versionIds = new List<int>();
            DataProviderUnitTest(nodeIds, versionIds, cleanup, () =>
            {
                var dp = Providers.Instance.DataProvider;
                var schema = new TestSchema();
                var propType = new PropertyType(schema, "TestReferenceProperty", 9999, DataType.Reference, 1234, false);
                schema.PropertyTypes.Add(propType);
                var expectedIds = new List<int> { 2345, 3456, 4567, 5678, 6789 };
                var nodeHeadData = CreateNodeHeadData("UT_RefProp_Insert");
                var versionData = CreateVersionData("1.42.D");
                var dynamicData = new DynamicPropertyData
                {
                    ContentListProperties = new Dictionary<PropertyType, object>(),
                    DynamicProperties = new Dictionary<PropertyType, object>(),
                    ReferenceProperties = new Dictionary<PropertyType, List<int>> {{propType, expectedIds}}
                };

                // ACTION
                dp.InsertNodeAsync(nodeHeadData, versionData, dynamicData, CancellationToken.None)
                    .ConfigureAwait(false).GetAwaiter().GetResult();
                nodeIds.Add(nodeHeadData.NodeId);
                versionIds.Add(versionData.VersionId);

                // ASSERT
                var after = getReferencesFromDatabase(versionData.VersionId, 9999);
                Assert.AreEqual(string.Join(",", expectedIds.OrderBy(x => x)),
                    string.Join(",", after.OrderBy(x => x)));
            });
        }
        public void UT_RefProp_Update(Func<int, int, int[]> getReferencesFromDatabase, Action<IEnumerable<int>, IEnumerable<int>> cleanup)
        {
            var nodeIds = new List<int>();
            var versionIds = new List<int>();
            DataProviderUnitTest(nodeIds, versionIds, cleanup, () =>
            {
                var dp = Providers.Instance.DataProvider;
                var schema = new TestSchema();
                var propType = new PropertyType(schema, "TestReferenceProperty", 9999, DataType.Reference, 1234, false);
                schema.PropertyTypes.Add(propType);
                var initialIds = new List<int> { 2345, 3456, 4567, 5678, 6789 };
                var expectedIds = new List<int> { 12345, 23456, 34567 };
                var nodeHeadData = CreateNodeHeadData("UT_RefProp_Update");
                var versionData = CreateVersionData("1.42.D");
                var dynamicData = new DynamicPropertyData
                {
                    ContentListProperties = new Dictionary<PropertyType, object>(),
                    DynamicProperties = new Dictionary<PropertyType, object>(),
                    ReferenceProperties = new Dictionary<PropertyType, List<int>> {{propType, initialIds}}
                };

                dp.InsertNodeAsync(nodeHeadData, versionData, dynamicData, CancellationToken.None)
                    .ConfigureAwait(false).GetAwaiter().GetResult();
                nodeIds.Add(nodeHeadData.NodeId);
                versionIds.Add(versionData.VersionId);

                // ACTION
                dynamicData.ReferenceProperties = new Dictionary<PropertyType, List<int>> {{propType, expectedIds}};
                dp.UpdateNodeAsync(nodeHeadData, versionData, dynamicData, null, CancellationToken.None)
                    .ConfigureAwait(false).GetAwaiter().GetResult();

                // ASSERT
                var after = getReferencesFromDatabase(versionData.VersionId, 9999);
                Assert.AreEqual(string.Join(",", expectedIds.OrderBy(x => x)),
                    string.Join(",", after.OrderBy(x => x)));
            });
        }
        public void UT_RefProp_Update3to0(Func<int, int, int[]> getReferencesFromDatabase, Action<IEnumerable<int>, IEnumerable<int>> cleanup)
        {
            var nodeIds = new List<int>();
            var versionIds = new List<int>();
            DataProviderUnitTest(nodeIds, versionIds, cleanup, () =>
            {
                var dp = Providers.Instance.DataProvider;
                var schema = new TestSchema();
                var propType = new PropertyType(schema, "TestReferenceProperty", 9999, DataType.Reference, 1234, false);
                schema.PropertyTypes.Add(propType);
                var initialIds = new List<int> { 2345, 3456, 4567 };
                var expectedIds = new List<int>();
                var nodeHeadData = CreateNodeHeadData("UT_RefProp_Update3to0");
                var versionData = CreateVersionData("42.0.A");
                var dynamicData = new DynamicPropertyData
                {
                    ContentListProperties = new Dictionary<PropertyType, object>(),
                    DynamicProperties = new Dictionary<PropertyType, object>(),
                    ReferenceProperties = new Dictionary<PropertyType, List<int>> { { propType, initialIds } }
                };

                dp.InsertNodeAsync(nodeHeadData, versionData, dynamicData, CancellationToken.None)
                    .ConfigureAwait(false).GetAwaiter().GetResult();
                nodeIds.Add(nodeHeadData.NodeId);
                versionIds.Add(versionData.VersionId);

                // ACTION
                dynamicData.ReferenceProperties = new Dictionary<PropertyType, List<int>> { { propType, expectedIds } };
                dp.UpdateNodeAsync(nodeHeadData, versionData, dynamicData, null, CancellationToken.None)
                    .ConfigureAwait(false).GetAwaiter().GetResult();

                // ASSERT
                var after = getReferencesFromDatabase(versionData.VersionId, 9999);
                Assert.IsNull(after);
            });
        }
        public void UT_RefProp_Update0to3(Func<int, int, int[]> getReferencesFromDatabase, Action<IEnumerable<int>, IEnumerable<int>> cleanup)
        {
            var nodeIds = new List<int>();
            var versionIds = new List<int>();
            DataProviderUnitTest(nodeIds, versionIds, cleanup, () =>
            {
                var dp = Providers.Instance.DataProvider;
                var schema = new TestSchema();
                var propType = new PropertyType(schema, "TestReferenceProperty", 9999, DataType.Reference, 1234, false);
                schema.PropertyTypes.Add(propType);
                var initialIds = new List<int>();
                var expectedIds = new List<int> { 2345, 3456, 4567 };
                var nodeHeadData = CreateNodeHeadData("UT_RefProp_Update0to3");
                var versionData = CreateVersionData("42.0.A");
                var dynamicData = new DynamicPropertyData
                {
                    ContentListProperties = new Dictionary<PropertyType, object>(),
                    DynamicProperties = new Dictionary<PropertyType, object>(),
                    ReferenceProperties = new Dictionary<PropertyType, List<int>> { { propType, initialIds } }
                };

                dp.InsertNodeAsync(nodeHeadData, versionData, dynamicData, CancellationToken.None)
                    .ConfigureAwait(false).GetAwaiter().GetResult();
                nodeIds.Add(nodeHeadData.NodeId);
                versionIds.Add(versionData.VersionId);

                // ACTION
                dynamicData.ReferenceProperties = new Dictionary<PropertyType, List<int>> { { propType, expectedIds } };
                dp.UpdateNodeAsync(nodeHeadData, versionData, dynamicData, null, CancellationToken.None)
                    .ConfigureAwait(false).GetAwaiter().GetResult();

                // ASSERT
                var after = getReferencesFromDatabase(versionData.VersionId, 9999);
                Assert.AreEqual(string.Join(",", expectedIds.OrderBy(x => x)),
                    string.Join(",", after.OrderBy(x => x)));
            });
        }

        /* ====================================================================== TOOLS */

        private Random _random = new Random();
        private int Rng()
        {
            return _random.Next(100000, int.MaxValue);
        }

        private NodeHeadData CreateNodeHeadData(string name)
        {
            return new NodeHeadData
            {
                Name = name,
                CreatedById = Rng(),
                ModifiedById = Rng(),
                CreationDate = DateTime.UtcNow,
                ModificationDate = DateTime.UtcNow,
                ContentListId = 0,
                ContentListTypeId = 0,
                DisplayName = $"{name} DisplayName",
                Index = Rng(),
                ParentNodeId = Rng(),
                Path = $"/TEST/{name}",
                OwnerId = Rng(),
                NodeTypeId = Rng(),
            };
        }
        private VersionData CreateVersionData(string version)
        {
            return new VersionData
            {
                CreatedById = 1,
                ModifiedById = 1,
                CreationDate = DateTime.UtcNow,
                ModificationDate = DateTime.UtcNow,
                Version = VersionNumber.Parse(version),
            };
        }
    }
}
