using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository.Storage.Schema;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.DataModel;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;

namespace SenseNet.ContentRepository.Storage
{
    internal interface IDynamicDataAccessor
    {
        Node OwnerNode { get; set; }
        PropertyType PropertyType { get; set; }
        object RawData { get; set; }
        object GetDefaultRawData();
    }
    [DebuggerDisplay("{Name}: {Original} -> {Value}")]
    public class ChangedData
    {
        public static readonly IEnumerable<ChangedData> EmptyArray = new ChangedData[0];
        public string Name { get; set; }
        public object Value { get; set; }
        public object Original { get; set; }
    }

    public class NodeData
    {
        private Stopwatch _savingTimer;

        internal Stopwatch SavingTimer
        {
            get { return _savingTimer ?? (_savingTimer = Stopwatch.StartNew()); }
        }

        private class SnapshotData
        {
            public int Id;
            public string Path;
            public long NodeTimestamp;
            public int VersionId;
            public VersionNumber Version;
            public long VersionTimestamp;
            public Dictionary<int, Tuple<int, int>> BinaryIds; // propType, binPropId, fileId
        }
        internal enum StaticDataSlot
        {
            Id, NodeTypeId, ContentListId, ContentListTypeId,
            ParentId, Name, DisplayName, Path, Index, CreatingInProgress, IsDeleted,
            CreationDate, ModificationDate, CreatedById, ModifiedById,
            VersionId, Version, VersionCreationDate, VersionModificationDate, VersionCreatedById, VersionModifiedById,
            Locked, LockedById, ETag, LockType, LockTimeout, LockDate, LockToken, LastLockUpdate,
            IsSystem, OwnerId,
            SavingState, ChangedData,
            NodeTimestamp, VersionTimestamp
        }
        private static readonly object LockObject = new Object();
        private object _readPropertySync = new object();

        // =========================================================== Data content

        private bool _isShared;
        private NodeData _sharedData;
        private object[] staticData;
        private bool[] staticDataIsModified;
        private Dictionary<int, object> dynamicData;

        // ----------------------------------------------------------- Storage models

        internal NodeHeadData GetNodeHeadData()
        {
            var nodeHead = NodeHead.Get(Id);
            
            return new NodeHeadData
            {
                NodeId = Id,
                NodeTypeId = NodeTypeId,
                ContentListTypeId = ContentListTypeId,
                ContentListId = ContentListId,
                CreatingInProgress = CreatingInProgress,
                IsDeleted = IsDeleted,
                ParentNodeId = ParentId,
                Name = Name,
                DisplayName = DisplayName,
                Path = Path,
                Index = Index,
                Locked = Locked,
                LockedById = LockedById,
                ETag = ETag,
                LockType = LockType,
                LockTimeout = LockTimeout,
                LockDate = LockDate,
                LockToken = LockToken,
                LastLockUpdate = LastLockUpdate,
                LastMinorVersionId = nodeHead?.LastMinorVersionId ?? 0,
                LastMajorVersionId = nodeHead?.LastMajorVersionId ?? 0,
                CreationDate = CreationDate,
                CreatedById = CreatedById,
                ModificationDate = ModificationDate,
                ModifiedById = ModifiedById,
                IsSystem = IsSystem,
                OwnerId = OwnerId,
                SavingState = SavingState,
                Timestamp = NodeTimestamp,
            };
        }
        internal VersionData GetVersionData()
        {
            return new VersionData
            {
                VersionId = VersionId,
                NodeId = Id,
                Version = Version,
                CreationDate = VersionCreationDate,
                CreatedById = VersionCreatedById,
                ModificationDate = VersionModificationDate,
                ModifiedById = VersionModifiedById,
                ChangedData = ChangedData,
                Timestamp = VersionTimestamp,
            };
        }
        internal DynamicPropertyData GetDynamicData(bool allBinaries)
        {
            lock (_readPropertySync)
            {
                var changedPropertyTypes = dynamicData.Keys.Select(x => ActiveSchema.PropertyTypes.GetItemById(x)).ToArray();
                var binaryTypes =
                    (allBinaries ? (IEnumerable<PropertyType>) PropertyTypes : changedPropertyTypes)
                    .Where(pt => pt.DataType == DataType.Binary).ToArray();
                return new DynamicPropertyData
                {
                    VersionId = VersionId,
                    PropertyTypes = PropertyTypes.ToList(),
                    DynamicProperties = changedPropertyTypes
                        .Where(pt => pt.DataType != DataType.Binary)
                        .ToDictionary(pt => pt, pt => dynamicData[pt.Id] ?? pt.DefaultValue),
                    BinaryProperties = binaryTypes
                        .ToDictionary(pt => pt, pt => (BinaryDataValue)GetDynamicRawData(pt))
                };                
            }
        }

        public IDictionary<string, object> GetDynamicDataNameKey()
        {
            lock (_readPropertySync)
                return new ReadOnlyDictionary<string, object>(
                    dynamicData.ToDictionary(x => ActiveSchema.PropertyTypes[x.Key].Name, x => x.Value));
        }
        public IDictionary<int, object> GetDynamicDataIdKey()
        {
            lock (_readPropertySync)
                return new ReadOnlyDictionary<int, object>(dynamicData);
        }

        internal TypeCollection<PropertyType> PropertyTypes { get; private set; }
        private int[] TextPropertyIds { get; set; }

        // ----------------------------------------------------------- Structure

        internal NodeData SharedData
        {
            get { return _sharedData; }
            set { _sharedData = value; }
        }
        internal bool IsShared
        {
            get { return _isShared; }
            set { _isShared = value; }
        }

        // ----------------------------------------------------------- Information Properties

