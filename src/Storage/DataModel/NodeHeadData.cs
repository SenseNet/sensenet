using System;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.DataModel
{
    public class NodeHeadData
    {
        public int NodeId;
        public int NodeTypeId;
        public int ContentListTypeId;
        public int ContentListId;
        public bool CreatingInProgress;
        public bool IsDeleted;
        public int ParentNodeId;
        public string Name;
        public string DisplayName;
        public string Path;
        public int Index;
        public bool Locked;
        public int LockedById;
        public string ETag;
        public int LockType;
        public int LockTimeout;
        public DateTime LockDate;
        public string LockToken;
        public DateTime LastLockUpdate;
        public int LastMinorVersionId;
        public int LastMajorVersionId;
        public DateTime CreationDate;
        public int CreatedById;
        public DateTime ModificationDate;
        public int ModifiedById;
        public bool IsSystem;
        public int OwnerId;
        public ContentSavingState SavingState;
        public long Timestamp;
    }
}
