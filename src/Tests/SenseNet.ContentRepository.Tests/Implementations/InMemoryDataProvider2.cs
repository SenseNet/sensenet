using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Schema;

namespace SenseNet.ContentRepository.Tests.Implementations
{
    //UNDONE:DB -------Delete original InMemoryDataProvider and use this. Move to the Tests project
    public class InMemoryDataProvider2 : DataProvider2
    {
        // ReSharper disable once InconsistentNaming
        private int __lastNodeId = 1247; // Uses the GetNextNodeId() method.
        // ReSharper disable once InconsistentNaming
        private long __lastNodeTimestamp;    // Uses the GetNextNodeTimestamp() method.
        /// <summary>
        /// NodeId, NodeHead data
        /// </summary>
        private Dictionary<int, Dictionary<string, object>> _nodes = new Dictionary<int, Dictionary<string, object>>();

        // ReSharper disable once InconsistentNaming
        private int __lastVersionId = 260; // Uses the GetNextVersionId() method.
        // ReSharper disable once InconsistentNaming
        private long __lastVersionTimestamp;   // Uses the GetNextVersionTimestamp() method.
        /// <summary>
        /// VersionId, NodeData minus NodeHead
        /// </summary>
        private Dictionary<int, Dictionary<string, object>> _versions = new Dictionary<int, Dictionary<string, object>>();


        /* ============================================================================================================= Nodes */

        public override async Task<SaveResult> InsertNodeAsync(NodeData nodeData)
        {
            //UNDONE:DB Lock? Transaction?

            var saveResult = new SaveResult();

            var nodeId = GetNextNodeId();
            saveResult.NodeId = nodeId;
            var nodeTimestamp = GetNextNodeTimestamp();
            saveResult.NodeTimestamp = nodeTimestamp;
            var nodeHeadData = GetNodeHeadData(nodeData);
            nodeHeadData["NodeId"] = nodeId;
            nodeHeadData["Timestamp"] = nodeTimestamp;

            var versionId = GetNextVersionId();
            saveResult.VersionId = versionId;
            var versionTimestamp = GetNextVersionTimestamp();
            saveResult.VersionTimestamp = versionTimestamp;
            var versionData = GetVersionData(nodeData);
            versionData["VersionId"] = versionId;
            versionData["NodeId"] = nodeId;
            versionData["Timestamp"] = versionTimestamp;

            _versions[versionId] = versionData;

            //UNDONE:DB BinaryIds?

            LoadLastVersionIds(nodeId, out var lastMajorVersionId, out var lastMinorVersionId);
            nodeHeadData["LastMajorVersionId"] = lastMajorVersionId;
            nodeHeadData["LastMinorVersionId"] = lastMinorVersionId;

            saveResult.LastMajorVersionId = lastMajorVersionId;
            saveResult.LastMinorVersionId = lastMinorVersionId;

            _nodes[nodeId] = nodeHeadData;

            return await System.Threading.Tasks.Task.FromResult(saveResult);
        }