        internal bool PathChanged
        {
            get { return staticDataIsModified[(int)StaticDataSlot.Path]; }
        }
        internal string OriginalPath
        {
            get
            {
                int index = (int)StaticDataSlot.Path;
                if (SharedData == null)
                    return (string)staticData[index];
                return (string)SharedData.staticData[index];
            }
        }
        internal bool AnyDataModified
        {
            get
            {
                if (IsShared)
                    return false;
                foreach (var b in staticDataIsModified)
                    if(b)
                        return true;

                var sharedDynamicData = this._sharedData.dynamicData;
                foreach (var propId in dynamicData.Keys)
                {
                    var propType = ActiveSchema.PropertyTypes.GetItemById(propId);
                    var origData = sharedDynamicData[propId];
                    var privData = dynamicData[propId];
                    if (RelevantChange(origData, privData, propType.DataType))
                        return true;
                }
                return false;
            }
        }
        internal bool IsModified(PropertyType propertyType)
        {
            if (IsShared)
                return false;
            bool result = false;
            lock (_readPropertySync)
            {
                result = dynamicData.ContainsKey(propertyType.Id);
            }
            return result;
        }
        internal string DefaultName { get; private set; }

        // ----------------------------------------------------------- Cached properties

        private string ContentFieldXml { get; set; }

        // =========================================================== Construction

        internal static readonly int StaticDataSlotCount;
        static NodeData()
        {
            StaticDataSlotCount = Enum.GetValues(typeof(StaticDataSlot)).Length;
        }

        public NodeData(int nodeTypeId, int contentListTypeId)
            : this(NodeTypeManager.Current.NodeTypes.GetItemById(nodeTypeId), NodeTypeManager.Current.ContentListTypes.GetItemById(contentListTypeId)) { }
        public NodeData(NodeType nodeType, ContentListType contentListType)
        {
            staticDataIsModified = new bool[StaticDataSlotCount];
            staticData = new object[StaticDataSlotCount];

            PropertyTypes = NodeTypeManager.GetDynamicSignature(nodeType.Id, contentListType == null ? 0 : contentListType.Id);
            TextPropertyIds = PropertyTypes.Where(p => p.DataType == DataType.Text).Select(p => p.Id).ToArray();

            dynamicData = new Dictionary<int, object>();
        }

        #region // ===========================================================  Static data slot shortcuts and accessors

        private T Get<T>(StaticDataSlot slot)
        {
            var value = Get(slot);
            return (value == null) ? default(T) : (T)value;
        }
        private void Set<T>(StaticDataSlot slot, T value) where T : IComparable
        {
            if (SharedData != null)
            {
                var index = (int)slot;
                var storedValue = SharedData.staticData[index];
                var sharedValue = storedValue == null ? default(T) : (T)storedValue;
                if (value == null)
                {
                    if (sharedValue == null)
                    {
                        Reset(slot);
                        return;
                    }
                }
                else if (value.CompareTo(sharedValue) == 0)
                {
                    Reset(slot);
                    return;
                }
            }
            Set(slot, (object)value);
        }
        private void SetChangedData(IEnumerable<ChangedData> value)
        {
            Set(StaticDataSlot.ChangedData, (object)value);
        }

        private object Get(StaticDataSlot slot)
        {
            int index = (int)slot;
            if (SharedData == null || staticDataIsModified[index])
                return staticData[index];
            return SharedData.staticData[index];
        }
        private void Set(StaticDataSlot slot, object value)
        {
            if (IsShared)
                throw Exception_SharedIsReadOnly();
            int index = (int)slot;

            // This conversion makes sure that the date we handle is in UTC format (e.g. if 
            // the developer provides DateTime.Now, which is in local time by default).
            if (value is DateTime)
                value = ConvertToUtcDateTime((DateTime)value);

            staticData[index] = value;
            staticDataIsModified[index] = true;
            PropertyChanged(slot.ToString());
        }
        private void Reset(StaticDataSlot slot)
        {
            if (IsShared)
                throw Exception_SharedIsReadOnly();
            var index = (int)slot;
            staticData[index] = null;
            staticDataIsModified[index] = false;
            PropertyChanged(slot.ToString());
        }

        internal int Id
        {
            get { return Get<int>(StaticDataSlot.Id); }
            set { Set<int>(StaticDataSlot.Id, value); }
        }
        internal int NodeTypeId
        {
            get { return Get<int>(StaticDataSlot.NodeTypeId); }
            set { Set<int>(StaticDataSlot.NodeTypeId, value); }
        }
        internal int ContentListId
        {
            get { return Get<int>(StaticDataSlot.ContentListId); }
            set { Set<int>(StaticDataSlot.ContentListId, value); }
        }
        internal int ContentListTypeId
        {
            get { return Get<int>(StaticDataSlot.ContentListTypeId); }
            set { Set<int>(StaticDataSlot.ContentListTypeId, value); }
        }

        internal int ParentId
        {
            get { return Get<int>(StaticDataSlot.ParentId); }
            set { Set<int>(StaticDataSlot.ParentId, value); }
        }
        internal string Name
        {
            get { return Get<string>(StaticDataSlot.Name); }
            set
            {
                if (Get<string>(StaticDataSlot.Name) == null)
                    DefaultName = value;
                Set<string>(StaticDataSlot.Name, value);
            }
        }
        internal string DisplayName
        {
            get { return Get<string>(StaticDataSlot.DisplayName); }
            set { Set<string>(StaticDataSlot.DisplayName, value); }
        }
        internal string Path
        {
            get { return Get<string>(StaticDataSlot.Path); }
            set { Set<string>(StaticDataSlot.Path, value); }
        }
        internal int Index
        {
            get { return Get<int>(StaticDataSlot.Index); }
            set { Set<int>(StaticDataSlot.Index, value); }
        }
        internal bool CreatingInProgress
        {
            get { return Get<bool>(StaticDataSlot.CreatingInProgress); }
            set { Set<bool>(StaticDataSlot.CreatingInProgress, value); }
        }
        internal bool IsDeleted
        {
            get { return Get<bool>(StaticDataSlot.IsDeleted); }
            set { Set<bool>(StaticDataSlot.IsDeleted, value); }
        }

