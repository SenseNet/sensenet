﻿using System;
using System.Collections.Generic;
using System.Text;
using SenseNet.ContentRepository.Storage.Schema;

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

    public class VersionData
    {
        public int VersionId;
        public int NodeId;
        public VersionNumber Version;
        public DateTime CreationDate;
        public int CreatedById;
        public DateTime ModificationDate;
        public int ModifiedById;
        public IEnumerable<ChangedData> ChangedData;
        public long Timestamp;
    }

    public class DynamicData
    {
        public int VersionId;
        public PropertyType[] PropertyTypes;
        public IDictionary<PropertyType, object> DynamicProperties;
        public IDictionary<PropertyType, BinaryDataValue> BinaryProperties;
        public IDictionary<PropertyType, string> VeryLongTextValues; //UNDONE:DB!!! Discuss: requires or not
    }
}
