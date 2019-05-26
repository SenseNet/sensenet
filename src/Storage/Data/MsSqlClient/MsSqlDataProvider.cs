using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Data.SqlClient;
using SenseNet.ContentRepository.Storage.DataModel;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.Diagnostics;
using SenseNet.Search.Indexing;

namespace SenseNet.Storage.Data.MsSqlClient
{
    public class MsSqlDataProvider : DataProvider2
    {
        public override DateTime DateTimeMinValue { get; } = new DateTime(1753, 1, 1, 12, 0, 0);

        public override Task InsertNodeAsync(NodeHeadData nodeHeadData, VersionData versionData, DynamicPropertyData dynamicData,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException(); //UNDONE:DB@ NotImplementedException
        }

        public override Task UpdateNodeAsync(NodeHeadData nodeHeadData, VersionData versionData, DynamicPropertyData dynamicData,
            IEnumerable<int> versionIdsToDelete, string originalPath = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException(); //UNDONE:DB@ NotImplementedException
        }

        public override Task CopyAndUpdateNodeAsync(NodeHeadData nodeHeadData, VersionData versionData, DynamicPropertyData dynamicData,
            IEnumerable<int> versionIdsToDelete, int expectedVersionId = 0, string originalPath = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException(); //UNDONE:DB@ NotImplementedException
        }

        public override Task UpdateNodeHeadAsync(NodeHeadData nodeHeadData, IEnumerable<int> versionIdsToDelete,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException(); //UNDONE:DB@ NotImplementedException
        }

        public override Task<IEnumerable<NodeData>> LoadNodesAsync(int[] versionIds, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException(new StackTrace().GetFrame(0).GetMethod().Name); //UNDONE:DB@ NotImplementedException
        }

        public override Task DeleteNodeAsync(NodeHeadData nodeHeadData, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException(); //UNDONE:DB@ NotImplementedException
        }

        public override Task MoveNodeAsync(NodeHeadData sourceNodeHeadData, int targetNodeId, long targetTimestamp,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException(); //UNDONE:DB@ NotImplementedException
        }

        public override Task<Dictionary<int, string>> LoadTextPropertyValuesAsync(int versionId, int[] notLoadedPropertyTypeIds,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException(); //UNDONE:DB@ NotImplementedException
        }

        public override Task<BinaryDataValue> LoadBinaryPropertyValueAsync(int versionId, int propertyTypeId,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException(); //UNDONE:DB@ NotImplementedException
        }

        public override Task<bool> NodeExistsAsync(string path, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException(); //UNDONE:DB@ NotImplementedException
        }

        public override Task<NodeHead> LoadNodeHeadAsync(string path, CancellationToken cancellationToken = default(CancellationToken))
        {
            var sql = string.Format(LoadNodeHeadSkeleton,
                $"LoadNodeHead by Path {path}",
                "",
                "Node.Path = @Path COLLATE Latin1_General_CI_AS");
            throw new NotImplementedException(); //UNDONE:DB@ NotImplementedException
        }

        private static readonly string LoadNodeHeadSkeleton = @"-- {0}
SELECT
    Node.NodeId,             -- 0
    Node.Name,               -- 1
    Node.DisplayName,        -- 2
    Node.Path,               -- 3
    Node.ParentNodeId,       -- 4
    Node.NodeTypeId,         -- 5
    Node.ContentListTypeId,  -- 6
    Node.ContentListId,      -- 7
    Node.CreationDate,       -- 8
    Node.ModificationDate,   -- 9
    Node.LastMinorVersionId, -- 10
    Node.LastMajorVersionId, -- 11
    Node.OwnerId,            -- 12
    Node.CreatedById,        -- 13
    Node.ModifiedById,       -- 14
    Node.[Index],            -- 15
    Node.LockedById,         -- 16
    Node.Timestamp           -- 17
FROM
    Nodes Node
    {1}
WHERE 
    {2}
";
        public override async Task<NodeHead> LoadNodeHeadAsync(int nodeId, CancellationToken cancellationToken = default(CancellationToken))
        {
            var sql = string.Format(LoadNodeHeadSkeleton,
                $"LoadNodeHead by NodeId {nodeId}",
                "",
                "Node.NodeId = @NodeId");

            return await MsSqlProcedure.ExecuteAsync<NodeHead>(sql, cmd =>
            {
                cmd.Parameters.Add("@NodeId", SqlDbType.Int).Value = nodeId;
            }, reader =>
            {
                if (!reader.Read())
                    return null;
                return GetNodeHeadFromReader(reader);
            });
        }