        internal DateTime CreationDate
        {
            get { return Get<DateTime>(StaticDataSlot.CreationDate); }
            set { Set<DateTime>(StaticDataSlot.CreationDate, value); }
        }
        internal DateTime ModificationDate
        {
            get { return Get<DateTime>(StaticDataSlot.ModificationDate); }
            set { Set<DateTime>(StaticDataSlot.ModificationDate, value); ModificationDateChanged = true; }
        }
        internal int CreatedById
        {
            get { return Get<int>(StaticDataSlot.CreatedById); }
            set { Set<int>(StaticDataSlot.CreatedById, value); }
        }
        internal int ModifiedById
        {
            get { return Get<int>(StaticDataSlot.ModifiedById); }
            set { Set<int>(StaticDataSlot.ModifiedById, value); ModifiedByIdChanged = true; }
        }

        internal int VersionId
        {
            get { return Get<int>(StaticDataSlot.VersionId); }
            set { Set<int>(StaticDataSlot.VersionId, value); }
        }
        internal VersionNumber Version
        {
            get { return Get<VersionNumber>(StaticDataSlot.Version); }
            set { Set<VersionNumber>(StaticDataSlot.Version, value); }
        }
        internal DateTime VersionCreationDate
        {
            get { return Get<DateTime>(StaticDataSlot.VersionCreationDate); }
            set { Set<DateTime>(StaticDataSlot.VersionCreationDate, value); }
        }
        internal DateTime VersionModificationDate
        {
            get { return Get<DateTime>(StaticDataSlot.VersionModificationDate); }
            set { Set<DateTime>(StaticDataSlot.VersionModificationDate, value); VersionModificationDateChanged = true; }
        }
        internal int VersionCreatedById
        {
            get { return Get<int>(StaticDataSlot.VersionCreatedById); }
            set { Set<int>(StaticDataSlot.VersionCreatedById, value); }
        }
        internal int VersionModifiedById
        {
            get { return Get<int>(StaticDataSlot.VersionModifiedById); }
            set { Set<int>(StaticDataSlot.VersionModifiedById, value); VersionModifiedByIdChanged = true; }
        }

        internal bool Locked
        {
            get { return Get<bool>(StaticDataSlot.Locked); }
            set { Set<bool>(StaticDataSlot.Locked, value); }
        }
        internal int LockedById
        {
            get { return Get<int>(StaticDataSlot.LockedById); }
            set { Set<int>(StaticDataSlot.LockedById, value); }
        }
        internal string ETag
        {
            get { return Get<string>(StaticDataSlot.ETag); }
            set { Set<string>(StaticDataSlot.ETag, value); }
        }
        internal int LockType
        {
            get { return Get<int>(StaticDataSlot.LockType); }
            set { Set<int>(StaticDataSlot.LockType, value); }
        }
        internal int LockTimeout
        {
            get { return Get<int>(StaticDataSlot.LockTimeout); }
            set { Set<int>(StaticDataSlot.LockTimeout, value); }
        }
        internal DateTime LockDate
        {
            get { return Get<DateTime>(StaticDataSlot.LockDate); }
            set { Set<DateTime>(StaticDataSlot.LockDate, value); }
        }
        internal string LockToken
        {
            get { return Get<string>(StaticDataSlot.LockToken); }
            set { Set<string>(StaticDataSlot.LockToken, value); }
        }
        internal DateTime LastLockUpdate
        {
            get { return Get<DateTime>(StaticDataSlot.LastLockUpdate); }
            set { Set<DateTime>(StaticDataSlot.LastLockUpdate, value); }
        }

        internal bool IsSystem
        {
            get { return Get<bool>(StaticDataSlot.IsSystem); }
            set { Set<bool>(StaticDataSlot.IsSystem, value); }
        }
        internal int OwnerId
        {
            get { return Get<int>(StaticDataSlot.OwnerId); }
            set { Set<int>(StaticDataSlot.OwnerId, value); }
        }

        internal ContentSavingState SavingState
        {
            get { return Get<ContentSavingState>(StaticDataSlot.SavingState); }
            set { Set<ContentSavingState>(StaticDataSlot.SavingState, value); }
        }
        internal IEnumerable<ChangedData> ChangedData
        {
            get { return Get<IEnumerable<ChangedData>>(StaticDataSlot.ChangedData); }
            set { SetChangedData(value); }
        }

        internal long NodeTimestamp
        {
            get { return Get<long>(StaticDataSlot.NodeTimestamp); }
            set { Set<long>(StaticDataSlot.NodeTimestamp, value); }
        }
        internal long VersionTimestamp
        {
            get { return Get<long>(StaticDataSlot.VersionTimestamp); }
            set { Set<long>(StaticDataSlot.VersionTimestamp, value); }
        }


        #endregion

        internal bool ModificationDateChanged { get; set; }
        internal bool ModifiedByIdChanged { get; set; }
        internal bool VersionModificationDateChanged { get; set; }
        internal bool VersionModifiedByIdChanged { get; set; }

        // =========================================================== Dynamic raw data accessors

        internal object GetDynamicRawData(int propertyTypeId)
        {
            var propType = this.PropertyTypes.GetItemById(propertyTypeId);
            if (propType == null)
                throw Exception_PropertyNotFound(propertyTypeId);
            return GetDynamicRawData(propType);
        }
        internal object GetDynamicRawData(PropertyType propertyType)
        {
            var id = propertyType.Id;
            object value;

            // if modified
            lock (_readPropertySync)
            {
                if (dynamicData.TryGetValue(id, out value))
                    return value;
            }
            
            if (SharedData != null)
            {
                // if loaded
                lock (_readPropertySync)
                {
                    if (SharedData.dynamicData.TryGetValue(id, out value))
                        return value;
                } 
                return SharedData.LoadProperty(propertyType);
            }
            
            if (this.IsShared)
                return LoadProperty(propertyType);

            return null;
        }

