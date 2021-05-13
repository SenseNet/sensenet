﻿using System;
using SenseNet.ContentRepository.Storage.Schema;
using System.Diagnostics;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.DataModel;

namespace SenseNet.ContentRepository.Storage
{
    [System.Diagnostics.DebuggerDisplay("{Path} [Id={Id}, ParentId={ParentId}, NodeTypeId={NodeTypeId}]")]
    [Serializable]
    public class NodeHead
    {
        private static IDataStore DataStore => Providers.Instance.DataStore;

        [DebuggerDisplay("{VersionId}, {VersionNumber}")]
        public class NodeVersion
        {
            public VersionNumber VersionNumber { get; private set; }
            public int VersionId { get; private set; }
            public NodeVersion(VersionNumber versionNumber, int versionId)
            {
                if (versionNumber == null)
                    throw new ArgumentNullException("versionNumber");
                if (versionId == 0)
                    throw new ArgumentException("VersionId cannot be 0.");

                VersionNumber = versionNumber;
                VersionId = versionId;
            }
        }

        private NodeVersion[] _versions;
        public NodeVersion[] Versions
        {
            get
            {
                if (_versions == null)
                {
                    _versions = DataStore.GetVersionNumbersAsync(this.Id, CancellationToken.None).GetAwaiter().GetResult()
                        .ToArray();
                    //TODO: After GetNodeVersions: check changes
                }
                return _versions;
            }
        }

        public int Id { get; private set; }
        public string Path { get; private set; }
        public string Name { get; private set; }
        public string DisplayName { get; private set; }
        public int ParentId { get; private set; }
        public int NodeTypeId { get; private set; }
        public int ContentListTypeId { get; private set; }
        public int ContentListId { get; private set; }
        public DateTime CreationDate { get; private set; }
        public DateTime ModificationDate { get; private set; }
        public DateTime LoadDate { get; private set; }
        public int LastMinorVersionId { get; private set; }
        public int LastMajorVersionId { get; private set; }
        public int OwnerId { get; private set; }
        public int CreatorId { get; private set; }
        public int LastModifierId { get; private set; }
        public int Index { get; private set; }
        public int LockerId { get; private set; }
        public long Timestamp { get; internal set; }

        internal NodeHead() { }

        public NodeHead(int nodeId, string name, string displayName, string path, int parentNodeId,
            int nodeTypeId, int contentListTypeId, int contentListId,
            DateTime creationDate, DateTime modificationDate,
            int lastMinorVersionId, int lastMajorVersionId,
            int ownerId, int creatorId, int lastModifierId, int index, int lockerId, long timestamp)
        {
            Id = nodeId;
            Name = name;
            DisplayName = displayName;
            Path = path;
            ParentId = parentNodeId;
            NodeTypeId = nodeTypeId;
            ContentListTypeId = contentListTypeId;
            ContentListId = contentListId;
            CreationDate = creationDate;
            ModificationDate = modificationDate;
            LastMinorVersionId = lastMinorVersionId;
            LastMajorVersionId = lastMajorVersionId;
            OwnerId = ownerId;
            CreatorId = creatorId;
            LastModifierId = lastModifierId;
            Index = index;
            LockerId = lockerId;
            Timestamp = timestamp;

            LoadDate = DateTime.UtcNow;
        }
        internal static NodeHead CreateFromNode(NodeData node, int lastMinorVersionId, int lastMajorVersionId)
        {
            return new NodeHead
            {
                Id = node.Id,
                Name = node.Name,
                DisplayName = node.DisplayName,
                Path = node.Path,
                ParentId = node.ParentId,
                NodeTypeId = node.NodeTypeId,
                ContentListTypeId = node.ContentListTypeId,
                ContentListId = node.ContentListId,
                CreationDate = node.CreationDate,
                ModificationDate = node.ModificationDate,
                LastMinorVersionId = lastMinorVersionId,
                LastMajorVersionId = lastMajorVersionId,
                OwnerId = node.OwnerId,
                CreatorId = node.CreatedById,
                Index = node.Index,
                LastModifierId = node.ModifiedById,
                LockerId = node.LockedById,
                Timestamp = node.NodeTimestamp,
                LoadDate = DateTime.UtcNow,
            };
        }

        /// <summary>
        /// Gets the VersionId that belongs to the given version. If no such version exists, 0 is returned.
        /// </summary>
		internal int GetVersionId(VersionNumber version)
		{
            foreach (var v in Versions)
            {
                if (v.VersionNumber == version)
                    return v.VersionId;
            }
            return 0;
		}

        public static NodeHead Get(int nodeId)
        {
            return DataStore.LoadNodeHeadAsync(nodeId, CancellationToken.None).GetAwaiter().GetResult();
        }
        public static Task<NodeHead> GetAsync(int nodeId, CancellationToken cancellationToken)
        {
            return DataStore.LoadNodeHeadAsync(nodeId, cancellationToken);
        }
        public static NodeHead GetByVersionId(int versionId)
        {
            return DataStore.LoadNodeHeadByVersionIdAsync(versionId, CancellationToken.None).GetAwaiter().GetResult();
        }
        public static Task<NodeHead> GetByVersionIdAsync(int versionId, CancellationToken cancellationToken)
        {
            return DataStore.LoadNodeHeadByVersionIdAsync(versionId, cancellationToken);
        }
        public static NodeHead Get(string path)
        {
            return DataStore.LoadNodeHeadAsync(path, CancellationToken.None).GetAwaiter().GetResult();
        }
        public static Task<NodeHead> GetAsync(string path, CancellationToken cancellationToken)
        {
            return DataStore.LoadNodeHeadAsync(path, cancellationToken);
        }
        public static IEnumerable<NodeHead> Get(IEnumerable<int> idArray)
        {
            return DataStore.LoadNodeHeadsAsync(idArray, CancellationToken.None).GetAwaiter().GetResult();
        }
        public static Task<IEnumerable<NodeHead>> GetAsync(IEnumerable<int> idArray, CancellationToken cancellationToken)
        {
            return DataStore.LoadNodeHeadsAsync(idArray, cancellationToken);
        }
        public static IEnumerable<NodeHead> Get(IEnumerable<string> pathSet)
        {
            foreach (var head in pathSet.Select(p => NodeHead.Get(p)))
                yield return head;
        }

        public NodeType GetNodeType()
        {
            return ActiveSchema.NodeTypes.GetItemById(NodeTypeId);
        }

        public NodeVersion GetLastMajorVersion()
        {
            return Versions.Where(v => v.VersionId == LastMajorVersionId).FirstOrDefault();
        }
        public NodeVersion GetLastMinorVersion()
        {
            return Versions.Where(v => v.VersionId == LastMinorVersionId).FirstOrDefault();
        }

        internal NodeHeadData GetNodeHeadData()
        {
            return new NodeHeadData
            {
                NodeId = Id,
                NodeTypeId = NodeTypeId,
                ContentListTypeId = ContentListTypeId,
                ContentListId = ContentListId,
                ParentNodeId = ParentId,
                Name = Name,
                DisplayName = DisplayName,
                Path = Path,
                Index = Index,
                Locked = LockerId != 0,
                LockedById = LockerId,
                LastMinorVersionId = LastMinorVersionId,
                LastMajorVersionId = LastMajorVersionId,
                CreationDate = CreationDate,
                CreatedById = CreatorId,
                ModificationDate = ModificationDate,
                ModifiedById = LastModifierId,
                OwnerId = OwnerId,
                Timestamp = Timestamp,
            };
        }

    }
}
