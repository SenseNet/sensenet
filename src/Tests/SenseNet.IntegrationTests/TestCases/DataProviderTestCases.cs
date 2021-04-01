using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.DataModel;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.IntegrationTests.Infrastructure;
using SenseNet.Testing;

namespace SenseNet.IntegrationTests.TestCases
{
    /// <summary>
    /// Unit test cases for any DataProvider
    /// </summary>
    public class DataProviderTestCases : TestCaseBase
    {
        private void DataProviderUnitTest(IEnumerable<int> nodes, IEnumerable<int> versions, Action<IEnumerable<int>, IEnumerable<int>> cleanup, Action callback)
        {
            try
            {
                var builder = Platform.CreateRepositoryBuilder();
                if(Providers.Instance.BlobStorage == null)
                    Providers.Instance.InitializeBlobProviders();
                TestInitializer?.Invoke(builder);
                callback();
            }
            finally
            {
                cleanup(nodes, versions);
            }
        }

        private class TestSchema : SchemaRoot
        {
        }

        /// <summary>
        /// UT_Node_InsertDraft. The parameter is a method that clears the main tables
        /// (Nodes, Versions, LongTextProperties, ReferenceProperties, BinaryProperties and Files).
        /// Note that the blobs do not need to be deleted.
        /// </summary>
        /// <param name="cleanup">Method that clears the main tables.</param>
        public void UT_Node_InsertDraft(Action<IEnumerable<int>, IEnumerable<int>> cleanup)
        {
            var nodeIds = new List<int>();
            var versionIds = new List<int>();
            DataProviderUnitTest(nodeIds, versionIds, cleanup, () =>
            {
                var dp = Providers.Instance.DataProvider;
                var nodeHeadData = CreateNodeHeadData("UT_Node_InsertDraft", Rng());
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
        /// <summary>
        /// UT_Node_InsertPublic. The parameter is a method that clears the main tables
        /// (Nodes, Versions, LongTextProperties, ReferenceProperties, BinaryProperties and Files).
        /// Note that the blobs do not need to be deleted.
        /// </summary>
        /// <param name="cleanup">Method that clears the main tables.</param>
        public void UT_Node_InsertPublic(Action<IEnumerable<int>, IEnumerable<int>> cleanup)
        {
            var nodeIds = new List<int>();
            var versionIds = new List<int>();
            DataProviderUnitTest(nodeIds, versionIds, cleanup, () =>
            {
                var dp = Providers.Instance.DataProvider;
                var nodeHeadData = CreateNodeHeadData("UT_Node_InsertPublic", Rng());
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
        /// <summary>
        /// UT_Node_UpdateFirstDraft. The parameter is a method that clears the main tables
        /// (Nodes, Versions, LongTextProperties, ReferenceProperties, BinaryProperties and Files).
        /// Note that the blobs do not need to be deleted.
        /// </summary>
        /// <param name="cleanup">Method that clears the main tables.</param>
        public void UT_Node_UpdateFirstDraft(Action<IEnumerable<int>, IEnumerable<int>> cleanup)
        {
            var nodeIds = new List<int>();
            var versionIds = new List<int>();
            DataProviderUnitTest(nodeIds, versionIds, cleanup, () =>
            {
                var dp = Providers.Instance.DataProvider;
                var nodeHeadData = CreateNodeHeadData("UT_Node_InsertDraft", Rng());
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
                dp.UpdateNodeAsync(nodeHeadData, versionData, dynamicData, new int[0], CancellationToken.None)
                    .ConfigureAwait(false).GetAwaiter().GetResult();

                // ASSERT
                var loaded = dp.LoadNodeHeadAsync(nodeHeadData.NodeId, CancellationToken.None)
                    .ConfigureAwait(false).GetAwaiter().GetResult();
                Assert.AreEqual(expectedIndex, loaded.Index);
            });
        }

        /// <summary>
        /// UT_RefProp_Insert. The parameter is a method that clears the main tables
        /// The first parameter is a method that returns the referred nodeIds from the ReferenceProperties by
        /// the given versionId and propertyTypeId: getReferencesFromDatabase(versionId, propertyTypeId)
        /// The second parameter is a method that clears the main tables
        /// (Nodes, Versions, LongTextProperties, ReferenceProperties, BinaryProperties and Files).
        /// Note that the blobs do not need to be deleted.
        /// </summary>
        /// <param name="getReferencesFromDatabase">getReferencesFromDatabase(versionId, propertyTypeId).</param>
        /// <param name="cleanup">Method that clears the main tables.</param>
        public void UT_RefProp_Insert(Func<int, int, int[]> getReferencesFromDatabase, Action<IEnumerable<int>, IEnumerable<int>> cleanup)
        {
            var nodeIds = new List<int>();
            var versionIds = new List<int>();
            DataProviderUnitTest(nodeIds, versionIds, cleanup, () =>
            {
                var schema = CreateSchema("UT_RefProp_Load", out var nodeType, out var propType);
                using (new Swindler<string>(schema.ToXml(),
                    () => NodeTypeManager.Current.ToXml(),
                    value =>
                    {
                        NodeTypeManager.Current.Clear();
                        NodeTypeManager.Current.Load(value);
                    }))
                {
                    var dp = Providers.Instance.DataProvider;

                    var expectedIds = new List<int> { 2345, 3456, 4567, 5678, 6789 };
                    var nodeHeadData = CreateNodeHeadData("UT_RefProp_Insert", nodeType.Id);
                    var versionData = CreateVersionData("1.42.D");
                    var dynamicData = new DynamicPropertyData
                    {
                        ContentListProperties = new Dictionary<PropertyType, object>(),
                        DynamicProperties = new Dictionary<PropertyType, object>(),
                        ReferenceProperties = new Dictionary<PropertyType, List<int>> { { propType, expectedIds } }
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
                }
            });
        }
        /// <summary>
        /// UT_RefProp_Load. The parameter is a method that clears the main tables
        /// The first parameter is a method that returns the referred nodeIds from the ReferenceProperties by
        /// the given versionId and propertyTypeId: getReferencesFromDatabase(versionId, propertyTypeId)
        /// The second parameter is a method that clears the main tables
        /// (Nodes, Versions, LongTextProperties, ReferenceProperties, BinaryProperties and Files).
        /// Note that the blobs do not need to be deleted.
        /// </summary>
        /// <param name="getReferencesFromDatabase">getReferencesFromDatabase(versionId, propertyTypeId).</param>
        /// <param name="cleanup">Method that clears the main tables.</param>
        public void UT_RefProp_Load(Func<int, int, int[]> getReferencesFromDatabase, Action<IEnumerable<int>, IEnumerable<int>> cleanup)
        {
            var nodeIds = new List<int>();
            var versionIds = new List<int>();
            DataProviderUnitTest(nodeIds, versionIds, cleanup, () =>
            {
                var schema = CreateSchema("UT_RefProp_Load", out var nodeType, out var propType);
                using (new Swindler<string>(schema.ToXml(),
                    () => NodeTypeManager.Current.ToXml(),
                    value =>
                    {
                        NodeTypeManager.Current.Clear();
                        NodeTypeManager.Current.Load(value);
                    }))
                {
                    var dp = Providers.Instance.DataProvider;

                    var expectedIds = new List<int> { 2345, 3456, 4567, 5678, 6789 };
                    var nodeHeadData = CreateNodeHeadData("UT_RefProp_Load", nodeType.Id);
                    var versionData = CreateVersionData("1.42.D");
                    var dynamicData = new DynamicPropertyData
                    {
                        ContentListProperties = new Dictionary<PropertyType, object>(),
                        DynamicProperties = new Dictionary<PropertyType, object>(),
                        ReferenceProperties = new Dictionary<PropertyType, List<int>> { { propType, expectedIds } }
                    };

                    dp.InsertNodeAsync(nodeHeadData, versionData, dynamicData, CancellationToken.None)
                        .ConfigureAwait(false).GetAwaiter().GetResult();
                    nodeIds.Add(nodeHeadData.NodeId);
                    versionIds.Add(versionData.VersionId);

                    // ACTION
                    var nodeData = dp.LoadNodesAsync(new[] { versionData.VersionId }, CancellationToken.None)
                        .ConfigureAwait(false).GetAwaiter().GetResult().FirstOrDefault();

                    // ASSERT
                    var after = getReferencesFromDatabase(versionData.VersionId, 9999);
                    Assert.AreEqual(string.Join(",", expectedIds.OrderBy(x => x)),
                        string.Join(",", after.OrderBy(x => x)));
                    Assert.IsNotNull(nodeData);
                    var loaded = (IEnumerable<int>)nodeData.GetDynamicRawData(propType);
                    Assert.AreEqual(string.Join(",", expectedIds.OrderBy(x => x)),
                        string.Join(",", loaded.OrderBy(x => x)));
                }
            });
        }
        /// <summary>
        /// UT_RefProp_Update.
        /// The first parameter is a method that returns the referred nodeIds from the ReferenceProperties by
        /// the given versionId and propertyTypeId: getReferencesFromDatabase(versionId, propertyTypeId)
        /// The second parameter is a method that clears the main tables
        /// (Nodes, Versions, LongTextProperties, ReferenceProperties, BinaryProperties and Files).
        /// Note that the blobs do not need to be deleted.
        /// </summary>
        /// <param name="getReferencesFromDatabase">getReferencesFromDatabase(versionId, propertyTypeId).</param>
        /// <param name="cleanup">Method that clears the main tables.</param>
        public void UT_RefProp_Update(Func<int, int, int[]> getReferencesFromDatabase, Action<IEnumerable<int>, IEnumerable<int>> cleanup)
        {
            var nodeIds = new List<int>();
            var versionIds = new List<int>();
            DataProviderUnitTest(nodeIds, versionIds, cleanup, () =>
            {
                var schema = CreateSchema("UT_RefProp_Update", out var nodeType, out var propType);
                using (new Swindler<string>(schema.ToXml(),
                    () => NodeTypeManager.Current.ToXml(),
                    value =>
                    {
                        NodeTypeManager.Current.Clear();
                        NodeTypeManager.Current.Load(value);
                    }))
                {
                    var dp = Providers.Instance.DataProvider;

                    var initialIds = new List<int> { 2345, 3456, 4567, 5678, 6789 };
                    var expectedIds = new List<int> { 12345, 23456, 34567 };
                    var nodeHeadData = CreateNodeHeadData("UT_RefProp_Update", nodeType.Id);
                    var versionData = CreateVersionData("1.42.D");
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
                    dp.UpdateNodeAsync(nodeHeadData, versionData, dynamicData, new int[0], CancellationToken.None)
                        .ConfigureAwait(false).GetAwaiter().GetResult();

                    // ASSERT
                    var after = getReferencesFromDatabase(versionData.VersionId, 9999);
                    Assert.AreEqual(string.Join(",", expectedIds.OrderBy(x => x)),
                        string.Join(",", after.OrderBy(x => x)));
                }
            });
        }
        /// <summary>
        /// The first parameter is a method that returns the referred nodeIds from the ReferenceProperties by
        /// the given versionId and propertyTypeId: getReferencesFromDatabase(versionId, propertyTypeId)
        /// The second parameter is a method that clears the main tables
        /// (Nodes, Versions, LongTextProperties, ReferenceProperties, BinaryProperties and Files).
        /// Note that the blobs do not need to be deleted.
        /// </summary>
        /// <param name="getReferencesFromDatabase">getReferencesFromDatabase(versionId, propertyTypeId).</param>
        /// <param name="cleanup">Method that clears the main tables.</param>
        public void UT_RefProp_Update3to0(Func<int, int, int[]> getReferencesFromDatabase, Action<IEnumerable<int>, IEnumerable<int>> cleanup)
        {
            var nodeIds = new List<int>();
            var versionIds = new List<int>();
            DataProviderUnitTest(nodeIds, versionIds, cleanup, () =>
            {
                var schema = CreateSchema("UT_RefProp_Update3to0", out var nodeType, out var propType);
                using (new Swindler<string>(schema.ToXml(),
                    () => NodeTypeManager.Current.ToXml(),
                    value =>
                    {
                        NodeTypeManager.Current.Clear();
                        NodeTypeManager.Current.Load(value);
                    }))
                {
                    var dp = Providers.Instance.DataProvider;

                    var initialIds = new List<int> { 2345, 3456, 4567 };
                    var expectedIds = new List<int>();
                    var nodeHeadData = CreateNodeHeadData("UT_RefProp_Update3to0", nodeType.Id);
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
                    dp.UpdateNodeAsync(nodeHeadData, versionData, dynamicData, new int[0], CancellationToken.None)
                        .ConfigureAwait(false).GetAwaiter().GetResult();

                    // ASSERT
                    var after = getReferencesFromDatabase(versionData.VersionId, 9999);
                    Assert.IsNull(after);
                }
            });
        }
        /// <summary>
        /// The first parameter is a method that returns the referred nodeIds from the ReferenceProperties by
        /// the given versionId and propertyTypeId: getReferencesFromDatabase(versionId, propertyTypeId)
        /// The second parameter is a method that clears the main tables
        /// (Nodes, Versions, LongTextProperties, ReferenceProperties, BinaryProperties and Files).
        /// Note that the blobs do not need to be deleted.
        /// </summary>
        /// <param name="getReferencesFromDatabase">getReferencesFromDatabase(versionId, propertyTypeId).</param>
        /// <param name="cleanup">Method that clears the main tables.</param>
        public void UT_RefProp_Update0to3(Func<int, int, int[]> getReferencesFromDatabase, Action<IEnumerable<int>, IEnumerable<int>> cleanup)
        {
            var nodeIds = new List<int>();
            var versionIds = new List<int>();
            DataProviderUnitTest(nodeIds, versionIds, cleanup, () =>
            {
                var schema = CreateSchema("UT_RefProp_Update0to3", out var nodeType, out var propType);
                using (new Swindler<string>(schema.ToXml(),
                    () => NodeTypeManager.Current.ToXml(),
                    value =>
                    {
                        NodeTypeManager.Current.Clear();
                        NodeTypeManager.Current.Load(value);
                    }))
                {
                    var dp = Providers.Instance.DataProvider;

                    var initialIds = new List<int>();
                    var expectedIds = new List<int> { 2345, 3456, 4567 };
                    var nodeHeadData = CreateNodeHeadData("UT_RefProp_Update0to3", nodeType.Id);
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
                    dp.UpdateNodeAsync(nodeHeadData, versionData, dynamicData, new int[0], CancellationToken.None)
                        .ConfigureAwait(false).GetAwaiter().GetResult();

                    // ASSERT
                    var after = getReferencesFromDatabase(versionData.VersionId, 9999);
                    Assert.AreEqual(string.Join(",", expectedIds.OrderBy(x => x)),
                        string.Join(",", after.OrderBy(x => x)));
                }
            });
        }
        /// <summary>
        /// UT_RefProp_Update.
        /// The first parameter is a method that returns the referred nodeIds from the ReferenceProperties by
        /// the given versionId and propertyTypeId: getReferencesFromDatabase(versionId, propertyTypeId)
        /// The second parameter is a method that clears the main tables
        /// (Nodes, Versions, LongTextProperties, ReferenceProperties, BinaryProperties and Files).
        /// Note that the blobs do not need to be deleted.
        /// </summary>
        /// <param name="getReferencesFromDatabase">getReferencesFromDatabase(versionId, propertyTypeId).</param>
        /// <param name="cleanup">Method that clears the main tables.</param>
        public void UT_RefProp_NewVersionAndUpdate(Func<int, int, int[]> getReferencesFromDatabase, Action<IEnumerable<int>, IEnumerable<int>> cleanup)
        {
            var nodeIds = new List<int>();
            var versionIds = new List<int>();
            DataProviderUnitTest(nodeIds, versionIds, cleanup, () =>
            {
                var schema = CreateSchema("UT_RefProp_NewVersionAndUpdate", out var nodeType, out var propType);
                using (new Swindler<string>(schema.ToXml(),
                    () => NodeTypeManager.Current.ToXml(),
                    value =>
                    {
                        NodeTypeManager.Current.Clear();
                        NodeTypeManager.Current.Load(value);
                    }))
                {
                    var dp = Providers.Instance.DataProvider;

                    var initialIds = new List<int> { 2345, 3456, 4567, 5678, 6789 };
                    var expectedIds = new List<int> { 12345, 23456, 34567 };
                    var nodeHeadData = CreateNodeHeadData("UT_RefProp_NewVersionAndUpdate", nodeType.Id);
                    var versionData = CreateVersionData("1.42.D");
                    var dynamicData = new DynamicPropertyData
                    {
                        ContentListProperties = new Dictionary<PropertyType, object>(),
                        DynamicProperties = new Dictionary<PropertyType, object>(),
                        ReferenceProperties = new Dictionary<PropertyType, List<int>> { { propType, initialIds } }
                    };

                    dp.InsertNodeAsync(nodeHeadData, versionData, dynamicData, CancellationToken.None)
                        .ConfigureAwait(false).GetAwaiter().GetResult();
                    nodeIds.Add(nodeHeadData.NodeId);
                    var versionIdBefore = versionData.VersionId;
                    versionIds.Add(versionIdBefore);

                    // ACTION
                    dynamicData.ReferenceProperties = new Dictionary<PropertyType, List<int>> { { propType, expectedIds } };
                    dp.CopyAndUpdateNodeAsync(nodeHeadData, versionData, dynamicData, new int[0], CancellationToken.None,
                            expectedVersionId: 0).ConfigureAwait(false).GetAwaiter().GetResult();
                    var versionIdAfter = versionData.VersionId;
                    versionIds.Add(versionIdAfter);

                    // ASSERT
                    Assert.AreNotEqual(versionIdBefore, versionIdAfter);
                    var initial = string.Join(",",
                        initialIds.OrderBy(x => x));
                    var before = string.Join(",",
                        getReferencesFromDatabase(versionIdBefore, 9999).OrderBy(x => x));
                    Assert.AreEqual(initial, before);
                    var expected = string.Join(",",
                        expectedIds.OrderBy(x => x));
                    var after = string.Join(",",
                        getReferencesFromDatabase(versionIdAfter, 9999).OrderBy(x => x));
                    Assert.AreEqual(expected, after);
                }
            });
        }

        /* ====================================================================== TOOLS */

        private Random _random = new Random();
        private int Rng()
        {
            return _random.Next(100000, int.MaxValue);
        }

        private TestSchema CreateSchema(string testName, out NodeType nodeType, out PropertyType propertyType)
        {
            var schema = new TestSchema();
            propertyType = new PropertyType(schema, "TestReferenceProperty", 9999, DataType.Reference, 1234, false);
            schema.PropertyTypes.Add(propertyType);

            nodeType = new NodeType(Rng(), $"{testName}_NodeType", schema,
                $"{testName}_ClassName", null);
            nodeType.AddPropertyType(propertyType);
            schema.NodeTypes.Add(nodeType);

            return schema;
        }

        private NodeHeadData CreateNodeHeadData(string name, int nodeTypeId)
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
                NodeTypeId = nodeTypeId,
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