        internal void SetDynamicRawData(int propertyTypeId, object data)
        {
            SetDynamicRawData(propertyTypeId, data, true);
        }
        internal void SetDynamicRawData(int propertyTypeId, object data, bool withCheckModifying)
        {
            var propType = this.PropertyTypes.GetItemById(propertyTypeId);
            
            // if the property has been deleted in the meantime, do not add it
            if (propType == null)
                return;

            SetDynamicRawData(propType, data, withCheckModifying);
        }
        internal void SetDynamicRawData(PropertyType propertyType, object data)
        {
            SetDynamicRawData(propertyType, data, true);
        }
        internal void SetDynamicRawData(PropertyType propertyType, object data, bool withCheckModifying)
        {
            if (IsShared)
                throw Exception_SharedIsReadOnly();

            if (propertyType.DataType == DataType.Binary)
                if (data != null && !(data is BinaryDataValue))
                    throw new NotSupportedException(
                        String.Format("An instance of {0} cannot be any Binary property value. Only {1} allowed", data.GetType().Name, typeof(BinaryDataValue).Name));

            // This conversion makes sure that the date we handle is in UTC format (e.g. if 
            // the developer provides DateTime.Now, which is in local time by default).
            if (data is DateTime)
                data = ConvertToUtcDateTime((DateTime)data);

            var id = propertyType.Id;
            if (!withCheckModifying || IsDynamicPropertyChanged(propertyType, data))
            {
                lock (_readPropertySync)
                {
                    if (dynamicData.ContainsKey(id))
                        dynamicData[propertyType.Id] = data;
                    else
                        dynamicData.Add(id, data);
                }
            }
            else
            {
                ResetDynamicRawData(propertyType);
            }

            PropertyChanged(propertyType.Name);
        }

        internal void CheckChanges(PropertyType propertyType)
        {
            lock (_readPropertySync)
            {
                if (!dynamicData.ContainsKey(propertyType.Id))
                    return;
                if (!IsDynamicPropertyChanged(propertyType, dynamicData[propertyType.Id]))
                    ResetDynamicRawData(propertyType);
            }
        }
        private void ResetDynamicRawData(PropertyType propertyType)
        {
            lock (_readPropertySync)
            {
                if (dynamicData.ContainsKey(propertyType.Id))
                    dynamicData.Remove(propertyType.Id);
            }
        }
        internal bool IsPropertyChanged(string propertyName)
        {
            if (IsShared)
                return false;
            if (SharedData == null)
                return true;

            StaticDataSlot slot;
            if (Enum.TryParse<StaticDataSlot>(propertyName, out slot))
                return staticDataIsModified[(int)slot];

            var propType = ActiveSchema.PropertyTypes[propertyName];
            if (propType == null)
                throw Exception_PropertyNotFound(propertyName);

            object currentValue;
            if(dynamicData.TryGetValue(propType.Id, out currentValue))
                return IsDynamicPropertyChanged(propType, currentValue);
            return false;
        }
        private bool IsPropertyChanged(PropertyType propertyType)
        {
            throw new SnNotSupportedException();
        }
        internal bool IsDynamicPropertyChanged(PropertyType propertyType, object data)
        {
            if (IsShared)
                return false;
            if (SharedData == null)
                return true;
            var containsPropertyType = false;
            lock (_readPropertySync)
            {
                containsPropertyType = SharedData.dynamicData.ContainsKey(propertyType.Id);
            }
            if (!containsPropertyType) 
                SharedData.LoadProperty(propertyType);
            var propId = propertyType.Id;
            object sharedDynamicData = null;
            lock (_readPropertySync)
            {
                sharedDynamicData = SharedData.dynamicData[propId];
            }

            if (data == null && sharedDynamicData == null)
                return false;
            if (data == null || sharedDynamicData == null)
                return true;

            switch (propertyType.DataType)
            {
                case DataType.String:
                case DataType.Text:
                    return ((string)data != (string)sharedDynamicData);
                case DataType.Int:
                    return ((int)data != (int)sharedDynamicData);
                case DataType.Currency:
                    return ((decimal)data != (decimal)sharedDynamicData);
                case DataType.DateTime:
                    return ((DateTime)data != (DateTime)sharedDynamicData);
                case DataType.Binary:
                    return IsBinaryChanged(data, sharedDynamicData);
                case DataType.Reference:
                    return IsIdListsChanged(data, sharedDynamicData);
                default:
                    throw new SnNotSupportedException(propertyType.DataType.ToString());
            }
        }
        private bool IsBinaryChanged(object value, object original)
        {
            var a = (BinaryDataValue)value;
            var b = (BinaryDataValue)original;
            if (a == null && b == null)
                return false;
            if (!(a != null && b != null))
                return true;
            if (a.Id != b.Id)
                return true;
            if (a.FileId != b.FileId)
                return true;
            if (a.ContentType != b.ContentType)
                return true;
            if (a.FileName != b.FileName)
                return true;
            if (a.Size != b.Size)
                return true;
            if (a.Timestamp == 0 || b.Timestamp == 0 || a.Timestamp != b.Timestamp)
                return true;

            return false;
        }
        private static bool IsIdListsChanged(object value, object original)
        {
            var a = (List<int>)value;
            var b = (List<int>)original;
            if (a == null && b == null)
                return false;
            if (!(a != null && b != null))
                return true;
            if (a.Count != b.Count)
                return true;
            for (int i = 0; i < a.Count; i++)
                if (a[i] != b[i])
                    return true;
            return false;
        }

        private object LoadProperty(PropertyType propertyType)
        {
            var propId = propertyType.Id;
            lock (_readPropertySync)
            {
                if (dynamicData.ContainsKey(propId))
                    return dynamicData[propId];
            }

            object data;
            if (propertyType.DataType == DataType.Text)
            {
                PreloadTextProperties();
                lock (_readPropertySync)
                    return  (dynamicData.TryGetValue(propId, out data)) ? data : null;
            }

            data = DataBackingStore.LoadProperty(this.VersionId, propertyType); //UNDONE:DB!!!!!!!!!! POTENTIEL BUG SOURCE: BinaryProperty loaded without blob provider information
            lock (_readPropertySync)
                dynamicData[propId] = data;
            return data;
        }

