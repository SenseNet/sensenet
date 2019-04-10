using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using SenseNet.ContentRepository.Storage;

namespace SenseNet.ContentRepository.Tests.Implementations
{
    internal class NodeDoc
    {
        private int _nodeId;
        private int _nodeTypeId;
        private int _contentListTypeId;
        private int _contentListId;
        private bool _creatingInProgress;
        private bool _isDeleted;
        private int _parentNodeId;
        private string _name;
        private string _displayName;
        private string _path;
        private int _index;
        private bool _locked;
        private int _lockedById;
        private string _eTag;
        private int _lockType;
        private int _lockTimeout;
        private DateTime _lockDate;
        private string _lockToken;
        private DateTime _lastLockUpdate;
        private int _lastMinorVersionId;
        private int _lastMajorVersionId;
        private DateTime _creationDate;
        private int _createdById;
        private DateTime _modificationDate;
        private int _modifiedById;
        private bool _isSystem;
        private int _ownerId;
        private ContentSavingState _savingState;
        private long _timestamp;

        // ReSharper disable once ConvertToAutoProperty
        public int NodeId
        {
            get => _nodeId;
            set => _nodeId = value;
        }

        public int NodeTypeId
        {
            get => _nodeTypeId;
            set
            {
                _nodeTypeId = value;
                SetTimestamp();
            }
        }

        public int ContentListTypeId
        {
            get => _contentListTypeId;
            set
            {
                _contentListTypeId = value;
                SetTimestamp();
            }
        }

        public int ContentListId
        {
            get => _contentListId;
            set
            {
                _contentListId = value;
                SetTimestamp();
            }
        }

        public bool CreatingInProgress
        {
            get => _creatingInProgress;
            set
            {
                _creatingInProgress = value;
                SetTimestamp();
            }
        }

        public bool IsDeleted
        {
            get => _isDeleted;
            set
            {
                _isDeleted = value;
                SetTimestamp();
            }
        }

