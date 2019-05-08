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

        public static async Task InstallInitialDataAsync(InitialData data, CancellationToken cancellationToken = default(CancellationToken))
        {
            await DataProvider.InstallInitialDataAsync(data, cancellationToken);
        }

        public static Task<IEnumerable<EntityTreeNodeData>> LoadEntityTreeAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return DataProvider.LoadEntityTreeAsync(cancellationToken);
        }

        /* =============================================================================================== Nodes */

        public static async Task SaveNodeAsync(NodeData nodeData, NodeSaveSettings settings, CancellationToken cancellationToken = default(CancellationToken))
        {
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
            var renamed = !isNewNode && nodeData.PathChanged && nodeData.SharedData != null;
            if (settings.NeedToSaveData)
            {
                var versionData = nodeData.GetVersionData();
                DynamicPropertyData dynamicData;
                switch (savingAlgorithm)
                {
                    case SavingAlgorithm.CreateNewNode:
                        dynamicData = nodeData.GetDynamicData(false);
                        await DataProvider.InsertNodeAsync(nodeHeadData, versionData, dynamicData, cancellationToken);
                        // Write back the new NodeId
                        nodeData.Id = nodeHeadData.NodeId;
                        break;
                    case SavingAlgorithm.UpdateSameVersion:
                        dynamicData = nodeData.GetDynamicData(false);
                        if(renamed)
                            await DataProvider.UpdateNodeAsync(
                                nodeHeadData, versionData, dynamicData, settings.DeletableVersionIds, nodeData.SharedData.Path, cancellationToken);
                        else
                            await DataProvider.UpdateNodeAsync(
                                nodeHeadData, versionData, dynamicData, settings.DeletableVersionIds, null, cancellationToken);
                        break;
                    case SavingAlgorithm.CopyToNewVersionAndUpdate:
                        dynamicData = nodeData.GetDynamicData(true);
                        if (renamed)
                            // Copy to brand new version and rename
                            await DataProvider.CopyAndUpdateNodeAsync(
                                nodeHeadData, versionData, dynamicData, settings.DeletableVersionIds, 0,
                                nodeData.SharedData.Path, cancellationToken);
                        else
                            // Copy to brand new version
                            await DataProvider.CopyAndUpdateNodeAsync(
                                nodeHeadData, versionData, dynamicData, settings.DeletableVersionIds, 0, null, cancellationToken);
                        break;
                    case SavingAlgorithm.CopyToSpecifiedVersionAndUpdate:
                        dynamicData = nodeData.GetDynamicData(true);
                        if (renamed)
                            // Copy to specified version and rename
                            await DataProvider.CopyAndUpdateNodeAsync(
                                nodeHeadData, versionData, dynamicData, settings.DeletableVersionIds, settings.ExpectedVersionId,
                                nodeData.SharedData.Path, cancellationToken);
                        else
                            // Copy to specified version
                            await DataProvider.CopyAndUpdateNodeAsync(
                                nodeHeadData, versionData, dynamicData, settings.DeletableVersionIds, settings.ExpectedVersionId,
                                null, cancellationToken);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("Unknown SavingAlgorithm: " + savingAlgorithm);
                }
                // Write back the version level changed values
                nodeData.VersionId = versionData.VersionId;
                nodeData.VersionTimestamp = versionData.Timestamp;

                //if (!isNewNode && nodeData.PathChanged && nodeData.SharedData != null)
                //    await DataProvider.UpdateSubTreePathAsync(nodeData.SharedData.Path, nodeData.Path);
            }
            else
            {
                await DataProvider.UpdateNodeHeadAsync(nodeHeadData, settings.DeletableVersionIds, cancellationToken);
            }
            // Write back NodeHead level changed values
            settings.LastMajorVersionIdAfter = nodeHeadData.LastMajorVersionId;
            settings.LastMinorVersionIdAfter = nodeHeadData.LastMinorVersionId;
            nodeData.NodeTimestamp = nodeHeadData.Timestamp;
        }
        public static async Task<NodeToken[]> LoadNodesAsync(NodeHead[] headArray, int[] versionIdArray, CancellationToken cancellationToken = default(CancellationToken))
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
                var loadedCollection = await DataProvider.LoadNodesAsync(versionIds, cancellationToken);
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
        public static async Task DeleteNodeAsync(NodeData nodeData, CancellationToken cancellationToken = default(CancellationToken))
        {
            // ORIGINAL SIGNATURES:
            // internal void DeleteNode(int nodeId)
            // internal void DeleteNodePsychical(int nodeId, long timestamp)
            // protected internal abstract DataOperationResult DeleteNodeTree(int nodeId);
            // protected internal abstract DataOperationResult DeleteNodeTreePsychical(int nodeId, long timestamp);
            // -------------------
            // The word as suffix "Tree" is unnecessary, "Psychical" is misleading.

            await DataProvider.DeleteNodeAsync(nodeData.GetNodeHeadData(), cancellationToken);
        }
        public static async Task MoveNodeAsync(NodeData sourceNodeData, int targetNodeId, long targetTimestamp, CancellationToken cancellationToken = default(CancellationToken))
        {
            // ORIGINAL SIGNATURES:
            // internal void MoveNode(int sourceNodeId, int targetNodeId, long sourceTimestamp, long targetTimestamp)
            // protected internal abstract DataOperationResult MoveNodeTree(int sourceNodeId, int targetNodeId, long sourceTimestamp = 0, long targetTimestamp = 0);
            var sourceNodeHeadData = sourceNodeData.GetNodeHeadData();
            await DataProvider.MoveNodeAsync(sourceNodeHeadData, targetNodeId, targetTimestamp, cancellationToken);
        }

        public static Task<Dictionary<int, string>> LoadTextPropertyValuesAsync(int versionId, int[] notLoadedPropertyTypeIds, CancellationToken cancellationToken = default(CancellationToken))
        {
            return DataProvider.LoadTextPropertyValuesAsync(versionId, notLoadedPropertyTypeIds, cancellationToken);
        }
        public static Task<BinaryDataValue> LoadBinaryPropertyValueAsync(int versionId, int propertyTypeId, CancellationToken cancellationToken = default(CancellationToken))
        {
            return DataProvider.LoadBinaryPropertyValueAsync(versionId, propertyTypeId, cancellationToken);
        }

        public static Task<bool> NodeExistsAsync(string path, CancellationToken cancellationToken = default(CancellationToken))
        {
            return DataProvider.NodeExistsAsync(path, cancellationToken);
        }

        /* =============================================================================================== NodeHead */

        public static Task<NodeHead> LoadNodeHeadAsync(string path, CancellationToken cancellationToken = default(CancellationToken))
        {
            return DataProvider.LoadNodeHeadAsync(path, cancellationToken);
        }
        public static Task<NodeHead> LoadNodeHeadAsync(int nodeId, CancellationToken cancellationToken = default(CancellationToken))
        {
            return DataProvider.LoadNodeHeadAsync(nodeId, cancellationToken);
        }
        public static Task<NodeHead> LoadNodeHeadByVersionIdAsync(int versionId, CancellationToken cancellationToken = default(CancellationToken))
        {
            return DataProvider.LoadNodeHeadByVersionIdAsync(versionId, cancellationToken);
        }
        public static Task<IEnumerable<NodeHead>> LoadNodeHeadsAsync(IEnumerable<int> heads, CancellationToken cancellationToken = default(CancellationToken))
        {
            return DataProvider.LoadNodeHeadsAsync(heads, cancellationToken);
        }
        public static Task<NodeHead.NodeVersion[]> GetNodeVersionsAsync(int nodeId, CancellationToken cancellationToken = default(CancellationToken))
        {
            return DataProvider.GetNodeVersions(nodeId, cancellationToken);
        }
        public static Task<IEnumerable<VersionNumber>> GetVersionNumbersAsync(int nodeId, CancellationToken cancellationToken = default(CancellationToken))
        {
            return DataProvider.GetVersionNumbersAsync(nodeId, cancellationToken);
        }
        public static Task<IEnumerable<VersionNumber>> GetVersionNumbersAsync(string path, CancellationToken cancellationToken = default(CancellationToken))
        {
            return DataProvider.GetVersionNumbersAsync(path, cancellationToken);
        }

        /* =============================================================================================== NodeQuery */

        public static Task<int> InstanceCountAsync(int[] nodeTypeIds, CancellationToken cancellationToken = default(CancellationToken))
        {
            return DataProvider.InstanceCountAsync(nodeTypeIds, cancellationToken);
        }
        public static Task<IEnumerable<int>> GetChildrenIdentfiersAsync(int parentId, CancellationToken cancellationToken = default(CancellationToken))
        {
            return DataProvider.GetChildrenIdentfiersAsync(parentId, cancellationToken);
        }
        public static Task<IEnumerable<int>> QueryNodesByPathAsync(string pathStart, bool orderByPath, CancellationToken cancellationToken = default(CancellationToken))
        {
            return QueryNodesByTypeAndPathAsync(null, pathStart, orderByPath);
        }
        public static Task<IEnumerable<int>> QueryNodesByTypeAsync(int[] nodeTypeIds, CancellationToken cancellationToken = default(CancellationToken))
        {
            return QueryNodesByTypeAndPathAsync(nodeTypeIds, new string[0], false);
        }
        public static Task<IEnumerable<int>> QueryNodesByTypeAndPathAsync(int[] nodeTypeIds, string pathStart, bool orderByPath, CancellationToken cancellationToken = default(CancellationToken))
        {
            return QueryNodesByTypeAndPathAndNameAsync(nodeTypeIds, pathStart, orderByPath, null);
        }
        public static Task<IEnumerable<int>> QueryNodesByTypeAndPathAsync(int[] nodeTypeIds, string[] pathStart, bool orderByPath, CancellationToken cancellationToken = default(CancellationToken))
        {
            return QueryNodesByTypeAndPathAndNameAsync(nodeTypeIds, pathStart, orderByPath, null);
        }
        public static Task<IEnumerable<int>> QueryNodesByTypeAndPathAndNameAsync(int[] nodeTypeIds, string pathStart, bool orderByPath, string name, CancellationToken cancellationToken = default(CancellationToken))
        {
            return QueryNodesByTypeAndPathAndNameAsync(nodeTypeIds, new[] { pathStart }, orderByPath, name);
        }
        public static Task<IEnumerable<int>> QueryNodesByTypeAndPathAndNameAsync(int[] nodeTypeIds, string[] pathStart, bool orderByPath, string name, CancellationToken cancellationToken = default(CancellationToken))
        {
            return DataProvider.QueryNodesByTypeAndPathAndNameAsync(nodeTypeIds, pathStart, orderByPath, name, cancellationToken);
        }
        public static Task<IEnumerable<int>> QueryNodesByTypeAndPathAndPropertyAsync(int[] nodeTypeIds, string pathStart, bool orderByPath, List<QueryPropertyData> properties, CancellationToken cancellationToken = default(CancellationToken))
        {
            return DataProvider.QueryNodesByTypeAndPathAndPropertyAsync(nodeTypeIds, pathStart, orderByPath, properties, cancellationToken);
        }
        public static Task<IEnumerable<int>> QueryNodesByReferenceAndTypeAsync(string referenceName, int referredNodeId, int[] nodeTypeIds, CancellationToken cancellationToken = default(CancellationToken))
        {
            return DataProvider.QueryNodesByReferenceAndTypeAsync(referenceName, referredNodeId, nodeTypeIds, cancellationToken);
        }

        /* =============================================================================================== Tree */

        public static Task<IEnumerable<NodeType>> LoadChildTypesToAllowAsync(int nodeId, CancellationToken cancellationToken = default(CancellationToken))
        {
            return DataProvider.LoadChildTypesToAllowAsync(nodeId, cancellationToken);
        }
        public static Task<List<ContentListType>> GetContentListTypesInTreeAsync(string path, CancellationToken cancellationToken = default(CancellationToken))
        {
            return DataProvider.GetContentListTypesInTreeAsync(path, cancellationToken);
        }

        /* =============================================================================================== TreeLock */

        public static Task<int> AcquireTreeLockAsync(string path, CancellationToken cancellationToken = default(CancellationToken))
        {
            return DataProvider.AcquireTreeLockAsync(path, cancellationToken);
        }
        public static Task<bool> IsTreeLockedAsync(string path, CancellationToken cancellationToken = default(CancellationToken))
        {
            return DataProvider.IsTreeLockedAsync(path, cancellationToken);
        }
        public static Task ReleaseTreeLockAsync(int[] lockIds, CancellationToken cancellationToken = default(CancellationToken))
        {
            return DataProvider.ReleaseTreeLockAsync(lockIds, cancellationToken);
        }
        public static Task<Dictionary<int, string>> LoadAllTreeLocksAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return DataProvider.LoadAllTreeLocksAsync(cancellationToken);
        }

        /* =============================================================================================== IndexDocument */

        public static async Task SaveIndexDocumentAsync(NodeData nodeData, IndexDocument indexDoc, CancellationToken cancellationToken = default(CancellationToken))
        {
            await DataProvider.SaveIndexDocumentAsync(nodeData, indexDoc, cancellationToken);
        }
        public static async Task SaveIndexDocumentAsync(int versionId, IndexDocument indexDoc, CancellationToken cancellationToken = default(CancellationToken))
        {
            await DataProvider.SaveIndexDocumentAsync(versionId, indexDoc, cancellationToken);
        }

        public static async Task<IndexDocumentData> LoadIndexDocumentByVersionIdAsync(int versionId, CancellationToken cancellationToken = default(CancellationToken))
        {
            var result = await DataProvider.LoadIndexDocumentsAsync(new []{versionId}, cancellationToken);
            return result.FirstOrDefault();
        }
        public static Task<IEnumerable<IndexDocumentData>> LoadIndexDocumentsAsync(IEnumerable<int> versionIds, CancellationToken cancellationToken = default(CancellationToken))
        {
            return DataProvider.LoadIndexDocumentsAsync(versionIds, cancellationToken);
        }
        public static Task<IEnumerable<IndexDocumentData>> LoadIndexDocumentsAsync(string path, int[] excludedNodeTypes, CancellationToken cancellationToken = default(CancellationToken))
        {
            return DataProvider.LoadIndexDocumentsAsync(path, excludedNodeTypes, cancellationToken);
        }

        public static Task<IEnumerable<int>> LoadNotIndexedNodeIdsAsync(int fromId, int toId, CancellationToken cancellationToken = default(CancellationToken))
        {
            return DataProvider.LoadNotIndexedNodeIdsAsync(fromId, toId, cancellationToken);
        }

        /* =============================================================================================== IndexingActivity */

        public static Task<int> GetLastIndexingActivityIdAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return DataProvider.GetLastIndexingActivityIdAsync(cancellationToken);
        }
        public static Task<IIndexingActivity[]> LoadIndexingActivitiesAsync(int fromId, int toId, int count, bool executingUnprocessedActivities, IIndexingActivityFactory activityFactory, CancellationToken cancellationToken = default(CancellationToken))
        {
            return DataProvider.LoadIndexingActivitiesAsync(fromId, toId, count, executingUnprocessedActivities, activityFactory, cancellationToken);
        }
        public static Task<IIndexingActivity[]> LoadIndexingActivitiesAsync(int[] gaps, bool executingUnprocessedActivities, IIndexingActivityFactory activityFactory, CancellationToken cancellationToken = default(CancellationToken))
        {
            return DataProvider.LoadIndexingActivitiesAsync(gaps, executingUnprocessedActivities, activityFactory, cancellationToken);
        }
        //public static Task<IIndexingActivity[]> LoadExecutableIndexingActivitiesAsync(IIndexingActivityFactory activityFactory, int maxCount, int runningTimeoutInSeconds, CancellationToken cancellationToken = default(CancellationToken))
        //{
        //    return DataProvider.LoadExecutableIndexingActivitiesAsync(activityFactory, maxCount, runningTimeoutInSeconds, cancellationToken);
        //}
        public static Task<ExecutableIndexingActivitiesResult> LoadExecutableIndexingActivitiesAsync(IIndexingActivityFactory activityFactory, int maxCount, int runningTimeoutInSeconds, int[] waitingActivityIds, CancellationToken cancellationToken = default(CancellationToken))
        {
            return DataProvider.LoadExecutableIndexingActivitiesAsync(activityFactory, maxCount, runningTimeoutInSeconds, waitingActivityIds, cancellationToken);
        }
        public static async Task RegisterIndexingActivityAsync(IIndexingActivity activity, CancellationToken cancellationToken = default(CancellationToken))
        {
            await DataProvider.RegisterIndexingActivityAsync(activity, cancellationToken);
        }
        public static async Task UpdateIndexingActivityRunningStateAsync(int indexingActivityId, IndexingActivityRunningState runningState, CancellationToken cancellationToken = default(CancellationToken))
        {
            await DataProvider.UpdateIndexingActivityRunningStateAsync(indexingActivityId, runningState, cancellationToken);
        }
        public static async Task RefreshIndexingActivityLockTimeAsync(int[] waitingIds, CancellationToken cancellationToken = default(CancellationToken))
        {
            await DataProvider.RefreshIndexingActivityLockTimeAsync(waitingIds, cancellationToken);
        }
        public static async Task DeleteFinishedIndexingActivitiesAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            await DataProvider.DeleteFinishedIndexingActivitiesAsync(cancellationToken);
        }
        public static async Task DeleteAllIndexingActivitiesAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            await DataProvider.DeleteAllIndexingActivitiesAsync(cancellationToken);
        }

        /* =============================================================================================== Schema */

        public static Task<RepositorySchemaData> LoadSchemaAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return DataProvider.LoadSchemaAsync(cancellationToken);
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

        public static async Task WriteAuditEventAsync(AuditEventInfo auditEvent, CancellationToken cancellationToken = default(CancellationToken))
        {
            await DataProvider.WriteAuditEventAsync(auditEvent, cancellationToken);
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
        public static Task<string> GetNameOfLastNodeWithNameBaseAsync(int parentId, string namebase, string extension, CancellationToken cancellationToken = default(CancellationToken))
        {
            return DataProvider.GetNameOfLastNodeWithNameBaseAsync(parentId, namebase, extension, cancellationToken);
        }
        public static Task<long> GetTreeSizeAsync(string path, bool includeChildren, CancellationToken cancellationToken = default(CancellationToken))
        {
            return DataProvider.GetTreeSizeAsync(path, includeChildren, cancellationToken);
        }
        public static Task<int> GetNodeCountAsync(string path = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return DataProvider.GetNodeCountAsync(path, cancellationToken);
        }
        public static Task<int> GetVersionCountAsync(string path = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return DataProvider.GetVersionCountAsync(path, cancellationToken);
        }
        public static Task<long> GetNodeTimestampAsync(int nodeId, CancellationToken cancellationToken = default(CancellationToken))
        {
            return DataProvider.GetNodeTimestampAsync(nodeId, cancellationToken);
        }
        public static Task<long> GetVersionTimestampAsync(int versionId, CancellationToken cancellationToken = default(CancellationToken))
        {
            return DataProvider.GetVersionTimestampAsync(versionId, cancellationToken);
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
    }
}