        /// <summary>Preloads all uncached text properties to avoid more than one database access.</summary>
        internal void PreloadTextProperties()
        {
            if (Id == 0)
                return;

            if (!this.IsShared)
            {
                if (this.SharedData != null)
                    this.SharedData.PreloadTextProperties();
                return;
            }

            var notLoadedTextPropertyTypeIds = this.TextPropertyIds.Where(p => !dynamicData.ContainsKey(p)).ToArray();
            var data = DataBackingStore.LoadTextProperties(this.VersionId, notLoadedTextPropertyTypeIds);
            foreach (var id in notLoadedTextPropertyTypeIds)
            {
                string value = null;
                data.TryGetValue(id, out value);
                dynamicData[id] = value;
            }
        }


        // ---------------------------------------------------------- static structure builder

        internal static NodeData CreatePrivateData(NodeData asSharedData)
        {
            if (!asSharedData.IsShared)
                MakeSharedData(asSharedData);
            var privateData = new NodeData(asSharedData.NodeTypeId, asSharedData.ContentListTypeId) { SharedData = asSharedData };
            return privateData;
        }
        internal static void MakeSharedData(NodeData data)
        {
            if (data.SharedData != null)
            {
                MergeData(data.SharedData, data);
                data.SharedData = null;
            }
            data.IsShared = true;
        }
        private static void MergeData(NodeData shared, NodeData target)
        {
            for (int i = 0; i < target.staticData.Length; i++)
            {
                if (!target.staticDataIsModified[i])
                    target.staticData[i] = shared.staticData[i];
                target.staticDataIsModified[i] = false;
            }
            foreach (var propType in target.PropertyTypes)
            {
                var id = propType.Id;
                object sharedData;
                lock (LockObject)
                {
                    if (shared.dynamicData.TryGetValue(id, out sharedData))
                        if (!target.dynamicData.ContainsKey(id))
                            target.dynamicData.Add(id, sharedData);
                }
            }
        }

        internal void RemoveStreamsAndLongTexts()
        {
            lock (LockObject)
            {
                foreach (var propTypeId in dynamicData.Keys.ToArray())
                {
                    var propType = ActiveSchema.PropertyTypes.GetItemById(propTypeId);
                    if (propType != null)
                    {
                        if (propType.DataType == DataType.Binary)
                        {
                            var bdv = dynamicData[propTypeId] as BinaryDataValue;
                            if (bdv != null)
                                bdv.Stream = null;
                        }
                        if (propType.DataType == DataType.Text)
                        {
                            var item = dynamicData[propTypeId] as string;
                            var isCacheable = DataStore.Enabled ? DataStore.IsCacheableText(item) : DataProvider.Current.IsCacheableText(item); //DB:ok
                            if (!isCacheable)
                                dynamicData.Remove(propTypeId);
                        }
                    }
                }
            }
        }

        // ---------------------------------------------------------- copy

        private static readonly StaticDataSlot[] slotsToCopy = {
            // StaticDataSlot.Id, StaticDataSlot.VersionId, StaticDataSlot.ParentId, StaticDataSlot.Path, StaticDataSlot.Name,
            StaticDataSlot.DisplayName,
            // StaticDataSlot.Locked, StaticDataSlot.LockedById,
            // StaticDataSlot.ETag, StaticDataSlot.LockType, StaticDataSlot.LockTimeout, StaticDataSlot.LockDate, StaticDataSlot.LockToken, StaticDataSlot.LastLockUpdate
            StaticDataSlot.NodeTypeId,
            // StaticDataSlot.ContentListId, StaticDataSlot.ContentListTypeId,
            StaticDataSlot.Index, StaticDataSlot.IsDeleted,
            StaticDataSlot.CreationDate, StaticDataSlot.ModificationDate, StaticDataSlot.CreatedById, StaticDataSlot.ModifiedById, StaticDataSlot.OwnerId,
            StaticDataSlot.Version,
            StaticDataSlot.VersionCreationDate, StaticDataSlot.VersionModificationDate, StaticDataSlot.VersionCreatedById, StaticDataSlot.VersionModifiedById,

            //TODO: IsSystem, SavingState, ChangedData
        };

        internal void CopyGeneralPropertiesTo(NodeData target)
        {
            foreach (var slot in slotsToCopy)
                target.Set(slot, Get(slot));
        }
        internal void CopyDynamicPropertiesTo(NodeData target)
        {
            foreach (var propType in PropertyTypes)
            {
                if (Node.EXCLUDED_COPY_PROPERTIES.Contains(propType.Name)) continue;

                if (!propType.IsContentListProperty || target.PropertyTypes[propType.Name] != null)
                    target.SetDynamicRawData(propType, GetDynamicRawData(propType));
            }
        }

        // ---------------------------------------------------------- exception helpers

        internal static Exception Exception_SharedIsReadOnly()
        {
            return new NotSupportedException("#### Storage2: shared data is read only.");
        }
        internal static Exception Exception_PropertyNotFound(string name)
        {
            return Exception_PropertyNotFound(name, null);
        }
        internal static Exception Exception_PropertyNotFound(string name, string typeName)
        {
            var tn = string.IsNullOrEmpty(typeName) ? string.Empty : ". Content type name: " + typeName;
            var propType = NodeTypeManager.Current.PropertyTypes[name];
            if (propType == null)
                return new ApplicationException("PropertyType not found. Name: " + name + tn);
            return new ApplicationException(String.Concat("Unknown property. Id: ", propType.Id, ", Name: ", name, tn));
        }
        internal static Exception Exception_PropertyNotFound(int propTypeId)
        {
            var propType = NodeTypeManager.Current.PropertyTypes.GetItemById(propTypeId);
            if (propType == null)
                return new ApplicationException("PropertyType not found. Id: " + propTypeId);
            return new ApplicationException(String.Concat("Unknown property. Id: ", propType.Id, ", Name: ", propType.Name));
        }

        // ---------------------------------------------------------- transaction