        public int ParentNodeId
        {
            get => _parentNodeId;
            set
            {
                _parentNodeId = value;
                SetTimestamp();
            }
        }

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                SetTimestamp();
            }
        }

        public string DisplayName
        {
            get => _displayName;
            set
            {
                _displayName = value;
                SetTimestamp();
            }
        }

        public string Path
        {
            get => _path;
            set
            {
                _path = value;
                SetTimestamp();
            }
        }

        public int Index
        {
            get => _index;
            set
            {
                _index = value;
                SetTimestamp();
            }
        }

        public bool Locked
        {
            get => _locked;
            set
            {
                _locked = value;
                SetTimestamp();
            }
        }

        public int LockedById
        {
            get => _lockedById;
            set
            {
                _lockedById = value;
                SetTimestamp();
            }
        }

        public string ETag
        {
            get => _eTag;
            set
            {
                _eTag = value;
                SetTimestamp();
            }
        }

        public int LockType
        {
            get => _lockType;
            set
            {
                _lockType = value;
                SetTimestamp();
            }
        }

        public int LockTimeout
        {
            get => _lockTimeout;
            set
            {
                _lockTimeout = value;
                SetTimestamp();
            }
        }

        public DateTime LockDate
        {
            get => _lockDate;
            set
            {
                _lockDate = value;
                SetTimestamp();
            }
        }

        public string LockToken
        {
            get => _lockToken;
            set
            {
                _lockToken = value;
                SetTimestamp();
            }
        }

        public DateTime LastLockUpdate
        {
            get => _lastLockUpdate;
            set
            {
                _lastLockUpdate = value;
                SetTimestamp();
            }
        }

        public int LastMinorVersionId
        {
            get => _lastMinorVersionId;
            set
            {
                _lastMinorVersionId = value;
                SetTimestamp();
            }
        }

        public int LastMajorVersionId
        {
            get => _lastMajorVersionId;
            set
            {
                _lastMajorVersionId = value;
                SetTimestamp();
            }
        }

        public DateTime CreationDate
        {
            get => _creationDate;
            set
            {
                _creationDate = value;
                SetTimestamp();
            }
        }

        public int CreatedById
        {
            get => _createdById;
            set
            {
                _createdById = value;
                SetTimestamp();
            }
        }

        public DateTime ModificationDate
        {
            get => _modificationDate;
            set
            {
                _modificationDate = value;
                SetTimestamp();
            }
        }

        public int ModifiedById
        {
            get => _modifiedById;
            set
            {
                _modifiedById = value;
                SetTimestamp();
            }
        }

        public bool IsSystem
        {
            get => _isSystem;
            set
            {
                _isSystem = value;
                SetTimestamp();
            }
        }

        public int OwnerId
        {
            get => _ownerId;
            set
            {
                _ownerId = value;
                SetTimestamp();
            }
        }

        public ContentSavingState SavingState
        {
            get => _savingState;
            set
            {
                _savingState = value;
                SetTimestamp();
            }
        }

        public long Timestamp => _timestamp;

        private static long _lastTimestamp;

        private void SetTimestamp()
        {
            _timestamp = Interlocked.Increment(ref _lastTimestamp);
        }

        public NodeDoc Clone()
        {
            return new NodeDoc
            {
                _nodeId = _nodeId,
                _nodeTypeId = _nodeTypeId,
                _contentListTypeId = _contentListTypeId,
                _contentListId = _contentListId,
                _creatingInProgress = _creatingInProgress,
                _isDeleted = _isDeleted,
                _parentNodeId = _parentNodeId,
                _name = _name,
                _displayName = _displayName,
                _path = _path,
                _index = _index,
                _locked = _locked,
                _lockedById = _lockedById,
                _eTag = _eTag,
                _lockType = _lockType,
                _lockTimeout = _lockTimeout,
                _lockDate = _lockDate,
                _lockToken = _lockToken,
                _lastLockUpdate = _lastLockUpdate,
                _lastMinorVersionId = _lastMinorVersionId,
                _lastMajorVersionId = _lastMajorVersionId,
                _creationDate = _creationDate,
                _createdById = _createdById,
                _modificationDate = _modificationDate,
                _modifiedById = _modifiedById,
                _isSystem = _isSystem,
                _ownerId = _ownerId,
                _savingState = _savingState,
                _timestamp = _timestamp,
            };
        }
    }

    internal class VersionDoc
    {
        private int _versionId;
        private int _nodeId;
        private VersionNumber _version;
        private DateTime _creationDate;
        private int _createdById;
        private DateTime _modificationDate;
        private int _modifiedById;
        private string _indexDocument; //UNDONE:DB --- Do not store IndexDocument in the VersionDoc
        private IEnumerable<ChangedData> _changedData;
        Dictionary<string, object> _dynamicProperties;
        private long _timestamp;

        // ReSharper disable once ConvertToAutoProperty
        public int VersionId
        {
            get => _versionId;
            set => _versionId = value;
        }

        public int NodeId
        {
            get => _nodeId;
            set
            {
                _nodeId = value;
                SetTimestamp();
            }
        }

        /// <summary>
        /// Gets or sets the clone of a VersionNumber
        /// </summary>
        public VersionNumber Version
        {
            get => _version.Clone();
            set
            {
                _version = value.Clone();
                SetTimestamp();
            }
        }

        public DateTime CreationDate
        {
            get => _creationDate;
            set
            {
                _creationDate = value;
                SetTimestamp();
            }
        }

        public int CreatedById
        {
            get => _createdById;
            set
            {
                _createdById = value;
                SetTimestamp();
            }
        }

        public DateTime ModificationDate
        {
            get => _modificationDate;
            set
            {
                _modificationDate = value;
                SetTimestamp();
            }
        }

        public int ModifiedById
        {
            get => _modifiedById;
            set
            {
                _modifiedById = value;
                SetTimestamp();
            }
        }

        public string IndexDocument
        {
            get => _indexDocument;
            set
            {
                _indexDocument = value;
                SetTimestamp();
            }
        }

        public IEnumerable<ChangedData> ChangedData
        {
            get => _changedData;
            set
            {
                _changedData = value;
                SetTimestamp();
            }
        }

        public long Timestamp => _timestamp;

        public Dictionary<string, object> DynamicProperties
        {
            get => _dynamicProperties;
            set
            {
                _dynamicProperties = value;
                SetTimestamp();
            }
        }

        /* =======================================================  */
        private static long _lastTimestamp;
        private void SetTimestamp()
        {
            _timestamp = Interlocked.Increment(ref _lastTimestamp);
        }
        public VersionDoc Clone()
        {
            return new VersionDoc
            {
                _versionId = _versionId,
                _nodeId = _nodeId,
                _version = _version,
                _creationDate = _creationDate,
                _createdById = _createdById,
                _modificationDate = _modificationDate,
                _modifiedById = _modifiedById,
                _indexDocument = _indexDocument,
                _changedData = _changedData?.ToArray(),
                _dynamicProperties = CloneDynamicProperties(_dynamicProperties),
                _timestamp = _timestamp,
            };
        }
        // ReSharper disable once UnusedParameter.Local
        private Dictionary<string, object> CloneDynamicProperties(Dictionary<string, object> dynamicProperties)
        {
            throw new NotImplementedException();
        }
    }

    internal class BinaryPropertyDoc
    {
        public int BinaryPropertyId { get; set; }
        public int VersionId { get; set; }
        public int PropertyTypeId { get; set; }
        public int FileId { get; set; }

        public BinaryPropertyDoc Clone()
        {
            return new BinaryPropertyDoc
            {
                BinaryPropertyId = BinaryPropertyId,
                VersionId = VersionId,
                PropertyTypeId = PropertyTypeId,
                FileId = FileId
            };
        }
    }

    internal class FileDoc
    {
        public int FileId { get; set; }
        public string ContentType { get; set; }
        public string Extension { get; set; }
        public string FileNameWithoutExtension { get; set; }
        public long Size { get; set; }
        public bool Staging { get; set; }
        public string BlobProvider { get; set; }
        public string BlobProviderData { get; set; }
        public byte[] Buffer { get; set; }
        public long Timestamp { get; set; } //UNDONE:DB ---FileDoc.Timestamp always 0L

        public FileDoc Clone()
        {
            return new FileDoc
            {
                FileId = FileId,
                ContentType = ContentType,
                Extension = Extension,
                FileNameWithoutExtension = FileNameWithoutExtension,
                Size = Size,
                Staging = Staging,
                BlobProvider = BlobProvider,
                BlobProviderData = BlobProviderData,
                Buffer = Buffer.ToArray(),
                Timestamp = Timestamp
            };
        }
    }
}