        public override Task<SaveResult> UpdateNodeAsync(NodeData nodeData, IEnumerable<int> versionIdsToDelete)
        {
            //UNDONE:DB Lock? Transaction?

            // Executes these:
            // INodeWriter: UpdateNodeRow(nodeData);
            // INodeWriter: UpdateVersionRow(nodeData, out lastMajorVersionId, out lastMinorVersionId);
            // DataProvider: private static void SaveNodeProperties(NodeData nodeData, SavingAlgorithm savingAlgorithm, INodeWriter writer, bool isNewNode)
            // DataProvider: protected internal abstract void DeleteVersion(int versionId, NodeData nodeData, out int lastMajorVersionId, out int lastMinorVersionId);

            if(!_nodes.TryGetValue(nodeData.Id, out var nodeHeadData))
                throw new Exception($"Cannot update a deleted Node. Id: {nodeData.Id}, path: {nodeData.Path}.");
            if ((long)nodeHeadData["Timestamp"] != nodeData.NodeTimestamp)
                throw new Exception($"Node is out of date Id: {nodeData.Id}, path: {nodeData.Path}.");

            var versionData = _versions[nodeData.VersionId];

            // UpdateVersionRow
            versionData["NodeId"] = nodeData.Id;
            versionData["MajorNumber"] = nodeData.Version.Major;
            versionData["MinorNumber"] = nodeData.Version.Minor;
            versionData["Status"] = nodeData.Version.Status;
            versionData["@CreationDate"] = nodeData.VersionCreationDate;
            versionData["@CreatedById"] = nodeData.VersionCreatedById;
            versionData["@ModificationDate"] = nodeData.VersionModificationDate;
            versionData["@ModifiedById"] = nodeData.VersionModifiedById;
            versionData["@ChangedData"] = JsonConvert.SerializeObject(nodeData.ChangedData);
            var versionTimestamp = GetNextVersionTimestamp();
            versionData["Timestamp"] = versionTimestamp;

            // UpdateNodeRow
            var path = nodeData.Id == Identifiers.PortalRootId
                ? Identifiers.RootPath
                : GetParentPath(nodeData.Id) + "/" + nodeData.Name; //UNDONE:DB Generalize "parent path + '/' + name" algorithm instead of calculating in the provider.
            nodeHeadData["NodeTypeId"] = nodeData.NodeTypeId;
            nodeHeadData["ContentListTypeId"] = nodeData.ContentListTypeId;
            nodeHeadData["ContentListId"] = nodeData.ContentListId;
            nodeHeadData["CreatingInProgress"] = nodeData.CreatingInProgress;
            nodeHeadData["IsDeleted"] = nodeData.IsDeleted;
            //nodeHeadData["IsInherited"] = nodeData.IsInherited;
            nodeHeadData["ParentNodeId"] = nodeData.ParentId;
            nodeHeadData["Name"] = nodeData.Name;
            nodeHeadData["DisplayName"] = nodeData.DisplayName;
            nodeHeadData["Path"] = path;
            nodeHeadData["Index"] = nodeData.Index;
            nodeHeadData["Locked"] = nodeData.Locked;
            nodeHeadData["LockedById"] = nodeData.LockedById;
            nodeHeadData["ETag"] = nodeData.ETag;
            nodeHeadData["LockType"] = nodeData.LockType;
            nodeHeadData["LockTimeout"] = nodeData.LockTimeout;
            nodeHeadData["LockDate"] = nodeData.LockDate;
            nodeHeadData["LockToken"] = nodeData.LockToken;
            nodeHeadData["LastLockUpdate"] = nodeData.LastLockUpdate;
            nodeHeadData["CreationDate"] = nodeData.CreationDate;
            nodeHeadData["CreatedById"] = nodeData.CreatedById;
            nodeHeadData["ModificationDate"] = nodeData.ModificationDate;
            nodeHeadData["ModifiedById"] = nodeData.ModifiedById;
            nodeHeadData["IsSystem"] = nodeData.IsSystem;
            nodeHeadData["OwnerId"] = nodeData.OwnerId;
            nodeHeadData["SavingState"] = nodeData.SavingState;
            var nodeTimestamp = GetNextNodeTimestamp();
            nodeHeadData["Timestamp"] = nodeTimestamp;
            LoadLastVersionIds(nodeData.Id, out var lastMajorVersionId, out var lastMinorVersionId);
            nodeHeadData["LastMajorVersionId"] = lastMajorVersionId;
            nodeHeadData["LastMinorVersionId"] = lastMinorVersionId;

            //UNDONE:DB BinaryIds?

            var result = new SaveResult
            {
                NodeTimestamp = nodeTimestamp,
                VersionTimestamp = versionTimestamp,
                LastMajorVersionId = lastMajorVersionId,
                LastMinorVersionId = lastMinorVersionId
            };
            return System.Threading.Tasks.Task.FromResult(result);
        }

        public override Task<SaveResult> CopyAndUpdateNodeAsync(NodeData nodeData, int settingsCurrentVersionId, IEnumerable<int> versionIdsToDelete)
        {
            throw new NotImplementedException();
        }

        public override Task<SaveResult> CopyAndUpdateNodeAsync(NodeData nodeData, int currentVersionId, int expectedVersionId,
            IEnumerable<int> versionIdsToDelete)
        {
            throw new NotImplementedException();
        }

        public override System.Threading.Tasks.Task UpdateNodeHeadAsync(NodeData nodeData)
        {
            throw new NotImplementedException();
        }

        public override System.Threading.Tasks.Task UpdateSubTreePathAsync(string oldPath, string newPath)
        {
            throw new NotImplementedException();
        }

        public override Task<IEnumerable<NodeData>> LoadNodesAsync(int[] versionIdArray)
        {
            return System.Threading.Tasks.Task.Run(() =>
            {
                //UNDONE:DB Implement LoadNodesAsync well.
                return (IEnumerable<NodeData>)new List<NodeData>();
            });
        }

        public override System.Threading.Tasks.Task DeleteNodeAsync(int nodeId, long timestamp)
        {
            throw new NotImplementedException();
        }

        public override System.Threading.Tasks.Task MoveNodeAsync(int sourceNodeId, int targetNodeId, long sourceTimestamp, long targetTimestamp)
        {
            throw new NotImplementedException();
        }

        /* ============================================================================================================= NodeHead */

        public override Task<NodeHead> LoadNodeHeadAsync(string path)
        {
            throw new NotImplementedException();
        }

        public override Task<NodeHead> LoadNodeHeadAsync(int nodeId)
        {
            throw new NotImplementedException();
        }

        public override Task<NodeHead> LoadNodeHeadByVersionIdAsync(int versionId)
        {
            throw new NotImplementedException();
        }

        public override Task<IEnumerable<NodeHead>> LoadNodeHeadsAsync(IEnumerable<int> heads)
        {
            throw new NotImplementedException();
        }

        /* ============================================================================================================= Schema */

        public override Task<DataSet> LoadSchemaAsync()
        {
            throw new NotImplementedException();
        }