        public override Task<NodeHead> LoadNodeHeadByVersionIdAsync(int versionId, CancellationToken cancellationToken = default(CancellationToken))
        {
            var sql = string.Format(LoadNodeHeadSkeleton,
                $"LoadNodeHead by VersionId {versionId}",
                "JOIN Versions V ON V.NodeId = Node.NodeId",
                "V.VersionId = @VersionId");
            throw new NotImplementedException(); //UNDONE:DB@ NotImplementedException
        }

        private NodeHead GetNodeHeadFromReader(SqlDataReader reader)
        {
            return new NodeHead(
                reader.GetInt32(0),         // nodeId,
                reader.GetString(1),        // name,
                reader.GetSafeString(2),    // displayName,
                reader.GetString(3),        // pathInDb,
                reader.GetSafeInt32(4),     // parentNodeId,
                reader.GetInt32(5),         // nodeTypeId,
                reader.GetSafeInt32(6),     // contentListTypeId,
                reader.GetSafeInt32(7),     // contentListId,
                reader.GetDateTimeUtc(8),   // creationDate,
                reader.GetDateTimeUtc(9),   // modificationDate,
                reader.GetSafeInt32(10),    // lastMinorVersionId,
                reader.GetSafeInt32(11),    // lastMajorVersionId,
                reader.GetSafeInt32(12),    // ownerId,
                reader.GetSafeInt32(13),    // creatorId,
                reader.GetSafeInt32(14),    // modifierId,
                reader.GetSafeInt32(15),    // index,
                reader.GetSafeInt32(16),    // lockerId
                GetLongFromBytes((byte[])reader.GetValue(17))     // timestamp
            );
        }

        public override Task<IEnumerable<NodeHead>> LoadNodeHeadsAsync(IEnumerable<int> heads, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException(); //UNDONE:DB@ NotImplementedException
        }

        public override Task<NodeHead.NodeVersion[]> GetNodeVersions(int nodeId, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException(); //UNDONE:DB@ NotImplementedException
        }

        public override Task<IEnumerable<VersionNumber>> GetVersionNumbersAsync(int nodeId, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException(); //UNDONE:DB@ NotImplementedException
        }

        public override Task<IEnumerable<VersionNumber>> GetVersionNumbersAsync(string path, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException(); //UNDONE:DB@ NotImplementedException
        }

        public override Task<int> InstanceCountAsync(int[] nodeTypeIds, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException(); //UNDONE:DB@ NotImplementedException
        }

        public override Task<IEnumerable<int>> GetChildrenIdentfiersAsync(int parentId, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException(); //UNDONE:DB@ NotImplementedException
        }

        public override Task<IEnumerable<int>> QueryNodesByTypeAndPathAndNameAsync(int[] nodeTypeIds, string[] pathStart, bool orderByPath, string name,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException(); //UNDONE:DB@ NotImplementedException
        }

        public override Task<IEnumerable<int>> QueryNodesByTypeAndPathAndPropertyAsync(int[] nodeTypeIds, string pathStart, bool orderByPath, List<QueryPropertyData> properties,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException(); //UNDONE:DB@ NotImplementedException
        }

        public override Task<IEnumerable<int>> QueryNodesByReferenceAndTypeAsync(string referenceName, int referredNodeId, int[] nodeTypeIds,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException(); //UNDONE:DB@ NotImplementedException
        }

        public override Task<IEnumerable<NodeType>> LoadChildTypesToAllowAsync(int nodeId, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException(); //UNDONE:DB@ NotImplementedException
        }

        public override Task<List<ContentListType>> GetContentListTypesInTreeAsync(string path, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException(); //UNDONE:DB@ NotImplementedException
        }

        public override Task<int> AcquireTreeLockAsync(string path, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException(); //UNDONE:DB@ NotImplementedException
        }

        public override Task<bool> IsTreeLockedAsync(string path, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException(); //UNDONE:DB@ NotImplementedException
        }

        public override Task ReleaseTreeLockAsync(int[] lockIds, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException(); //UNDONE:DB@ NotImplementedException
        }

        public override Task<Dictionary<int, string>> LoadAllTreeLocksAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException(); //UNDONE:DB@ NotImplementedException
        }

        public override Task SaveIndexDocumentAsync(NodeData nodeData, IndexDocument indexDoc,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException(); //UNDONE:DB@ NotImplementedException
        }

        public override Task SaveIndexDocumentAsync(int versionId, IndexDocument indexDoc,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException(); //UNDONE:DB@ NotImplementedException
        }

        public override Task<IEnumerable<IndexDocumentData>> LoadIndexDocumentsAsync(IEnumerable<int> versionIds, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException(); //UNDONE:DB@ NotImplementedException
        }

