using System;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.DataModel
{
    public class NodeHeadData : IDataModel
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
        public virtual long Timestamp { get; set; }

        public void SetProperty(string name, string value)
        {
            switch (name)
            {
                case "NodeId":
                    NodeId = int.Parse(value);
                    break;
                case "TypeId":
                case "NodeTypeId":
                    NodeTypeId = int.Parse(value);
                    break;
                case "ContentListTypeId":
                    ContentListTypeId = int.Parse(value);
                    break;
                case "ContentListId":
                    ContentListId = int.Parse(value);
                    break;
                case "CreatingInProgress":
                    CreatingInProgress = value.ToLowerInvariant() == "true";
                    break;
                case "IsDeleted":
                    IsDeleted = value.ToLowerInvariant() == "true";
                    break;
                case "Parent":
                case "ParentId":
                case "ParentNodeId":
                    ParentNodeId = int.Parse(value);
                    break;
                case "Name":
                    Name = value;
                    break;
                case "DisplayName":
                    DisplayName = value;
                    break;
                case "Path":
                    Path = value;
                    break;
                case "Index":
                    Index = int.Parse(value);
                    break;
                case "Locked":
                    Locked = value.ToLowerInvariant() == "true";
                    break;
                case "LockedById":
                    LockedById = int.Parse(value);
                    break;
                case "ETag":
                    ETag = value;
                    break;
                case "LockType":
                    LockType = int.Parse(value);
                    break;
                case "LockTimeout":
                    LockTimeout = int.Parse(value);
                    break;
                case "LockDate":
                    LockDate = DateTime.Parse(value);
                    break;
                case "LockToken":
                    LockToken = value;
                    break;
                case "LastLockUpdate":
                    LastLockUpdate = DateTime.Parse(value);
                    break;
                case "MinorV":
                case "LastMinorVersionId":
                    LastMinorVersionId = int.Parse(value);
                    break;
                case "MajorV":
                case "LastMajorVersionId":
                    LastMajorVersionId = int.Parse(value);
                    break;
                case "CreationDate":
                    CreationDate = DateTime.Parse(value);
                    break;
                case "Creator":
                case "CreatedById":
                    CreatedById = int.Parse(value);
                    break;
                case "ModificationDate":
                    ModificationDate = DateTime.Parse(value);
                    break;
                case "Modifier":
                case "ModifiedById":
                    ModifiedById = int.Parse(value);
                    break;
                case "IsSystem":
                    IsSystem = value.ToLowerInvariant() == "true";
                    break;
                case "Owner":
                case "OwnerId":
                    OwnerId = int.Parse(value);
                    break;
                case "SavingState":
                    SavingState = (ContentSavingState)Enum.Parse(typeof(ContentSavingState), value, true);
                    break;
                case "Timestamp":
                    Timestamp = long.Parse(value);
                    break;
                default:
                    throw new ApplicationException("Unknown property: " + name);
            }
        }
    }
}
