using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.ContentRepository.Storage.Caching.Dependency;
using SenseNet.ContentRepository.Storage.Data.SqlClient;
using SenseNet.ContentRepository.Storage.DataModel;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;
using SenseNet.Search.Indexing;
using SenseNet.Search.Querying;

// ReSharper disable ParameterOnlyUsedForPreconditionCheck.Local

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.Data
{
    [DebuggerDisplay("{" + nameof(ToString) + "()}")]
    public class SnapshotItem //UNDONE:DB -------Remove SnapshotItem class
    {
        public string Name;
        public bool IsDp2;
        public object Snapshot;

        public override string ToString()
        {
            return $"{Name} DP{(IsDp2 ? 2 : 0)} {Snapshot.GetType().Name}";
        }
    }

    public static class DataStore
    {
        // ReSharper disable once InconsistentNaming
        //UNDONE:DB -------Remove DataStore.__enabled
        private static bool __enabled;
        public static bool Enabled
        {
            get => __enabled;
            set
            {
                __enabled = value;
                BlobStorageComponents.DataStoreEnabled = value;
            }
        }

        //UNDONE:DB -------Remove DataStore.SnapshotsEnabled
        public static bool SnapshotsEnabled { get; set; }

        public static List<SnapshotItem> Snapshots { get; } = new List<SnapshotItem>();//UNDONE:DB -------Remove DataStore.Snapshots


        public static DataProvider2 DataProvider => Providers.Instance.DataProvider2;

        public static int PathMaxLength => DataProvider.PathMaxLength;
        public static DateTime DateTimeMinValue => DataProvider.DateTimeMinValue;
        public static DateTime DateTimeMaxValue => DataProvider.DateTimeMaxValue;
        public static decimal DecimalMinValue => DataProvider.DecimalMinValue;
        public static decimal DecimalMaxValue => DataProvider.DecimalMaxValue;


        public static T GetDataProviderExtension<T>() where T : class, IDataProviderExtension
        {
            return DataProvider.GetExtensionInstance<T>();
        }

        public static void Reset()
        {
            DataProvider.Reset();
        }

        /* =============================================================================================== Installation */

        public static async Task InstallInitialDataAsync(InitialData data)
        {
            await DataProvider.InstallInitialDataAsync(data);
        }

        public static async Task<IEnumerable<EntityTreeNodeData>> LoadEntityTreeAsync()
        {
            return await DataProvider.LoadEntityTreeAsync();
        }

        /* =============================================================================================== Nodes */

        public static async Task SaveNodeAsync(NodeData nodeData, NodeSaveSettings settings, CancellationToken cancellationToken)
        {
            //UNDONE:DB -------Delete CheckTimestamps feature
            var nodeTimestampBefore = DataProvider.GetNodeTimestamp(nodeData.Id);
            var versionTimestampBefore = DataProvider.GetVersionTimestamp(nodeData.VersionId);

            // ORIGINAL SIGNATURES:
            // internal void SaveNodeData(NodeData nodeData, NodeSaveSettings settings, out int lastMajorVersionId, out int lastMinorVersionId)
            // private static void SaveNodeBaseData(NodeData nodeData, SavingAlgorithm savingAlgorithm, INodeWriter writer, NodeSaveSettings settings, out int lastMajorVersionId, out int lastMinorVersionId)
            // private static void SaveNodeProperties(NodeData nodeData, SavingAlgorithm savingAlgorithm, INodeWriter writer, bool isNewNode)
            // protected internal abstract INodeWriter CreateNodeWriter();
            // protected internal abstract void DeleteVersion(int versionId, NodeData nodeData, out int lastMajorVersionId, out int lastMinorVersionId);
            // -------------------
            // Before return the LastMajorVersionIdAfter and LastMinorVersionIdAfter properties of the given "settings" need to be updated
            //    instead of use the original output values.

            //UNDONE:DB ?Implement transaction related stuff (from DataBackingStore)
            //UNDONE:DB Implement cache invalidations (from DataBackingStore)

            cancellationToken.ThrowIfCancellationRequested();

            if (nodeData == null)
                throw new ArgumentNullException(nameof(nodeData));
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            var isNewNode = nodeData.Id == 0;

            // SAVE DATA (head, version, dynamic metadata, binaries)
            // Do not block any exception from the called methods.
            // If need a catch block rethrow away the exception.

            var nodeHeadData = nodeData.GetNodeHeadData();
            var savingAlgorithm = settings.GetSavingAlgorithm();
            if (settings.NeedToSaveData)
            {
                var versionData = nodeData.GetVersionData();
                DynamicPropertyData dynamicData;
                switch (savingAlgorithm)
                {
                    case SavingAlgorithm.CreateNewNode:
                        dynamicData = nodeData.GetDynamicData(false);
                        await DataProvider.InsertNodeAsync(nodeHeadData, versionData, dynamicData);
                        // Write back the new NodeId
                        nodeData.Id = nodeHeadData.NodeId;
                        break;
                    case SavingAlgorithm.UpdateSameVersion:
                        dynamicData = nodeData.GetDynamicData(false);
                        await DataProvider.UpdateNodeAsync(nodeHeadData, versionData, dynamicData, settings.DeletableVersionIds);
                        break;
                    case SavingAlgorithm.CopyToNewVersionAndUpdate:
                        dynamicData = nodeData.GetDynamicData(true);
                        await DataProvider.CopyAndUpdateNodeAsync(nodeHeadData, versionData, dynamicData, settings.DeletableVersionIds,
                            settings.CurrentVersionId);
                        break;
                    case SavingAlgorithm.CopyToSpecifiedVersionAndUpdate:
                        dynamicData = nodeData.GetDynamicData(true);
                        await DataProvider.CopyAndUpdateNodeAsync(nodeHeadData, versionData, dynamicData, settings.DeletableVersionIds,
                            settings.CurrentVersionId, settings.ExpectedVersionId);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("Unknown SavingAlgorithm: " + savingAlgorithm);
                }
                // Write back the version level changed values
                nodeData.VersionId = versionData.VersionId;
                nodeData.VersionTimestamp = versionData.Timestamp;
                //UNDONE:DB -------Delete CheckTimestamps feature
                AssertVersionTimestampIncremented(nodeData, versionTimestampBefore);

                if (!isNewNode && nodeData.PathChanged && nodeData.SharedData != null)
                    await DataProvider.UpdateSubTreePathAsync(nodeData.SharedData.Path, nodeData.Path);
            }
            else
            {
                await DataProvider.UpdateNodeHeadAsync(nodeHeadData, settings.DeletableVersionIds);
            }
            // Write back NodeHead level changed values
            settings.LastMajorVersionIdAfter = nodeHeadData.LastMajorVersionId;
            settings.LastMinorVersionIdAfter = nodeHeadData.LastMinorVersionId;
            nodeData.NodeTimestamp = nodeHeadData.Timestamp;
            //UNDONE:DB -------Delete CheckTimestamps feature
            AssertNodeTimestampIncremented(nodeData, nodeTimestampBefore);
        }
        public static async Task<NodeToken[]> LoadNodesAsync(NodeHead[] headArray, int[] versionIdArray)
        {
            // ORIGINAL SIGNATURES:
            // internal void LoadNodeData(IEnumerable<NodeToken> tokens)
            // protected internal abstract void LoadNodes(Dictionary<int, NodeBuilder> buildersByVersionId);

            var tokens = new List<NodeToken>();
            var tokensToLoad = new List<NodeToken>();
            for (var i = 0; i < headArray.Length; i++)
            {
                var head = headArray[i];
                var versionId = versionIdArray[i];

                var token = new NodeToken(head.Id, head.NodeTypeId, head.ContentListId, head.ContentListTypeId, versionId, null)
                {
                    NodeHead = head
                };
                tokens.Add(token);

                var cacheKey = GenerateNodeDataVersionIdCacheKey(versionId);
                if (DistributedApplication.Cache.Get(cacheKey) is NodeData nodeData)
                    token.NodeData = nodeData;
                else
                    tokensToLoad.Add(token);
            }
            if (tokensToLoad.Count > 0)
            {
                var versionIds = tokensToLoad.Select(x => x.VersionId).ToArray();
                var loadedCollection = await DataProvider.LoadNodesAsync(versionIds);
                foreach (var nodeData in loadedCollection)
                {
                    if (nodeData != null) // lost version
                    {
                        CacheNodeData(nodeData);
                        var token = tokensToLoad.First(x => x.VersionId == nodeData.VersionId);
                        token.NodeData = nodeData;
                    }
                }
            }
            return tokens.ToArray();
        }
        public static async Task DeleteNodeAsync(int nodeId, long timestamp)
        {
            // ORIGINAL SIGNATURES:
            // internal void DeleteNode(int nodeId)
            // internal void DeleteNodePsychical(int nodeId, long timestamp)
            // protected internal abstract DataOperationResult DeleteNodeTree(int nodeId);
            // protected internal abstract DataOperationResult DeleteNodeTreePsychical(int nodeId, long timestamp);
            // -------------------
            // The word as suffix "Tree" is unnecessary, "Psychical" is misleading.

            await DataProvider.DeleteNodeAsync(nodeId, timestamp);
        }
        public static async Task MoveNodeAsync(int sourceNodeId, int targetNodeId, long sourceTimestamp, long targetTimestamp)
        {
            // ORIGINAL SIGNATURES:
            // internal void MoveNode(int sourceNodeId, int targetNodeId, long sourceTimestamp, long targetTimestamp)
            // protected internal abstract DataOperationResult MoveNodeTree(int sourceNodeId, int targetNodeId, long sourceTimestamp = 0, long targetTimestamp = 0);

            await DataProvider.MoveNodeAsync(sourceNodeId, targetNodeId, sourceTimestamp, targetTimestamp);
        }

        public static async Task<Dictionary<int, string>> LoadTextPropertyValuesAsync(int versionId, int[] notLoadedPropertyTypeIds)
        {
            return await DataProvider.LoadTextPropertyValuesAsync(versionId, notLoadedPropertyTypeIds);
        }
        public static Task<BinaryDataValue> LoadBinaryPropertyValueAsync(int versionId, int propertyTypeId)
        {
            return DataProvider.LoadBinaryPropertyValueAsync(versionId, propertyTypeId);
        }

        public static async Task<bool> NodeExistsAsync(string path)
        {
            return await DataProvider.NodeExistsAsync(path);
        }

        /* =============================================================================================== NodeHead */

        public static async Task<NodeHead> LoadNodeHeadAsync(string path)
        {
            return await DataProvider.LoadNodeHeadAsync(path);
        }
        public static async Task<NodeHead> LoadNodeHeadAsync(int nodeId)
        {
            return await DataProvider.LoadNodeHeadAsync(nodeId);
        }
        public static async Task<NodeHead> LoadNodeHeadByVersionIdAsync(int versionId)
        {
            return await DataProvider.LoadNodeHeadByVersionIdAsync(versionId);
        }
        public static async Task<IEnumerable<NodeHead>> LoadNodeHeadsAsync(IEnumerable<int> heads)
        {
            return await DataProvider.LoadNodeHeadsAsync(heads);
        }
        public static async Task<NodeHead.NodeVersion[]> GetNodeVersionsAsync(int nodeId)
        {
            return await DataProvider.GetNodeVersions(nodeId);
        }
        public static async Task<IEnumerable<VersionNumber>> GetVersionNumbersAsync(int nodeId)
        {
            return await DataProvider.GetVersionNumbersAsync(nodeId);
        }
        public static async Task<IEnumerable<VersionNumber>> GetVersionNumbersAsync(string path)
        {
            return await DataProvider.GetVersionNumbersAsync(path);
        }

        /* =============================================================================================== NodeQuery */

        public static async Task<int> InstanceCountAsync(int[] nodeTypeIds)
        {
            return await DataProvider.InstanceCountAsync(nodeTypeIds);
        }
        public static async Task<IEnumerable<int>> GetChildrenIdentfiersAsync(int parentId)
        {
            return await DataProvider.GetChildrenIdentfiersAsync(parentId);
        }
        public static async Task<IEnumerable<int>> QueryNodesByPathAsync(string pathStart, bool orderByPath)
        {
            return await DataProvider.QueryNodesByPathAsync(pathStart, orderByPath);
        }
        public static async Task<IEnumerable<int>> QueryNodesByTypeAsync(int[] nodeTypeIds)
        {
            return await DataProvider.QueryNodesByTypeAsync(nodeTypeIds);
        }
        public static async Task<IEnumerable<int>> QueryNodesByTypeAndPathAsync(int[] nodeTypeIds, string pathStart, bool orderByPath)
        {
            return await DataProvider.QueryNodesByTypeAndPathAsync(nodeTypeIds, pathStart, orderByPath);
        }
        public static async Task<IEnumerable<int>> QueryNodesByTypeAndPathAsync(int[] nodeTypeIds, string[] pathStart, bool orderByPath)
        {
            return await DataProvider.QueryNodesByTypeAndPathAsync(nodeTypeIds, pathStart, orderByPath);
        }
        public static async Task<IEnumerable<int>> QueryNodesByTypeAndPathAndNameAsync(int[] nodeTypeIds, string pathStart, bool orderByPath, string name)
        {
            return await DataProvider.QueryNodesByTypeAndPathAndNameAsync(nodeTypeIds, pathStart, orderByPath, name);
        }
        public static async Task<IEnumerable<int>> QueryNodesByTypeAndPathAndNameAsync(int[] nodeTypeIds, string[] pathStart, bool orderByPath, string name)
        {
            return await DataProvider.QueryNodesByTypeAndPathAndNameAsync(nodeTypeIds, pathStart, orderByPath, name);
        }
        public static async Task<IEnumerable<int>> QueryNodesByTypeAndPathAndPropertyAsync(int[] nodeTypeIds, string pathStart, bool orderByPath, List<QueryPropertyData> properties)
        {
            return await DataProvider.QueryNodesByTypeAndPathAndPropertyAsync(nodeTypeIds, pathStart, orderByPath, properties);
        }
        public static async Task<IEnumerable<int>> QueryNodesByReferenceAndTypeAsync(string referenceName, int referredNodeId, int[] nodeTypeIds)
        {
            return await DataProvider.QueryNodesByReferenceAndTypeAsync(referenceName, referredNodeId, nodeTypeIds);
        }

        /* =============================================================================================== Tree */

        public static async Task<IEnumerable<NodeType>> LoadChildTypesToAllowAsync(int nodeId)
        {
            return await DataProvider.LoadChildTypesToAllowAsync(nodeId);
        }
        public static async Task<List<ContentListType>> GetContentListTypesInTreeAsync(string path)
        {
            return await DataProvider.GetContentListTypesInTreeAsync(path);
        }

        /* =============================================================================================== TreeLock */

        public static async Task<int> AcquireTreeLockAsync(string path)
        {
            return await DataProvider.AcquireTreeLockAsync(path);
        }
        public static async Task<bool> IsTreeLockedAsync(string path)
        {
            return await DataProvider.IsTreeLockedAsync(path);
        }
        public static async void ReleaseTreeLockAsync(int[] lockIds)
        {
            await DataProvider.ReleaseTreeLockAsync(lockIds);
        }
        public static async Task<Dictionary<int, string>> LoadAllTreeLocksAsync()
        {
            return await DataProvider.LoadAllTreeLocksAsync();
        }

        /* =============================================================================================== IndexDocument */

        public static async Task SaveIndexDocumentAsync(NodeData nodeData, IndexDocument indexDoc)
        {
            await DataProvider.SaveIndexDocumentAsync(nodeData, indexDoc);
        }

        public static async Task<IEnumerable<IndexDocumentData>> LoadIndexDocumentsAsync(IEnumerable<int> versionIds)
        {
            return await DataProvider.LoadIndexDocumentsAsync(versionIds);
        }
        public static async Task<IEnumerable<IndexDocumentData>> LoadIndexDocumentsAsync(string path, int[] excludedNodeTypes)
        {
            return await DataProvider.LoadIndexDocumentsAsync(path, excludedNodeTypes);
        }

        public static async Task<IEnumerable<int>> LoadNotIndexedNodeIdsAsync(int fromId, int toId)
        {
            return await DataProvider.LoadNotIndexedNodeIdsAsync(fromId, toId);
        }

        /* =============================================================================================== IndexingActivity */

        public static async Task<int> GetLastIndexingActivityIdAsync()
        {
            return await DataProvider.GetLastIndexingActivityIdAsync();
        }
        public static async Task<IIndexingActivity[]> LoadIndexingActivitiesAsync(int fromId, int toId, int count, bool executingUnprocessedActivities, IIndexingActivityFactory activityFactory)
        {
            return await DataProvider.LoadIndexingActivitiesAsync(fromId, toId, count, executingUnprocessedActivities, activityFactory);
        }
        public static async Task<IIndexingActivity[]> LoadIndexingActivitiesAsync(int[] gaps, bool executingUnprocessedActivities, IIndexingActivityFactory activityFactory)
        {
            return await DataProvider.LoadIndexingActivitiesAsync(gaps, executingUnprocessedActivities, activityFactory);
        }
        public static async Task<IIndexingActivity[]> LoadExecutableIndexingActivitiesAsync(IIndexingActivityFactory activityFactory, int maxCount, int runningTimeoutInSeconds)
        {
            return await DataProvider.LoadExecutableIndexingActivitiesAsync(activityFactory, maxCount, runningTimeoutInSeconds);
        }
        public static async Task<Tuple<IIndexingActivity[], int[]>> LoadExecutableIndexingActivitiesAsync(IIndexingActivityFactory activityFactory, int maxCount, int runningTimeoutInSeconds, int[] waitingActivityIds) //UNDONE:DB: Do not use Tuple return value
        {
            return await DataProvider.LoadExecutableIndexingActivitiesAsync(activityFactory, maxCount, runningTimeoutInSeconds, waitingActivityIds);
        }
        public static async Task RegisterIndexingActivityAsync(IIndexingActivity activity)
        {
            await DataProvider.RegisterIndexingActivityAsync(activity);
        }
        public static async Task UpdateIndexingActivityRunningStateAsync(int indexingActivityId, IndexingActivityRunningState runningState)
        {
            await DataProvider.UpdateIndexingActivityRunningStateAsync(indexingActivityId, runningState);
        }
        public static async Task RefreshIndexingActivityLockTimeAsync(int[] waitingIds)
        {
            await DataProvider.RefreshIndexingActivityLockTimeAsync(waitingIds);
        }
        public static async Task DeleteFinishedIndexingActivitiesAsync()
        {
            await DataProvider.DeleteFinishedIndexingActivitiesAsync();
        }
        public static async Task DeleteAllIndexingActivitiesAsync()
        {
            await DataProvider.DeleteAllIndexingActivitiesAsync();
        }

        /* =============================================================================================== Schema */

        public static async Task<RepositorySchemaData> LoadSchemaAsync()
        {
            return await DataProvider.LoadSchemaAsync();
        }

        public static string StartSchemaUpdate_EXPERIMENTAL(long schemaTimestamp)
        {
            return DataProvider.StartSchemaUpdate_EXPERIMENTAL(schemaTimestamp);
        }
        public static SchemaWriter CreateSchemaWriter()
        {
            return DataProvider.CreateSchemaWriter();
        }
        public static long FinishSchemaUpdate_EXPERIMENTAL(string schemaLock)
        {
            return DataProvider.FinishSchemaUpdate_EXPERIMENTAL(schemaLock);
        }

        #region Backward compatibility

        private static readonly int _contentListStartPage = 10000000;
        internal static readonly int StringPageSize = 80;
        internal static readonly int IntPageSize = 40;
        internal static readonly int DateTimePageSize = 25;
        internal static readonly int CurrencyPageSize = 15;

        public static IDictionary<DataType, int> ContentListMappingOffsets { get; } =
            new ReadOnlyDictionary<DataType, int>(new Dictionary<DataType, int>
        {
            {DataType.String, StringPageSize * _contentListStartPage},
            {DataType.Int, IntPageSize * _contentListStartPage},
            {DataType.DateTime, DateTimePageSize * _contentListStartPage},
            {DataType.Currency, CurrencyPageSize * _contentListStartPage},
            {DataType.Binary, 0},
            {DataType.Reference, 0},
            {DataType.Text, 0}
        });

        #endregion

        /* =============================================================================================== Logging */

        public static async Task WriteAuditEventAsync(AuditEventInfo auditEvent)
        {
            await DataProvider.WriteAuditEventAsync(auditEvent);
        }

        /* =============================================================================================== Tools */

        public static DateTime RoundDateTime(DateTime d)
        {
            return DataProvider.RoundDateTime(d);
        }
        public static bool IsCacheableText(string value)
        {
            return DataProvider.IsCacheableText(value);
        }
        public static async Task<string> GetNameOfLastNodeWithNameBaseAsync(int parentId, string namebase, string extension)
        {
            return await DataProvider.GetNameOfLastNodeWithNameBaseAsync(parentId, namebase, extension);
        }
        public static async Task<long> GetTreeSizeAsync(string path, bool includeChildren)
        {
            return await DataProvider.GetTreeSizeAsync(path, includeChildren);
        }
        public static async Task<int> GetVersionCountAsync(string path)
        {
            return await DataProvider.GetVersionCountAsync(path);
        }

        public static IMetaQueryEngine MetaQueryEngine { get; } = new NullMetaQueryEngine();

        /* =============================================================================================== */

        private static readonly string NodeDataPrefix = "NodeData.";
        internal static string GenerateNodeDataVersionIdCacheKey(int versionId)
        {
            return string.Concat(NodeDataPrefix, versionId);
        }

        internal static void CacheNodeData(NodeData nodeData, string cacheKey = null)
        {
            if (nodeData == null)
                throw new ArgumentNullException(nameof(nodeData));
            if (cacheKey == null)
                cacheKey = GenerateNodeDataVersionIdCacheKey(nodeData.VersionId);
            var dependency = CacheDependencyFactory.CreateNodeDataDependency(nodeData);
            DistributedApplication.Cache.Insert(cacheKey, nodeData, dependency);
        }


        //UNDONE:DB -------Delete CheckTimestamps feature
        private static void AssertNodeTimestampIncremented(NodeData nodeData, long nodeTimestampBefore)
        {
            if (nodeData.NodeTimestamp <= nodeTimestampBefore)
                throw new Exception("NodeTimestamp need to be incremented.");
        }
        private static void AssertVersionTimestampIncremented(NodeData nodeData, long versionTimestampBefore)
        {
            if (nodeData.VersionTimestamp <= versionTimestampBefore)
                throw new Exception("VersionTimestamp need to be incremented.");
        }

        //UNDONE:DB -------Delete GetNodeTimestamp feature
        public static long GetNodeTimestamp(int nodeId)
        {
            return DataProvider.GetNodeTimestamp(nodeId);
        }
        //UNDONE:DB -------Delete GetVersionTimestamp feature
        public static long GetVersionTimestamp(int versionId)
        {
            return DataProvider.GetVersionTimestamp(versionId);
        }

        //UNDONE:DB -------Remove DataStore.AddSnapshot
        public static void AddSnapshot(string name, object snapshot)
        {
            if (!SnapshotsEnabled)
                return;

            Snapshots.Add(new SnapshotItem
            {
                Name = name,
                IsDp2 = Enabled,
                Snapshot = snapshot
            });
        }
    }
}