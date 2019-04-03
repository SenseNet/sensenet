using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Caching.Dependency;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.Search.Indexing;

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


        private static DataProvider2 DataProvider => Providers.Instance.DataProvider2;
        public static DateTime DateTimeMinValue { get; } = DateTime.MinValue; //UNDONE:DB ---- DataStore.DateTimeMinValue

        /* ============================================================================================================= Installation */

        public static void InstallDefaultStructure() //UNDONE:DB ------Implement well: InstallDefaultStructure
        {
            DataProvider.InstallDefaultStructure();
        }

        /* ============================================================================================================= Nodes */

        public static async Task SaveNodeAsync(NodeData nodeData, NodeSaveSettings settings, CancellationToken cancellationToken)
        {
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

            var isNewNode = nodeData.Id == 0; // shortcut

            //// Finalize path
            //string path;
            //if (nodeData.Id != Identifiers.PortalRootId)
            //{
            //    var parent = NodeHead.Get(nodeData.ParentId);
            //    if (parent == null)
            //        throw new ContentNotFoundException(nodeData.ParentId.ToString());
            //    path = RepositoryPath.Combine(parent.Path, nodeData.Name);
            //}
            //else
            //{
            //    path = Identifiers.RootPath;
            //}
            //Node.AssertPath(path);
            //nodeData.Path = path;

            // SAVE DATA
            // Do not block any exception from the called methods.
            // If need a catch block rethrow away the exception.
            SaveResult saveResult = null; // comes from DataProvider methods

            var savingAlgorithm = settings.GetSavingAlgorithm();
            if (settings.NeedToSaveData) //UNDONE: This decision by "NeedToSaveData" is provider responsibility.
            {
                const string saveResultCannorBeNull = "Return value saveResult cannot be null.";
                switch (savingAlgorithm)
                {
                    case SavingAlgorithm.CreateNewNode:
                        saveResult = await DataProvider.InsertNodeAsync(nodeData, savingAlgorithm);
                        if (saveResult == null)
                            throw new InvalidOperationException(saveResultCannorBeNull);
                        break;
                    case SavingAlgorithm.UpdateSameVersion:
                        saveResult = await DataProvider.UpdateNodeAsync(nodeData, settings.DeletableVersionIds);
                        if (saveResult == null)
                            throw new InvalidOperationException(saveResultCannorBeNull);
                        break;
                    case SavingAlgorithm.CopyToNewVersionAndUpdate:
                        saveResult = await DataProvider.CopyAndUpdateNodeAsync(nodeData, settings.CurrentVersionId,
                            settings.DeletableVersionIds);
                        if (saveResult == null)
                            throw new InvalidOperationException(saveResultCannorBeNull);
                        break;
                    case SavingAlgorithm.CopyToSpecifiedVersionAndUpdate:
                        saveResult = await DataProvider.CopyAndUpdateNodeAsync(nodeData, settings.CurrentVersionId,
                            settings.ExpectedVersionId, settings.DeletableVersionIds);
                        if (saveResult == null)
                            throw new InvalidOperationException(saveResultCannorBeNull);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("Unknown SavingAlgorithm: " + savingAlgorithm);
                }

                if (!isNewNode && nodeData.PathChanged && nodeData.SharedData != null)
                    await DataProvider.UpdateSubTreePathAsync(nodeData.SharedData.Path, nodeData.Path);

                //UNDONE:DB MISSING LOGICAL STEP: SaveNodeProperties(nodeData, savingAlgorithm, writer, isNewNode);
            }
            else
            {
                await DataProvider.UpdateNodeHeadAsync(nodeData);
            }

            // Deletable checker method
            AssertTimestampsIncremented(saveResult, nodeTimestampBefore, versionTimestampBefore); //UNDONE:DB -------Delete CheckTimestamps feature

            if (saveResult != null)
            {
                // Write successful modifications after all checks or transaction commit.
                if (saveResult.NodeId >= 0)
                    nodeData.Id = saveResult.NodeId;
                if (saveResult.VersionId >= 0)
                    nodeData.VersionId = saveResult.VersionId;
                if (saveResult.NodeTimestamp >= 0L)
                    nodeData.NodeTimestamp = saveResult.NodeTimestamp;
                if (saveResult.VersionTimestamp >= 0L)
                    nodeData.VersionTimestamp = saveResult.VersionTimestamp;
                if (saveResult.LastMajorVersionId >= 0)
                    settings.LastMajorVersionIdAfter = saveResult.LastMajorVersionId;
                if (saveResult.LastMinorVersionId >= 0)
                    settings.LastMinorVersionIdAfter = saveResult.LastMinorVersionId;
            }
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

        /* ============================================================================================================= NodeHead */

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
        public static async Task<NodeHead.NodeVersion[]> GetNodeVersions(int nodeId)
        {
            return await DataProvider.GetNodeVersions(nodeId);
        }

        /* ============================================================================================================= IndexDocument */

        public static async Task<SaveResult> SaveIndexDocumentAsync(NodeData nodeData, IndexDocument indexDoc)
        {
            return await DataProvider.SaveIndexDocumentAsync(nodeData, indexDoc);
        }

        /* ============================================================================================================= Schema */

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

        /* ============================================================================================================= Tools */

        public static DateTime RoundDateTime(DateTime d)
        {
            return DataProvider.RoundDateTime(d);
        }

        /* ============================================================================================================= */

        private static readonly string NodeDataPrefix = "NodeData.";
        private static string GenerateNodeDataVersionIdCacheKey(int versionId)
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


        private static void AssertTimestampsIncremented(SaveResult saveResult, long nodeTimestampBefore, long versionTimestampBefore) //UNDONE:DB -------Delete CheckTimestamps feature
        {
            if (saveResult.NodeTimestamp <= nodeTimestampBefore)
                throw new Exception("NodeTimestamp need to be incremented.");
            if (saveResult.VersionTimestamp <= versionTimestampBefore)
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