        public override Task<IEnumerable<IndexDocumentData>> LoadIndexDocumentsAsync(string path, int[] excludedNodeTypes,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException(); //UNDONE:DB@ NotImplementedException
        }

        public override Task<IEnumerable<int>> LoadNotIndexedNodeIdsAsync(int fromId, int toId, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException(); //UNDONE:DB@ NotImplementedException
        }

        public override Task<int> GetLastIndexingActivityIdAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException(); //UNDONE:DB@ NotImplementedException
        }

        public override Task<IIndexingActivity[]> LoadIndexingActivitiesAsync(int fromId, int toId, int count, bool executingUnprocessedActivities,
            IIndexingActivityFactory activityFactory, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException(); //UNDONE:DB@ NotImplementedException
        }

        public override Task<IIndexingActivity[]> LoadIndexingActivitiesAsync(int[] gaps, bool executingUnprocessedActivities,
            IIndexingActivityFactory activityFactory, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException(); //UNDONE:DB@ NotImplementedException
        }

        public override Task<ExecutableIndexingActivitiesResult> LoadExecutableIndexingActivitiesAsync(IIndexingActivityFactory activityFactory, int maxCount,
            int runningTimeoutInSeconds, int[] waitingActivityIds,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException(); //UNDONE:DB@ NotImplementedException
        }

        public override Task RegisterIndexingActivityAsync(IIndexingActivity activity,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException(); //UNDONE:DB@ NotImplementedException
        }

        public override Task UpdateIndexingActivityRunningStateAsync(int indexingActivityId, IndexingActivityRunningState runningState,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException(); //UNDONE:DB@ NotImplementedException
        }

        public override Task RefreshIndexingActivityLockTimeAsync(int[] waitingIds,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException(); //UNDONE:DB@ NotImplementedException
        }

        public override Task DeleteFinishedIndexingActivitiesAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException(); //UNDONE:DB@ NotImplementedException
        }

        public override Task DeleteAllIndexingActivitiesAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException(); //UNDONE:DB@ NotImplementedException
        }

        public override Task<RepositorySchemaData> LoadSchemaAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException(); //UNDONE:DB@ NotImplementedException
        }

        public override SchemaWriter CreateSchemaWriter()
        {
            throw new NotImplementedException(); //UNDONE:DB@ NotImplementedException
        }

        public override Task<string> StartSchemaUpdateAsync(long schemaTimestamp, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException(); //UNDONE:DB@ NotImplementedException
        }

        public override Task<long> FinishSchemaUpdateAsync(string schemaLock, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException(); //UNDONE:DB@ NotImplementedException
        }

        public override Task WriteAuditEventAsync(AuditEventInfo auditEvent, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException(); //UNDONE:DB@ NotImplementedException
        }

        public override DateTime RoundDateTime(DateTime d)
        {
            throw new NotImplementedException(); //UNDONE:DB@ NotImplementedException
        }

        public override bool IsCacheableText(string text)
        {
            throw new NotImplementedException(); //UNDONE:DB@ NotImplementedException
        }

        public override Task<string> GetNameOfLastNodeWithNameBaseAsync(int parentId, string namebase, string extension,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException(); //UNDONE:DB@ NotImplementedException
        }

        public override Task<long> GetTreeSizeAsync(string path, bool includeChildren,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException(); //UNDONE:DB@ NotImplementedException
        }

        public override Task<int> GetNodeCountAsync(string path, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException(); //UNDONE:DB@ NotImplementedException
        }

        public override Task<int> GetVersionCountAsync(string path, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException(); //UNDONE:DB@ NotImplementedException
        }

        public override async Task InstallInitialDataAsync(InitialData data, CancellationToken cancellationToken = default(CancellationToken))
        {
            await MsSqlDataInstaller.InstallInitialDataAsync(data, this, ConnectionStrings.ConnectionString);
        }

        public override Task<IEnumerable<EntityTreeNodeData>> LoadEntityTreeAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException(); //UNDONE:DB@ NotImplementedException
        }


        /* ======================================================================================================= TOOLS */

        internal static long GetLongFromBytes(byte[] bytes)
        {
            var @long = 0L;
            for (int i = 0; i < bytes.Length; i++)
                @long = (@long << 8) + bytes[i];
            return @long;
        }

        internal static byte[] GetBytesFromLong(long @long)
        {
            var bytes = new byte[8];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[7 - i] = (byte)(@long & 0xFF);
                @long = @long >> 8;
            }
            return bytes;
        }

    }
}
