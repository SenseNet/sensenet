using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.ContentRepository.Storage;

namespace SenseNet.ContentRepository.InMemory
{
    public interface IDataDocument
    {
        int Id { get; }
    }
    public class NodeDoc : IDataDocument
    {
        public int Id => NodeId;

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

    public class VersionDoc : IDataDocument
    {
        public int Id => VersionId;

        private int _versionId;
        private int _nodeId;
        private VersionNumber _version;
        private DateTime _creationDate;
        private int _createdById;
        private DateTime _modificationDate;
        private int _modifiedById;
        private string _indexDocument; //TODO: Do not store IndexDocument in the VersionDoc
        private string _changedData;
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
            get
            {
                var result = (IEnumerable<ChangedData>)JsonConvert.DeserializeObject(_changedData, typeof(IEnumerable<ChangedData>));
                return result;
            }
            set
            {
                _changedData = JsonConvert.SerializeObject(value);
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
                _changedData = _changedData,
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

    [DebuggerDisplay("{VersionId}/{PropertyTypeId}: {Value}")]
    public class LongTextPropertyDoc : IDataDocument
    {
        public int Id => LongTextPropertyId;

        public int LongTextPropertyId { get; set; }
        public int VersionId { get; set; }
        public int PropertyTypeId { get; set; }
        public int Length { get; set; }
        public string Value { get; set; }

        public LongTextPropertyDoc Clone()
        {
            return new LongTextPropertyDoc
            {
                LongTextPropertyId = LongTextPropertyId,
                VersionId = VersionId,
                PropertyTypeId = PropertyTypeId,
                Length = Length,
                Value = Value
            };
        }
    }

    public class BinaryPropertyDoc : IDataDocument
    {
        public int Id => BinaryPropertyId;

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

    public class FileDoc : IDataDocument
    {
        public int Id => FileId;

        public int FileId { get; set; }
        public string ContentType { get; set; }
        public string Extension { get; set; }
        public string FileNameWithoutExtension { get; set; }
        public long Size { get; set; }
        public bool Staging { get; set; }
        public string BlobProvider { get; set; }
        public string BlobProviderData { get; set; }
        public byte[] Buffer { get; set; }
        public long Timestamp { get; set; }

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

    public class TreeLockDoc : IDataDocument
    {
        public int Id => TreeLockId;

        public int TreeLockId;
        public string Path;
        public DateTime LockedAt;

        public TreeLockDoc Clone()
        {
            return new TreeLockDoc
            {
                TreeLockId = TreeLockId,
                Path = Path,
                LockedAt = LockedAt
            };
        }
    }

    public class LogEntryDoc : IDataDocument
    {
        public int Id => LogId;

        public int LogId;
        public int EventId;
        public string Category;
        public int Priority;
        public string Severity;

        public string Title;
        public int ContentId;
        public string ContentPath;
        public string UserName;
        public DateTime LogDate;
        public string MachineName;
        public string AppDomainName;
        public int ProcessId;
        public string ProcessName;
        public string ThreadName;
        public int Win32ThreadId;
        public string Message;
        public string FormattedMessage;

        public LogEntryDoc Clone()
        {
            return new LogEntryDoc
            {
                LogId = LogId,
                EventId = EventId,
                Category = Category,
                Priority = Priority,
                Severity = Severity,
                Title = Title,
                ContentId = ContentId,
                ContentPath = ContentPath,
                UserName = UserName,
                LogDate = LogDate,
                MachineName = MachineName,
                AppDomainName = AppDomainName,
                ProcessId = ProcessId,
                ProcessName = ProcessName,
                ThreadName = ThreadName,
                Win32ThreadId = Win32ThreadId,
                Message = Message,
                FormattedMessage = FormattedMessage,
            };
        }
    }

    public class IndexingActivityDoc : IDataDocument
    {
        public int Id => IndexingActivityId;

        public int IndexingActivityId;
        public IndexingActivityType ActivityType;
        public DateTime CreationDate;
        public IndexingActivityRunningState RunningState;
        public DateTime? LockTime;
        public int NodeId;
        public int VersionId;
        public string Path;
        public string Extension;

        public IndexingActivityDoc Clone()
        {
            return new IndexingActivityDoc
            {
                IndexingActivityId = IndexingActivityId,
                ActivityType = ActivityType,
                CreationDate = CreationDate,
                RunningState = RunningState,
                LockTime = LockTime,
                NodeId = NodeId,
                VersionId = VersionId,
                Path = Path,
                Extension = Extension,
            };
        }
    }

    //TODO:  Remove from well known collections. Define in the extension and install it in the  boot sequence
    public class SharedLockDoc : IDataDocument
    {
        public int Id => SharedLockId;
        
        public int SharedLockId;
        public int ContentId;
        public string Lock;
        public DateTime CreationDate;

        public SharedLockDoc Clone()
        {
            return new SharedLockDoc
            {
                SharedLockId = SharedLockId,
                ContentId = ContentId,
                Lock = Lock,
                CreationDate = CreationDate
            };
        }
    }

    //TODO:  Remove from well known collections. Define in the extension and install it in the  boot sequence
    public class AccessTokenDoc : IDataDocument
    {
        public int Id => AccessTokenRowId;

        public int AccessTokenRowId;
        public string Value;
        public int UserId;
        public int? ContentId;
        public string Feature;
        public DateTime CreationDate;
        public DateTime ExpirationDate;

        public AccessTokenDoc Clone()
        {
            return new AccessTokenDoc
            {
                AccessTokenRowId = AccessTokenRowId,
                Value = Value,
                UserId = UserId,
                ContentId = ContentId,
                Feature = Feature,
                CreationDate = CreationDate,
                ExpirationDate = ExpirationDate
            };
        }
    }

    //TODO:  Remove from well known collections. Define in the extension and install it in the  boot sequence
    public class PackageDoc : IDataDocument
    {
        public int Id { get; set; }
        public string Description { get; set; }
        public string ComponentId { get; set; }
        public PackageType PackageType { get; set; }
        public DateTime ReleaseDate { get; set; }
        public DateTime ExecutionDate { get; set; }
        public ExecutionResult ExecutionResult { get; set; }
        public Version ComponentVersion { get; set; }
        public Exception ExecutionError { get; set; }
        public string Manifest { get; set; }

        public PackageDoc Clone()
        {
            return new PackageDoc
            {
                Id = Id,
                Description = Description,
                ComponentId = ComponentId,
                PackageType = PackageType,
                ReleaseDate = ReleaseDate,
                ExecutionDate = ExecutionDate,
                ExecutionResult = ExecutionResult,
                ComponentVersion = ComponentVersion,
                ExecutionError = ExecutionError,
                Manifest = Manifest,
            };
        }
    }
}