        private SnapshotData _snapshotData;
        internal void CreateSnapshotData()
        {
            var binIds = new Dictionary<int, Tuple<int, int>>();
            foreach (var propType in PropertyTypes)
            {
                if (propType.DataType == DataType.Binary)
                {
                    var binValue = GetDynamicRawData(propType) as BinaryDataValue;
                    if (binValue != null)
                        binIds.Add(propType.Id, new Tuple<int, int>(binValue.Id, binValue.FileId));
                }
            }
            _snapshotData = new SnapshotData
            {
                Id = this.Id,
                Path = this.Path,
                NodeTimestamp = this.NodeTimestamp,
                VersionId = this.VersionId,
                Version = this.Version,
                VersionTimestamp = this.VersionTimestamp,
                BinaryIds = binIds
            };
        }
        internal void Rollback()
        {
            if (IsShared)
                throw Exception_SharedIsReadOnly();

            this.Id = _snapshotData.Id;
            this.Path = _snapshotData.Path;
            this.NodeTimestamp = _snapshotData.NodeTimestamp;
            this.VersionId = _snapshotData.VersionId;
            this.Version = _snapshotData.Version;
            this.VersionTimestamp = _snapshotData.VersionTimestamp;
            foreach (var propTypeId in _snapshotData.BinaryIds.Keys)
            {
                var binValue = GetDynamicRawData(propTypeId) as BinaryDataValue;
                if (binValue != null)
                {
                    var item = _snapshotData.BinaryIds[propTypeId];
                    binValue.Id = item.Item1;
                    binValue.FileId = item.Item2;
                }
            }
        }

        internal IEnumerable<ChangedData> GetChangedValues()
        {
            var changes = new List<ChangedData>();
            if (this._sharedData == null)
                return changes;
            var sharedStaticData = this._sharedData.staticData;

            for (int i = 0; i < staticData.Length; i++)
            {
                if (staticDataIsModified[i])
                {
                    var slot = (StaticDataSlot)i;
                    changes.Add(new ChangedData
                    {
                        Name = slot.ToString(),
                        Original = FormatStaticData(sharedStaticData[i], slot),
                        Value = FormatStaticData(staticData[i], slot)
                    });
                    var spec = FormatSpecialStaticChangedValue(slot, sharedStaticData[i], staticData[i]);
                    if (spec != null)
                        changes.Add(spec);
                }
            }

            var sharedDynamicData = this._sharedData.dynamicData;
            foreach (var propId in dynamicData.Keys)
            {
                var propType = ActiveSchema.PropertyTypes.GetItemById(propId);
                var origData = sharedDynamicData[propId];
                var privData = dynamicData[propId];
                if (RelevantChange(origData, privData, propType.DataType))
                {
                    changes.Add(new ChangedData
                    {
                        Name = propType.Name,
                        Original = FormatDynamicData(origData, propType.DataType),
                        Value = FormatDynamicData(privData, propType.DataType)
                    });
                }
            }

            return changes;
        }
        private bool RelevantChange(object origData, object privData, DataType dataType)
        {
            if (dataType == DataType.Reference)
            {
                if (origData == null)
                {
                    if (privData == null)
                        return false;

                    var list = privData as List<int>;
                    if (list != null)
                        return list.Count > 0;
                }
            }
            return true;
        }

        internal IDictionary<string, object> GetAllValues()
        {
            var values = new Dictionary<string, object>();

            values.Add("Id", Id);
            values.Add("NodeTypeId", NodeTypeId);
            values.Add("NodeType", FormatNodeType(NodeTypeId));
            values.Add("ContentListId", ContentListId);
            values.Add("ContentListTypeId", ContentListTypeId);
            values.Add("ParentId", ParentId);
            values.Add("Name", Name);
            values.Add("DisplayName", DisplayName);
            values.Add("Path", Path);
            values.Add("Index", Index);
            values.Add("CreatingInProgress", CreatingInProgress.ToString().ToLower());
            values.Add("IsDeleted", IsDeleted.ToString().ToLower());
            values.Add("CreationDate", FormatDate(CreationDate));
            values.Add("ModificationDate", FormatDate(ModificationDate));
            values.Add("CreatedById", CreatedById);
            values.Add("CreatedBy", FormatUser(CreatedById));
            values.Add("ModifiedById", ModifiedById);
            values.Add("ModifiedBy", FormatUser(ModifiedById));
            values.Add("VersionId", VersionId);
            values.Add("Version", Version.ToString());
            values.Add("VersionCreationDate", FormatDate(VersionCreationDate));
            values.Add("VersionModificationDate", FormatDate(VersionModificationDate));
            values.Add("VersionCreatedById", VersionCreatedById);
            values.Add("VersionCreatedBy", FormatUser(VersionCreatedById));
            values.Add("VersionModifiedById", VersionModifiedById);
            values.Add("VersionModifiedBy", FormatUser(VersionModifiedById));
            values.Add("Locked", Locked.ToString().ToLower());
            values.Add("LockedById", LockedById);
            values.Add("LockedBy", FormatUser(LockedById));
            values.Add("ETag", ETag);
            values.Add("LockType", LockType);
            values.Add("LockTimeout", LockTimeout);
            values.Add("LockDate", FormatDate(LockDate));
            values.Add("LockToken", LockToken);
            values.Add("LastLockUpdate", FormatDate(LastLockUpdate));
            values.Add("IsSystem", IsSystem.ToString().ToLower());
            values.Add("OwnerId", OwnerId);
            values.Add("Owner",  FormatUser(OwnerId));
            values.Add("SavingState", SavingState);

            foreach (var key in dynamicData.Keys)
            {
                var propType = ActiveSchema.PropertyTypes.GetItemById(key);
                if (propType != null)
                    values.Add(propType.Name.Replace("#", "_"), FormatDynamicData(dynamicData[key] ?? string.Empty, propType.DataType));
            }
            return values;
        }

