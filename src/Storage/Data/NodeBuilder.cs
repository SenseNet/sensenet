using System;
using System.Collections.Generic;
using SenseNet.ContentRepository.Storage.Schema;

namespace SenseNet.ContentRepository.Storage.Data
{
    public class NodeBuilder
    {
        private bool _loaded;
        private NodeData _data;
        private NodeToken _token;

        public NodeToken Token
        {
            get { return _token; }
        }

        public NodeBuilder(NodeToken token)
        {
            _token = token;
            _token.NodeData = null;
            _data = new NodeData(_token.NodeTypeId, _token.ContentListTypeId)
            {
                IsShared = false,
                SharedData = null,
            };
        }

        public void AddDynamicProperty(int propertyTypeId, object value)
        {
            _data.SetDynamicRawData(propertyTypeId, value);
        }
        public void AddDynamicProperty(PropertyType propertyType, object value)
        {
            _data.SetDynamicRawData(propertyType, value);
        }

        public void SetCoreAttributes(int nodeId, int nodeTypeId, int contentListId, int contentListTypeId,
            bool creatingInProgress, bool isDeleted, int parentId, string name, string displayName, string path, int index,
            bool locked, int lockedById, string etag, int lockType, int lockTimeout, DateTime lockDate, string lockToken, DateTime lastLockUpdate,
            int versionId, VersionNumber version, DateTime versionCreationDate, int versionCreatedById, DateTime versionModificationDate, int versionModifiedById,
            bool isSystem, int ownerId, ContentSavingState savingState, IEnumerable<ChangedData> changedData,
            DateTime nodeCreationDate, int nodeCreatedById, DateTime nodeModificationDate, int nodeModifiedById, long nodeTimestamp, long versionTimestamp)
        {
            _data.Id = nodeId;
            _data.NodeTypeId = nodeTypeId;
            _data.ContentListId = contentListId;
            _data.ContentListTypeId = contentListTypeId;
            _data.CreatingInProgress = creatingInProgress;
            _data.IsDeleted = isDeleted;
            _data.ParentId = parentId;
            _data.Name = name;
            _data.DisplayName = displayName;
            _data.Path = path;
            _data.Index = index;
            _data.Locked = locked;
            _data.LockedById = lockedById;
            _data.ETag = etag;
            _data.LockType = lockType;
            _data.LockTimeout = lockTimeout;
            _data.LockDate = lockDate;
            _data.LockToken = lockToken;
            _data.LastLockUpdate = lastLockUpdate;
            _data.VersionId = versionId;
            _data.Version = version;
            _data.CreationDate = nodeCreationDate;
            _data.CreatedById = nodeCreatedById;
            _data.ModificationDate = nodeModificationDate;
            _data.ModifiedById = nodeModifiedById;
            _data.IsSystem = isSystem;
            _data.OwnerId = ownerId;
            _data.SavingState = savingState;
            _data.ChangedData = changedData;
            _data.VersionCreationDate = versionCreationDate;
            _data.VersionCreatedById = versionCreatedById;
            _data.VersionModificationDate = versionModificationDate;
            _data.VersionModifiedById = versionModifiedById;
            _data.NodeTimestamp = nodeTimestamp;
            _data.VersionTimestamp = versionTimestamp;

            _loaded = true;
        }

        public void Finish()
        {
            if(_loaded)
                _token.NodeData = _data;
        }
    }
}