        public override SchemaWriter CreateSchemaWriter()
        {
            throw new NotImplementedException();
        }

        public override string StartSchemaUpdate_EXPERIMENTAL(long schemaTimestamp)
        {
            throw new NotImplementedException();
        }

        public override long FinishSchemaUpdate_EXPERIMENTAL(string schemaLock)
        {
            throw new NotImplementedException();
        }

        /* ============================================================================================================= Tools */

        public override DateTime RoundDateTime(DateTime d)
        {
            throw new NotImplementedException();
        }

        /* ============================================================================================================= Infrastructure */

        private int GetNextNodeId()
        {
            return Interlocked.Increment(ref __lastNodeId);
        }
        private int GetNextVersionId()
        {
            return Interlocked.Increment(ref __lastVersionId);
        }
        private long GetNextNodeTimestamp()
        {
            return Interlocked.Increment(ref __lastNodeTimestamp);
        }
        private long GetNextVersionTimestamp()
        {
            return Interlocked.Increment(ref __lastVersionTimestamp);
        }

        private Dictionary<string, object> GetNodeHeadData(NodeData nodeData)
        {
            return new Dictionary<string, object>
            {
                {"NodeId", nodeData.Id},
                {"NodeTypeId", nodeData.NodeTypeId},
                {"ContentListTypeId", nodeData.ContentListTypeId},
                {"ContentListId", nodeData.ContentListId},
                {"CreatingInProgress", nodeData.CreatingInProgress},
                {"IsDeleted", nodeData.IsDeleted},
                //{"IsInherited", nodeData.???},
                {"ParentNodeId", nodeData.ParentId},
                {"Name", nodeData.Name},
                {"Path", nodeData.Path},
                {"Index", nodeData.Index},
                {"Locked", nodeData.Locked},
                {"LockedById", nodeData.LockedById},
                {"ETag", nodeData.ETag},
                {"LockType", nodeData.LockType},
                {"LockTimeout", nodeData.LockTimeout},
                {"LockDate", nodeData.LockDate},
                {"LockToken", nodeData.LockToken},
                {"LastLockUpdate", nodeData.LastLockUpdate},
                //{"LastMinorVersionId", nodeData},
                //{"LastMajorVersionId", nodeData},
                {"CreationDate", nodeData.CreationDate},
                {"CreatedById", nodeData.CreatedById},
                {"ModificationDate", nodeData.ModificationDate},
                {"ModifiedById", nodeData.ModifiedById},
                {"DisplayName", nodeData.DisplayName},
                {"IsSystem", nodeData.IsSystem},
                {"OwnerId", nodeData.OwnerId},
                {"SavingState", nodeData.SavingState},
                //{"Timestamp", nodeData},
            };
        }
        private Dictionary<string, object> GetVersionData(NodeData nodeData)
        {
            return new Dictionary<string, object>
            {
                {"VersionId", nodeData.VersionId},
                {"NodeId", nodeData.Id},
                {"MajorNumber", nodeData.Version.Major},
                {"MinorNumber", nodeData.Version.Minor},
                {"CreationDate", nodeData.VersionCreationDate},
                {"CreatedById", nodeData.VersionCreatedById},
                {"ModificationDate", nodeData.VersionModificationDate},
                {"ModifiedById", nodeData.VersionModifiedById},
                {"Status", nodeData.Version.Status},
                //{"IndexDocument", ____},
                {"ChangedData", nodeData.ChangedData},
                //{"Timestamp", ____},
            };
        }

        private void LoadLastVersionIds(int nodeId, out int lastMajorVersionId, out int lastMinorVersionId)
        {
            var allVersions = _versions.Values
                    .Where(v => (int) v["NodeId"] == nodeId)
                    .OrderBy(v => (int)v["MajorNumber"])
                    .ThenBy(v => (int)v["MinorNumber"])
                    .ThenBy(v => (int)v["Status"])
                    .ToArray();
            var lastMinorVersion = allVersions.LastOrDefault();
            lastMinorVersionId = lastMinorVersion == null ? 0 : (int)lastMinorVersion["VersionId"];

            var majorVersions = allVersions
                .Where(v => (int) v["MinorNumber"] == 0 && (int) v["Status"] == (int) VersionStatus.Approved)
                .ToArray();

            var lastMajorVersion = majorVersions.LastOrDefault();
            lastMajorVersionId = lastMajorVersion == null ? 0 : (int)lastMajorVersion["VersionId"];
        }

        private string GetParentPath(int nodeId)
        {
            return (string)_nodes[nodeId]["Path"];
        }

        //UNDONE:DB -------Delete GetNodeTimestamp feature
        public override long GetNodeTimestamp(int nodeId)
        {
            if (!_nodes.TryGetValue(nodeId, out var existing))
                return 0L;
            return (long)existing["Timestamp"];
        }
        //UNDONE:DB -------Delete GetVersionTimestamp feature
        public override long GetVersionTimestamp(int versionId)
        {
            if (!_versions.TryGetValue(versionId, out var existing))
                return 0L;
            return (long)existing["Timestamp"];
        }

    }
}