        private string FormatStaticData(object data, StaticDataSlot slot)
        {
            switch (slot)
            {
                case StaticDataSlot.Id:
                case StaticDataSlot.NodeTypeId:
                case StaticDataSlot.ContentListId:
                case StaticDataSlot.ContentListTypeId:
                case StaticDataSlot.ParentId:
                case StaticDataSlot.Name:
                case StaticDataSlot.DisplayName:
                case StaticDataSlot.Path:
                case StaticDataSlot.Index:
                case StaticDataSlot.VersionId:
                case StaticDataSlot.Version:
                case StaticDataSlot.ETag:
                case StaticDataSlot.LockType:
                case StaticDataSlot.LockTimeout:
                case StaticDataSlot.LockToken:
                    return data == null ? String.Empty : data.ToString();

                case StaticDataSlot.IsDeleted:
                case StaticDataSlot.Locked:
                case StaticDataSlot.IsSystem:
                    return data.ToString().ToLower();

                case StaticDataSlot.CreatedById:
                case StaticDataSlot.ModifiedById:
                case StaticDataSlot.VersionCreatedById:
                case StaticDataSlot.VersionModifiedById:
                case StaticDataSlot.LockedById:
                case StaticDataSlot.OwnerId:
                case StaticDataSlot.SavingState:
                    return data.ToString();

                case StaticDataSlot.CreationDate:
                case StaticDataSlot.ModificationDate:
                case StaticDataSlot.VersionCreationDate:
                case StaticDataSlot.VersionModificationDate:
                case StaticDataSlot.LockDate:
                case StaticDataSlot.LastLockUpdate:
                    return FormatDate((DateTime)data);

                default:
                    return string.Empty;
            }
        }
        private ChangedData FormatSpecialStaticChangedValue(StaticDataSlot slot, object oldValue, object newValue)
        {
            string name = null;
            switch (slot)
            {
                case StaticDataSlot.CreatedById: name = "CreatedBy"; break;
                case StaticDataSlot.ModifiedById: name = "ModifiedBy"; break;
                case StaticDataSlot.VersionCreatedById: name = "VersionCreatedBy"; break;
                case StaticDataSlot.VersionModifiedById: name = "VersionModifiedBy"; break;
                case StaticDataSlot.LockedById: name = "LockedBy"; break;
            }
            if (name == null)
                return null;
            return new ChangedData
            {
                Name = name,
                Original = FormatUser((int)oldValue),
                Value = FormatUser((int)newValue)
            };
        }
        private string FormatDynamicData(object data, DataType dataType)
        {
            if (data == null)
                return string.Empty;
            
            switch (dataType)
            {
                case DataType.Text:
                case DataType.String:
                case DataType.Int:
                case DataType.Currency:
                    return Convert.ToString(data, CultureInfo.InvariantCulture);
                case DataType.DateTime:
                    // cast cannot be used here
                    if (!(data is DateTime))
                        return string.Empty;
                    return FormatDate((DateTime)data);
                case DataType.Binary:
                    var bin = data as BinaryDataValue;
                    if (bin == null)
                        return string.Empty;
                    return String.Format(CultureInfo.InvariantCulture,
                              "Id: {0}, FileId: {1}, MimeType: {2}, FileName: {3}, IsEmpty: {4}, Size: {5}, Checksum {6}, Timestamp {7}",
                              bin.Id, bin.FileId, bin.ContentType, bin.FileName, bin.IsEmpty, bin.Size, bin.Checksum, bin.Timestamp);
                case DataType.Reference:
                    var refIds = data as IEnumerable<int>;
                    if (refIds != null)
                        return String.Join(", ", (from id in refIds select id.ToString()).ToArray());

                    var refs = data as NodeList<Node>;
                    if (refs != null)
                        return String.Join(", ", (from id in refs.GetIdentifiers() select id.ToString()).ToArray());

                    return string.Empty;
            }
            return string.Empty;
        }
        private string RemoveCDataFromText(string text)
        {
            return text.Replace("]]>", @"\]\]\>");
        }

        private string FormatDate(DateTime dateTime)
        {
            return dateTime.ToString("yyyy-MM-dd HH:mm:ss.ffffK", CultureInfo.InvariantCulture);
        }
        private string FormatNodeType(int id)
        {
            var nt = ActiveSchema.NodeTypes.GetItemById(id);
            if (nt == null)
                return string.Empty;
            return nt.Name;
        }
        private string FormatUser(int id)
        {
            // Elevation: this method is used by logging, the permissions 
            // of the current user should not affect that.
            using (new SystemAccount())
            {
                var n = Node.LoadNode(id);
                var u = n as IUser;
                if (u == null)
                    return string.Empty;
                return u.Username; 
            }
        }

        /*=========================================================== Shared extension */

        private Dictionary<string, object> _extendedSharedData;
        private ReaderWriterLockSlim _extendedSharedDataLock;
        private object _sharedDataCreateLock = new object();

        internal object GetExtendedSharedData(string name)
        {
            if (name == null)
                throw new ArgumentNullException("name");

            var shared = IsShared ? this : SharedData;
            if (shared == null)
                return null;

            return shared.GetExtendedSharedDataPrivate(name);
        }
        private object GetExtendedSharedDataPrivate(string name)
        {
            switch (name)
            {
                case "ContentFieldXml":
                    return ContentFieldXml;
            }

            var dict = _extendedSharedData;
            if (dict == null)
                return null;

            _extendedSharedDataLock.EnterReadLock();
            try
            {
                object value;
                if (dict.TryGetValue(name, out value))
                    return value;
                return null;
            }
            finally
            {
                _extendedSharedDataLock.ExitReadLock();
            }
        }
        internal void SetExtendedSharedData(string name, object value)
        {
            if (name == null)
                throw new ArgumentNullException("name");

            var shared = IsShared ? this : SharedData;
            if (shared == null)
                return;

            shared.SetExtendedSharedDataPrivate(name, value);
        }
        internal void SetExtendedSharedDataPrivate(string name, object value)
        {
            switch (name)
            {
                case "ContentFieldXml":
                    ContentFieldXml = value as string;
                    return;
            }

            if (_extendedSharedData == null)
            {
                lock (_sharedDataCreateLock)
                {
                    if (_extendedSharedData == null)
                    {
                        _extendedSharedDataLock = new ReaderWriterLockSlim();
                        _extendedSharedData = new Dictionary<string, object>();
                    }
                }
            }
            var dict = _extendedSharedData;

            _extendedSharedDataLock.EnterWriteLock();
            try
            {
                if (!dict.ContainsKey(name))
                    dict.Add(name, value);
                else
                    dict[name] = value;
            }
            finally
            {
                _extendedSharedDataLock.ExitWriteLock();
            }
        }
        internal void ResetExtendedSharedData(string name)
        {
            if (name == null)
                throw new ArgumentNullException("name");

            if (SharedData == null)
                return;

            SharedData.ResetExtendedSharedDataPrivate(name);
        }
        internal void ResetExtendedSharedDataPrivate(string name)
        {
            switch (name)
            {
                case "ContentFieldXml":
                    SharedData.ContentFieldXml = null;
                    return;
            }

            var dict = _extendedSharedData;
            if (dict == null)
                return;

            _extendedSharedDataLock.EnterReadLock();
            try
            {
                if (dict.ContainsKey(name))
                    dict.Remove(name);
            }
            finally
            {
                _extendedSharedDataLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Converts the given datetime to a datetime in UTC format. If it is already in UTC, there will be 
        /// no conversion. Undefined datetime will be considered as UTC. A duplicate of this method exists 
        /// in the ContentRepository layer.
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        internal static DateTime ConvertToUtcDateTime(DateTime dateTime)
        {
            switch (dateTime.Kind)
            {
                case DateTimeKind.Local:
                    return dateTime.ToUniversalTime();
                case DateTimeKind.Utc:
                    return dateTime;
                case DateTimeKind.Unspecified:
                    return DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
                default:
                    throw new InvalidOperationException("Unknown datetime kind: " + dateTime.Kind);
            }
       }

        internal Action<string> PropertyChangedCallback;
        protected virtual void PropertyChanged(string propertyName)
        {
            PropertyChangedCallback?.Invoke(propertyName);
        }

        /*=========================================================== Testing tools */

        //UNDONE:DB -------Delete NodeData.Clone feature
        /// <summary>
        /// For test purposes.
        /// </summary>
        internal NodeData Clone()
        {
            var isSharedBefore = IsShared;
            IsShared = false;

            var sharedClone = SharedData?.Clone();

            var clone = new NodeData(NodeType.GetById( this.NodeTypeId), NodeTypeManager.Current.ContentListTypes.GetItemById(this.ContentListTypeId))
            {
                _isShared = _isShared,
                SharedData = sharedClone,

                // static slots
                Id = Id,
                NodeTypeId = NodeTypeId,
                ContentListId = ContentListId,
                ContentListTypeId = ContentListTypeId,
                ParentId = ParentId,
                Name = Name,
                DisplayName = DisplayName,
                Path = Path,
                Index = Index,
                CreatingInProgress = CreatingInProgress,
                IsDeleted = IsDeleted,
                CreationDate = CreationDate,
                ModificationDate = ModificationDate,
                CreatedById = CreatedById,
                ModifiedById = ModifiedById,
                VersionId = VersionId,
                Version = Version,
                VersionCreationDate = VersionCreationDate,
                VersionModificationDate = VersionModificationDate,
                VersionCreatedById = VersionCreatedById,
                VersionModifiedById = VersionModifiedById,
                Locked = Locked,
                LockedById = LockedById,
                ETag = ETag,
                LockType = LockType,
                LockTimeout = LockTimeout,
                LockDate = LockDate,
                LockToken = LockToken,
                LastLockUpdate = LastLockUpdate,
                IsSystem = IsSystem,
                OwnerId = OwnerId,
                SavingState = SavingState,
                ChangedData = ChangedData,
                NodeTimestamp = NodeTimestamp,
                VersionTimestamp = VersionTimestamp,

                //
                ModificationDateChanged = ModificationDateChanged,
                ModifiedByIdChanged = ModifiedByIdChanged,
                VersionModificationDateChanged = VersionModificationDateChanged,
                VersionModifiedByIdChanged = VersionModifiedByIdChanged,

                //
                DefaultName = DefaultName,
                ContentFieldXml = ContentFieldXml,
            };
            CloneDynamicData(clone);

            IsShared = isSharedBefore;
            return clone;
        }
        private void CloneDynamicData(NodeData clone)
        {
            var propTypes = NodeTypeManager.Current.PropertyTypes;
            var target = clone.dynamicData;
            target.Clear();
            foreach (var item in dynamicData)
            {
                var propType = propTypes.GetItemById(item.Key);
                switch (propType.DataType)
                {
                    case DataType.String:
                    case DataType.Text:
                    case DataType.Int:
                    case DataType.Currency:
                        target.Add(item.Key, item.Value);
                        break;
                    case DataType.DateTime:
                        target.Add(item.Key, item.Value == null ? null : (object)(new DateTime(((DateTime) item.Value).Ticks)));
                        break;
                    case DataType.Binary:
                        target.Add(item.Key, CloneBinaryProperty((BinaryDataValue)item.Value));
                        break;
                    case DataType.Reference:
                        target.Add(item.Key, ((List<int>) item.Value)?.ToList());
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
        private BinaryDataValue CloneBinaryProperty(BinaryDataValue original)
        {
            if (original == null)
                return null;

            return new BinaryDataValue
            {
                Id = original.Id,
                Stream = CloneStream(original.Stream),
                FileId = original.FileId,
                Size = original.Size,
                FileName = original.FileName,
                ContentType = original.ContentType,
                Checksum = original.Checksum,
                Timestamp = original.Timestamp,
                BlobProviderName = original.BlobProviderName,
                BlobProviderData = original.BlobProviderData,
            };
        }
        private Stream CloneStream(Stream original)
        {
            if (original == null)
                return null;
            return Stream.Null;
        }

        //UNDONE:DB -------Delete NodeData.GetPropertyNames
        /// <summary>
        /// For test purposes.
        /// </summary>
        internal string[] GetPropertyNames()
        {
            return PropertyTypes.Select(x => x.Name).ToArray();
        }
    }
}
