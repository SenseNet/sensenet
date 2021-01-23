using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Search;
using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.ContentRepository.Search.Querying;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Storage.Search;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.ContentRepository.Storage.Events;
using SenseNet.ContentRepository.Storage.Caching.Dependency;
using SenseNet.ContentRepository.Storage.Data.MsSqlClient;
using SenseNet.Diagnostics;
using SenseNet.Events;
using SenseNet.Search;
using SenseNet.Search.Querying;
using SenseNet.Security;
// ReSharper disable ArrangeThisQualifier

namespace SenseNet.ContentRepository.Storage
{
    /// <summary>
    /// Represents the method that will handle the revocable events of a <see cref="Node"/>.
    /// </summary>
    public delegate void CancellableNodeEventHandler(object sender, CancellableNodeEventArgs e);
    /// <summary>
    /// Represents the method that will handle the revocable batch operation events of a <see cref="Node"/>.
    /// </summary>
    public delegate void CancellableNodeOperationEventHandler(object sender, CancellableNodeOperationEventArgs e);

    internal enum AccessLevel { Header, Major, Minor }

    /// <summary>
    /// Defines constants for version raising modes during saving a <see cref="Node"/>.
    /// </summary>
    public enum VersionRaising
    {
        /// <summary>Version number will not be changed.</summary>
        None,
        /// <summary>Minor number will be increased.</summary>
        NextMinor,
        /// <summary>Major number will be increased.</summary>
        NextMajor
    }

    /// <summary>
    /// Defines constants of saving states.
    /// </summary>
    public enum ContentSavingState
    {
        /// <summary>Saving the <see cref="Node"/> is finished.</summary>
        Finalized,
        /// <summary>The <see cref="Node"/> is created but not saved (finalized) yet.</summary>
        Creating,
        /// <summary>The <see cref="Node"/> is modified.</summary>
        Modifying,
        /// <summary>The <see cref="Node"/> is modified in checked-out state.</summary>
        ModifyingLocked
    }

    /// <summary>
    /// <para>Represents a structured set of data that can be stored in the sensenet Content Repository.</para>
    /// <para>A <see cref="Node"/> can be loaded from the sensenet Content Repository, the data can be modified via the properties
    /// of the <see cref="Node"/>, and the changes can be persisted to the database.</para>
    /// </summary>
    /// <remarks>Remark</remarks>
    /// <example>Here is an example from the test project that creates a new <see cref="Node"/> with two integer Properties TestInt1 and TestInt2
    /// <code>
    /// [ContentHandler]
    /// public class TestNode : Node
    /// {
    /// public TestNode(Node parent) : base(parent) { }
    /// public TestNode(NodeToken token) : base(token) { }
    /// [RepositoryProperty("TestInt1")]
    /// public int TestInt
    /// {
    /// get { return (int)this["TestInt1"]; }
    /// set { this["TestInt1"] = value; }
    /// }
    /// [RepositoryProperty("TestInt2", DataType.Int)]
    /// public string TestInt2
    /// {
    /// get { return this["TestInt2"].ToString(); }
    /// set { this["TestInt2"] = Convert.ToInt32(value); }
    /// }
    /// }
    /// </code></example>
    [DebuggerDisplay("Id={Id}, Name={Name}, Version={Version}, Path={Path}")]
    public abstract class Node : IPasswordSaltProvider
    {
        private NodeData _data;
        internal NodeData Data => _data;

        private List<INodeOperationValidator> NodeOperationValidators =>
            Providers.Instance.GetProvider<List<INodeOperationValidator>>("NodeOperationValidators");

        private void SetNodeData(NodeData nodeData)
        {
            _data = nodeData;
            _data.PropertyChangedCallback = PropertyChanged;
        }

        /// <summary>
        /// This method is called when a property of the owner <see cref="Node"/> is changed.
        /// </summary>
        /// <param name="propertyName">Name of the changed property.</param>
        protected virtual void PropertyChanged(string propertyName)
        {

        }

        /// <summary>
        /// Gets true if the audit log is enabled.
        /// This property is obsolete. Use the SenseNet.Configuration.Logging.AuditEnabled property instead.
        /// </summary>
        [Obsolete("After V6.5 PATCH 9: Use SenseNet.Configuration.Logging.AuditEnabled instead.")]
        public static bool AuditEnabled => Logging.AuditEnabled;

        private bool _copying;
        /// <summary>
        /// Gets true if this instance is a target of a copy operation.
        /// </summary>
        public bool CopyInProgress
        {
            get { return _copying; }
            protected set { _copying = value; }
        }

        private bool IsDirty { get; set; }

        /// <summary>
        /// Gets a value that states whether the inherited type is ContentType or not.
        /// </summary>
        public abstract bool IsContentType { get; }

        /// <summary>
        /// Set this to override the AllowIncrementalNaming setting of ContentType programatically.
        /// </summary>
        public bool? AllowIncrementalNaming;

        /// <summary>
        /// Gets the currently running node operation (e.g. Undo checkout or Template creation).
        /// </summary>
        public string NodeOperation { get; set; } 

        private SecurityHandler _security;
        private LockHandler _lockHandler;
        /// <summary>
        /// Gets true if the current user has only See permission for this <see cref="Node"/>.
        /// </summary>
        public bool IsHeadOnly { get; private set; }
        /// <summary>
        /// Gets true if the current user has only Preview permission for this <see cref="Node"/>.
        /// </summary>
        public bool IsPreviewOnly { get; private set; }

        /// <summary>
        /// Gets a value that states if indexing is enabled for this content item. By default this is true
        /// but can be overridden in derived classes. Determines whether an indexing activity and index
        /// document will be created for this content.
        /// </summary>
        protected internal virtual bool IsIndexingEnabled => true;

        private static readonly List<string> SeeEnabledProperties = new List<string> { "Name", "Path", "Id", "Index", "NodeType", "ContentListId", "ContentListType", "Parent", "IsModified", "IsDeleted", "CreationDate", "ModificationDate", "CreatedBy", "ModifiedBy", "VersionCreationDate", "VersionModificationDate", "VersionCreatedById", "VersionModifiedById", "Aspects", "Icon", "StoredIcon" };
        /// <summary>
        /// Returns true if the current user has enough permissions to view the property identified by the given name.
        /// </summary>
        public bool IsAllowedProperty(string name)
        {
            if (!HasProperty(name))
                return false;
            if (IsPreviewOnly)
            {
                var propertyType = this.PropertyTypes[name];
                if (propertyType != null && propertyType.DataType == DataType.Binary)
                    return false;
                return true;
            }
            if (IsHeadOnly)
                return SeeEnabledProperties.Contains(name);
            return true;
        }
        /// <summary>
        /// Returns the array of property names that are allowed to be viewed by a user who has only See permission for this instance.
        /// </summary>
        public static string[] GetHeadOnlyProperties()
        {
            return SeeEnabledProperties.ToArray();
        }

        /// <summary>
        /// Gets the list of property names that are not copied from the source <see cref="Node"/> to the target instance.
        /// </summary>
        public static readonly List<string> EXCLUDED_COPY_PROPERTIES = new List<string> { "ApprovingMode", "InheritableApprovingMode", "InheritableVersioningMode", "VersioningMode", "AvailableContentTypeFields", "FieldSettingContents" };

        /// <summary>
        /// Gets the collection of child <see cref="Node"/>s. The current user needs to have See permission for every instance.
        /// </summary>
        [Obsolete("Use GetChildren() method instead.")]
        public IEnumerable<Node> PhysicalChildArray => this.GetChildren();

        /// <summary>
        /// Gets the collection of child <see cref="Node"/>s. The current user need to have See permission for every instance.
        /// </summary>
        protected virtual IEnumerable<Node> GetChildren()
        {
            var nodeHeads = DataStore.LoadNodeHeadsAsync(QueryChildren().Identifiers, CancellationToken.None)
                .GetAwaiter().GetResult();
            var user = AccessProvider.Current.GetCurrentUser();

            // use loop here instead of LoadNodes to check permissions
            return new NodeList<Node>((from nodeHead in nodeHeads
                                       where nodeHead != null && SecurityHandler.HasPermission(user, nodeHead, PermissionType.See)
                                       select nodeHead.Id));
        }
        /// <summary>
        /// Returns the count of children.
        /// </summary>
        protected int GetChildCount()
        {
            return QueryChildren().Count;
        }
        private QueryResult QueryChildren()
        {
            if (this.Id == 0)
                return new QueryResult(new NodeList<Node>());

            return NodeQuery.QueryChildren(this.Id);
        }

        /// <summary>
        /// Returns the count of <see cref="Node"/>s in the subtree.
        /// </summary>
        public virtual int NodesInTree => SearchManager.ExecuteContentQuery(
                "+InTree:@0",
                QuerySettings.AdminSettings,
                this.Path)
            .Count;

        internal void MakePrivateData()
        {
            if (!_data.IsShared)
                return;
            SetNodeData(NodeData.CreatePrivateData(_data));
        }

        #region // ================================================================================================= General Properties

        /// <summary>
        /// Gets the identifier of this <see cref="Node"/>.
        /// </summary>
        /// <remarks>Note that if you develop a data provider for the system, you have to convert your Id to an integer.</remarks>
        public int Id
        {
            get
            {
                if (_data == null)
                    return 0;
                return _data.Id;
            }
            internal set
            {
                MakePrivateData();
                _data.Id = value;
            }
        }
        /// <summary>
        /// Gets the id of this <see cref="Node"/>'s <see cref="NodeType"/>.
        /// </summary>
        public virtual int NodeTypeId
        {
            get { return _data.NodeTypeId; }
        }
        /// <summary>
        /// Gets the nearest <see cref="ContentListType"/> id on the ancestor chain.
        /// </summary>
        public virtual int ContentListTypeId
        {
            get { return _data.ContentListTypeId; }
        }
        /// <summary>
        /// Gets the <see cref="NodeType"/> of this <see cref="Node"/>.
        /// </summary>
        public virtual NodeType NodeType
        {
            get { return NodeTypeManager.Current.NodeTypes.GetItemById(_data.NodeTypeId); }
        }
        /// <summary>
        /// Gets the nearest <see cref="ContentListType"/> on the ancestor chain.
        /// </summary>
        public virtual ContentListType ContentListType
        {
            get
            {
                if (_data.ContentListTypeId == 0)
                    return null;
                return NodeTypeManager.Current.ContentListTypes.GetItemById(_data.ContentListTypeId);
            }
            internal set
            {
                MakePrivateData();
                _data.ContentListTypeId = value == null ? 0 : value.Id;
            }
        }
        /// <summary>
        /// Gets the nearest content list id on the ancestor chain.
        /// </summary>
        public virtual int ContentListId
        {
            get { return _data.ContentListId; }
            internal set
            {
                MakePrivateData();
                _data.ContentListId = value;
            }
        }

        /// <summary>
        /// Gets or sets the version of this <see cref="Node"/>.
        /// </summary>
        /// <value>The version.</value>
        public VersionNumber Version
        {
            get
            {
                return _data.Version;
            }
            set
            {
                MakePrivateData();
                _data.Version = value;
            }
        }
        /// <summary>
        /// Returns true if the Version property has changed since the last loading.
        /// </summary>
        public bool IsVersionChanged()
        {
            return _data.IsPropertyChanged("Version");
        }

        /// <summary>
        /// Gets the version id of this <see cref="Node"/>.
        /// </summary>
        public int VersionId
        {
            get
            {
                return _data.VersionId;
            }
        }

        /// <summary>
        /// Returns true if this instance is a new one.
        /// Can be useful in overridden Save methods.
        /// </summary>
        public bool CreatingInProgress
        {
            get { return _data.CreatingInProgress; }
            internal set
            {
                MakePrivateData();
                _data.CreatingInProgress = value;
            }
        }

        /// <summary>
        /// Gets a value that describes that this instance is the highest public version or not.
        /// </summary>
        public bool IsLastPublicVersion { get; private set; }
        /// <summary>
        /// Gets a value that describes that this instance is the highest version or not (draft or public).
        /// </summary>
        public bool IsLatestVersion { get; private set; }

        /// <summary>
        /// Gets the parent <see cref="Node"/>.
        /// Use this.ParentId, this.ParentPath, this.ParentName instead of Parent.Id, Parent.Path, Parent.Name if possible.
        /// </summary>
        /// <value>The parent.</value>
        public Node Parent
        {
            get
            {
                if (_data == null || _data.ParentId == 0)
                    return null;
                try
                {
                    return Node.LoadNode(_data.ParentId);
                }
                catch (Exception e) // rethrow
                {
                    throw Exception_ReferencedNodeCouldNotBeLoadedException("Parent", _data.ParentId, e);
                }
            }
        }
        /// <summary>
        /// Gets the Id of the parent <see cref="Node"/>.
        /// </summary>
        public int ParentId
        {
            get { return Data.ParentId; }
        }
        /// <summary>
        /// Gets the Path of the parent <see cref="Node"/>.
        /// </summary>
        public string ParentPath
        {
            get { return RepositoryPath.GetParentPath(this.Path); }
        }
        /// <summary>
        /// Gets the Name of the parent <see cref="Node"/>.
        /// </summary>
        public string ParentName
        {
            get { return RepositoryPath.GetFileName(this.ParentPath); }
        }
        /// <summary>
        /// Gets or sets the name of this <see cref="Node"/>.
        /// </summary>
        /// <remarks>
        /// A <see cref="Node"/> is uniquely identified by the Name within a container in the sensenet Content Repository tree.
        /// This guarantees that there can be no two <see cref="Node"/>s with the same path in the Content Repository.
        /// </remarks>
        public virtual string Name
        {
            get { return _data.Name; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                MakePrivateData();
                _data.Name = value;
            }

        }
        /// <summary>
        /// Gets or sets the human readable name of this <see cref="Node"/>. This value is not affected 
        /// by versioning (it is the same for all versions) and may contain any character.
        /// </summary>
        public virtual string DisplayName
        {
            get { return _data.DisplayName; }
            set
            {
                MakePrivateData();
                _data.DisplayName = value;
            }
        }
        /// <summary>
        /// Gets the path of this <see cref="Node"/>.
        /// </summary>
        public virtual string Path
        {
            get { return _data.Path; }
        }

        /// <summary>
        /// Gets the depth (count of parent folders) of the <see cref="Node"/> in the tree (Root = 0)
        /// </summary>
        public virtual int Depth
        {
            get { return GetDepth(_data.Path); }
        }
        /// <summary>
        /// Gets or sets the index.
        /// </summary>
        /// <value>The index.</value>
        public virtual int Index
        {
            get { return _data.Index; }
            set
            {
                MakePrivateData();
                _data.Index = value;
            }
        }
        /// <summary>
        /// Gets a value indicating whether this <see cref="Node"/> instance is modified.
        /// </summary>
        public bool IsModified
        {
            get { return this._data.AnyDataModified; }
        }
        /// <summary>
        /// Gets a value indicating whether this <see cref="Node"/> instance is deleted.
        /// </summary>
        public bool IsDeleted
        {
            get { return _data.IsDeleted; }
        }
        /// <summary>
        /// Gets a value indicating whether the permissions of this <see cref="Node"/> are inherited from its parent.
        /// </summary>
        public bool IsInherited
        {
            get { return Security.IsInherited; }
        }

        /// <summary>
        /// Gets the security handler for this <see cref="Node"/>. This is the entry point of all permission-related operations.
        /// </summary>
        /// <value>The security handler.</value>
        public SecurityHandler Security
        {
            get
            {
                if (_security == null)
                    _security = new SecurityHandler(this);
                return _security;
            }
        }
        /// <summary>
        /// Gets the lock handler.
        /// </summary>
        /// <value>The lock handler.</value>
        public LockHandler Lock
        {
            get
            {
                if (_lockHandler == null)
                    _lockHandler = new LockHandler(this);
                return _lockHandler;
            }
        }

        /// <summary>
        /// Gets the saving state of the node (e.g. whether it is under a multistep saving operation).
        /// </summary>
        public ContentSavingState SavingState
        {
            get { return _data.SavingState; }
            private set
            {
                MakePrivateData();
                _data.SavingState = value;
            }
        }
        internal IEnumerable<ChangedData> ChangedData
        {
            get { return _data.ChangedData == null ? Storage.ChangedData.EmptyArray : _data.ChangedData; }
            private set
            {
                MakePrivateData();
                _data.ChangedData = value;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="Node"/> is system Content or not.
        /// ContentQuery results do not contain system Content by default if the auto filtering is active.
        /// </summary>
        public bool IsSystem
        {
            get { return _data.IsSystem; }
            set
            {
                MakePrivateData();
                _data.IsSystem = value;
            }
        }

        /// <summary>
        /// Gets the timestamp of the <see cref="Node"/> as it was when it was loaded from the database.
        /// </summary>
        public long NodeTimestamp
        {
            get
            {
                return _data.NodeTimestamp;
            }
        }
        /// <summary>
        /// Gets the timestamp of this version of the <see cref="Node"/> as it was when it was loaded from the database.
        /// </summary>
        public long VersionTimestamp
        {
            get
            {
                return _data.VersionTimestamp;
            }
        }

        /// <summary>
        /// The <see cref="Node"/> is new if its Id is 0.
        /// </summary>
        public virtual bool IsNew
        {
            get
            {
                return this.Id == 0;
            }
        }

        #endregion
        #region // --------------------------------------------------------- Creation, Modification
        // --------------------------------------------------------- Node-level Creation, Modification

        /// <summary>
        /// Gets or sets the creation date of the first version. Writing is only allowed for a member of the Operators group.
        /// </summary>
        public virtual DateTime CreationDate
        {
            get { return _data.CreationDate; }
            set
            {
                AssertUserIsOperator("CreationDate");
                SetCreationDate(value);
            }
        }
        /// <summary>
        /// Checks if the current user is a system user or a member of the Operators group
        /// and throws a <see cref="NotSupportedException"/> if not.
        /// </summary>
        /// <param name="propertyName">The property that the caller tried to access. Used only when throwing an exception.</param>
        protected void AssertUserIsOperator(string propertyName) 
        {
            var user = AccessProvider.Current.GetCurrentUser();

            // there is no need for group check in elevated mode
            if (user is SystemUser)
                return;

            using (new SystemAccount())
            {
                if (!user.IsInGroup((IGroup)Node.LoadNode(Identifiers.OperatorsGroupPath)))
                    throw new NotSupportedException(String.Format(SR.Exceptions.General.Msg_CannotWriteReadOnlyProperty_1, propertyName));
            }
        }

        /// <summary>
        /// Sets the CreationDate of this node to the specified value.
        /// </summary>
        private void SetCreationDate(DateTime creation)
        {
            if (creation < DataStore.DateTimeMinValue)
                throw SR.Exceptions.General.Exc_LessThanDateTimeMinValue();
            if (creation > DataStore.DateTimeMaxValue)
                throw SR.Exceptions.General.Exc_BiggerThanDateTimeMaxValue();
            MakePrivateData();
            _data.CreationDate = creation;
        }

        /// <summary>
        /// Gets or sets the <see cref="Node"/> that represents the user who created the first version.
        /// </summary>
        public virtual Node CreatedBy
        {
            get
            {
                return (Node)LoadRefUserOrSomebody(CreatedById, "CreatedBy");
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");
                if (value.Id == 0)
                    throw new ArgumentOutOfRangeException("value", "Referenced 'CreatedBy' node must be saved.");
                if (value.Id == Identifiers.SomebodyUserId)
                    throw new SenseNetSecurityException("Cannot set the CreatedBy property with the Somebody user");
                if (!(value is IUser))
                    throw new ArgumentOutOfRangeException("value", "'CreatedBy' must be IUser.");

                MakePrivateData();
                _data.CreatedById = value.Id;
            }
        }
        /// <summary>
        /// Gets the user id who created the first version.
        /// </summary>
        public virtual int CreatedById
        {
            get { return _data.CreatedById; }
        }
        /// <summary>
        /// Gets or sets the modification date of the last version.
        /// </summary>
        public virtual DateTime ModificationDate
        {
            get { return _data.ModificationDate; }
            set
            {
                if (value < DataStore.DateTimeMinValue)
                    throw SR.Exceptions.General.Exc_LessThanDateTimeMinValue();
                if (value > DataStore.DateTimeMaxValue)
                    throw SR.Exceptions.General.Exc_BiggerThanDateTimeMaxValue();

                MakePrivateData();
                _data.ModificationDate = value;
            }
        }
        /// <summary>
        /// Gets or sets the <see cref="Node"/> that represents the user who modified the last version.
        /// </summary>
        public virtual Node ModifiedBy
        {
            get
            {
                return (Node)LoadRefUserOrSomebody(ModifiedById, "ModifiedBy");
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");
                if (value.Id == 0)
                    throw new ArgumentOutOfRangeException("value", "Referenced 'ModifiedBy' node must be saved.");
                if (value.Id == Identifiers.SomebodyUserId)
                    throw new SenseNetSecurityException("Cannot set the ModifiedBy property with the Somebody user");
                if (!(value is IUser))
                    throw new ArgumentOutOfRangeException("value", "'ModifiedBy' must be IUser.");

                MakePrivateData();
                _data.ModifiedById = value.Id;
            }
        }
        /// <summary>
        /// Gets the user id who modified the last version.
        /// </summary>
        public virtual int ModifiedById
        {
            get { return _data.ModifiedById; }
        }

        /// <summary>
        /// Gets the user id who is the owner of this <see cref="Node"/>.
        /// </summary>
        public virtual int OwnerId
        {
            get { return _data.OwnerId; }
        }

        /// <summary>
        /// Gets or sets the <see cref="Node"/> that represents the user who is the owner of this <see cref="Node"/>.
        /// </summary>
        public virtual Node Owner
        {
            get
            {
                return (Node)LoadRefUserOrSomebody(OwnerId, "Owner");
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");
                if (value.Id == 0)
                    throw new ArgumentOutOfRangeException("value", "Referenced 'Owner' node must be saved.");
                if (value.Id == Identifiers.SomebodyUserId)
                    throw new SenseNetSecurityException("Cannot set the Owner property with the Somebody user");
                if (value.Id == Identifiers.VisitorUserId)
                    throw new SenseNetSecurityException("The Visitor user cannot be owner of a content.");
                if (!(value is IUser))
                    throw new ArgumentOutOfRangeException("value", "'Owner' must be IUser.");

                MakePrivateData();
                _data.OwnerId = value.Id;
            }
        }

        // --------------------------------------------------------- Version-level Creation, Modification

        /// <summary>
        /// Gets or sets the creation date of this version. Writing is only allowed for a member of the Operators group.
        /// </summary>
        public virtual DateTime VersionCreationDate
        {
            get { return _data.VersionCreationDate; }
            set
            {
                AssertUserIsOperator("VersionCreationDate");
                SetVersionCreationDate(value);
            }
        }

        /// <summary>
        /// Sets the VersionCreationDate of this node version to the specified value.
        /// </summary>
        private void SetVersionCreationDate(DateTime creation)
        {
            if (creation < DataStore.DateTimeMinValue)
                throw SR.Exceptions.General.Exc_LessThanDateTimeMinValue();
            if (creation > DataStore.DateTimeMaxValue)
                throw SR.Exceptions.General.Exc_BiggerThanDateTimeMaxValue();
            MakePrivateData();
            _data.VersionCreationDate = creation;
        }

        /// <summary>
        /// Gets or sets the user who created this version.
        /// </summary>
        public virtual Node VersionCreatedBy
        {
            get
            {
                return (Node)LoadRefUserOrSomebody(VersionCreatedById, "VersionCreatedBy");
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");
                if (value.Id == 0)
                    throw new ArgumentOutOfRangeException("value", "Referenced 'CreatedBy' node must be saved.");
                if (value.Id == Identifiers.SomebodyUserId)
                    throw new SenseNetSecurityException("Cannot set the CreatedBy property with the Somebody user");
                if (!(value is IUser))
                    throw new ArgumentOutOfRangeException("value", "'CreatedBy' must be IUser.");

                MakePrivateData();
                _data.VersionCreatedById = value.Id;
            }
        }
        /// <summary>
        /// Gets the user id who created this version.
        /// </summary>
        public virtual int VersionCreatedById
        {
            get { return _data.VersionCreatedById; }
        }
        /// <summary>
        /// Gets or sets the modification date of this version.
        /// </summary>
        public virtual DateTime VersionModificationDate
        {
            get { return _data.VersionModificationDate; }
            set
            {
                if (value < DataStore.DateTimeMinValue)
                    throw SR.Exceptions.General.Exc_LessThanDateTimeMinValue();
                if (value > DataStore.DateTimeMaxValue)
                    throw SR.Exceptions.General.Exc_BiggerThanDateTimeMaxValue();

                MakePrivateData();
                _data.VersionModificationDate = value;
            }
        }
        /// <summary>
        /// Gets or sets the user who modified this version.
        /// </summary>
        public virtual Node VersionModifiedBy
        {
            get
            {
                return (Node)LoadRefUserOrSomebody(VersionModifiedById, "VersionModifiedBy");
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");
                if (value.Id == 0)
                    throw new ArgumentOutOfRangeException("value", "Referenced 'ModifiedBy' node must be saved.");
                if (value.Id == Identifiers.SomebodyUserId)
                    throw new SenseNetSecurityException("Cannot set the ModifiedBy property with the Somebody user");
                if (!(value is IUser))
                    throw new ArgumentOutOfRangeException("value", "'ModifiedBy' must be IUser.");

                MakePrivateData();
                _data.VersionModifiedById = value.Id;
            }
        }
        /// <summary>
        /// Gets the user id who modified this version.
        /// </summary>
        public virtual int VersionModifiedById
        {
            get { return _data.VersionModifiedById; }
        }

        #endregion
        #region // --------------------------------------------------------- Properties for locking

        /// <summary>
        /// Gets true if this <see cref="Node"/> is locked (i.e. the LockedById property is not null).
        /// </summary>
        public bool Locked
        {
            get { return LockedById != 0; }
        }
        /// <summary>
        /// Gets the id of the user who locks this <see cref="Node"/>.
        /// </summary>
        public int LockedById
        {
            get { return _data == null ? 0 : _data.LockedById; }
            internal set
            {
                MakePrivateData();
                _data.LockedById = value;
                _data.Locked = value != 0;
            }
        }
        /// <summary>
        /// Gets the user who locks this <see cref="Node"/>.
        /// </summary>
        public IUser LockedBy
        {
            get
            {
                if (!this.Locked)
                    return null;
                return LoadRefUserOrSomebody(LockedById, "LockedBy");
            }
        }
        /// <summary>
        /// Gets or sets the ETag value.
        /// </summary>
        public string ETag
        {
            get { return _data.ETag; }
            set
            {
                MakePrivateData();
                _data.ETag = value;
            }
        }
        /// <summary>
        /// Gets or sets a value that can be used in any locking mechanism.
        /// </summary>
        public int LockType
        {
            get { return _data.LockType; }
            set
            {
                MakePrivateData();
                _data.LockType = value;
            }
        }
        /// <summary>
        /// Gets the maximum lock time in seconds.
        /// </summary>
        public int LockTimeout
        {
            get { return _data.LockTimeout; }
            internal set
            {
                MakePrivateData();
                _data.LockTimeout = value;
            }
        }
        /// <summary>
        /// Gets or sets the date and time of the start of the locking.
        /// The lock can be kept alive if it is updated within the timeout.
        /// </summary>
        public DateTime LockDate
        {
            get { return _data.LockDate; }
            set
            {
                MakePrivateData();
                _data.LockDate = value;
            }
        }
        /// <summary>
        /// Gets an unique value that is generated at the start of the locking.
        /// Returns null or empty if tis <see cref="Node"/> is not locked.
        /// </summary>
        public string LockToken
        {
            get { return _data.LockToken; }
            internal set
            {
                MakePrivateData();
                _data.LockToken = value;
            }
        }
        /// <summary>
        /// Gets the date and time when the lock was refreshed.
        /// </summary>
        public DateTime LastLockUpdate
        {
            get { return _data.LastLockUpdate; }
            internal set
            {
                MakePrivateData();
                _data.LastLockUpdate = value;
            }
        }

        #endregion

        /// <summary>
        /// Returns the loaded value of the specified property. Modifications are ignored.
        /// </summary>
        public object GetStoredValue(string name)
        {
            var data = Data;
            if (!data.IsShared && data.SharedData != null)
                data = data.SharedData;
            switch (name)
            {
                case "Id": return data.Id;
                case "NodeTypeId": return data.NodeTypeId;
                case "ContentListId": return data.ContentListId;
                case "ContentListTypeId": return data.ContentListTypeId;
                case "ParentId": return data.ParentId;
                case "CreatingInProgress": return data.CreatingInProgress;
                case "Name": return data.Name;
                case "DisplayName": return data.DisplayName;
                case "Path": return data.Path;
                case "Index": return data.Index;
                case "IsInherited": return Security.IsInherited;
                case "VersionCreationDate": return data.VersionCreationDate;
                case "VersionModificationDate": return data.VersionModificationDate;
                case "VersionCreatedById": return data.VersionCreatedById;
                case "VersionModifiedById": return data.VersionModifiedById;
                case "Version": return data.Version;
                case "VersionId": return data.VersionId;
                case "CreationDate": return data.CreationDate;
                case "ModificationDate": return data.ModificationDate;
                case "CreatedById": return data.CreatedById;
                case "ModifiedById": return data.ModifiedById;
                case "Locked": return data.Locked;
                case "LockedById": return data.LockedById;
                case "ETag": return data.ETag;
                case "LockType": return data.LockType;
                case "LockTimeout": return data.LockTimeout;
                case "LockDate": return data.LockDate;
                case "LockToken": return data.LockToken;
                case "LastLockUpdate": return data.LastLockUpdate;
                case "SavingState": return data.SavingState;
                case "NodeTimestamp": return data.NodeTimestamp;
                case "VersionTimestamp": return data.VersionTimestamp;
                default: return data.GetDynamicRawData(GetPropertyTypeByName(name)); // this[GetPropertyTypeByName(name)];
            }
        }

        #region // ================================================================================================= Dynamic property accessors

        /// <summary>
        /// Gets or sets value of the specified property.
        /// </summary>
        public object this[string propertyName]
        {
            get
            {
                switch (propertyName)
                {
                    case "Id": return this.Id;
                    case "NodeType": return this.NodeType;
                    case "ContentListId": return this.ContentListId;
                    case "ContentListType": return this.ContentListType;
                    case "Parent": return this.Parent;
                    case "ParentId": return this._data.ParentId;
                    case "Name": return this.Name;
                    case "DisplayName": return this.DisplayName;
                    case "Path": return this.Path;
                    case "Index": return this.Index;
                    case "IsModified": return this.IsModified;
                    case "IsDeleted": return this.IsDeleted;
                    case "IsInherited": return this.IsInherited;
                    case "IsSystem": return this.IsSystem;
                    case "CreationDate": return this.CreationDate;
                    case "CreatedBy": return this.CreatedBy;
                    case "CreatedById": return this.CreatedById;
                    case "Owner": return this.Owner;
                    case "OwnerId": return this.OwnerId;
                    case "ModificationDate": return this.ModificationDate;
                    case "ModifiedBy": return this.ModifiedBy;
                    case "ModifiedById": return this.ModifiedById;
                    case "NodeTimestamp": return this.NodeTimestamp;
                    case "VersionTimestamp": return this.VersionTimestamp;
                    case "Version": return this.Version;
                    case "VersionId": return this.VersionId;
                    case "VersionCreationDate": return this.VersionCreationDate;
                    case "VersionCreatedBy": return this.VersionCreatedBy;
                    case "VersionCreatedById": return this.VersionCreatedById;
                    case "VersionModificationDate": return this.VersionModificationDate;
                    case "VersionModifiedBy": return this.VersionModifiedBy;
                    case "VersionModifiedById": return this.VersionModifiedById;
                    case "Locked": return this.Locked;
                    case "Lock": return this.Lock;
                    case "LockedById": return this.LockedById;
                    case "LockedBy": return this.LockedBy;
                    case "ETag": return this.ETag;
                    case "LockType": return this.LockType;
                    case "LockTimeout": return this.LockTimeout;
                    case "LockDate": return this.LockDate;
                    case "LockToken": return this.LockToken;
                    case "LastLockUpdate": return this.LastLockUpdate;
                    case "SavingState": return this.SavingState;
                    case "Security": return this.Security;
                    default: return this[GetPropertyTypeByName(propertyName)];
                }
            }
            set
            {
                switch (propertyName)
                {
                    case "Id":
                    case "IsModified":
                    case "NodeType":
                    case "Parent":
                    case "Path":
                    case "Security":
                        throw new InvalidOperationException(String.Concat("Property is read only: ", propertyName));
                    case "CreationDate": this.CreationDate = (DateTime)value; break;
                    case "VersionCreationDate": this.VersionCreationDate = (DateTime)value; break;
                    case "CreatedBy": this.CreatedBy = (Node)value; break;
                    case "Owner": this.Owner = (Node)value; break;
                    case "VersionCreatedBy": this.VersionCreatedBy = (Node)value; break;
                    case "Index": this.Index = value == null ? 0 : (int)value; break;
                    case "ModificationDate": this.ModificationDate = (DateTime)value; break;
                    case "ModifiedBy": this.ModifiedBy = (Node)value; break;
                    case "VersionModificationDate": this.VersionModificationDate = (DateTime)value; break;
                    case "VersionModifiedBy": this.VersionModifiedBy = (Node)value; break;
                    case "Name": this.Name = (string)value; break;
                    case "DisplayName": this.DisplayName = (string)value; break;
                    case "Version": this.Version = (VersionNumber)value; break;
                    case "SavingState": this.SavingState = (ContentSavingState)value; break;
                    default: this[GetPropertyTypeByName(propertyName)] = value; break;
                }
            }
        }
        internal object this[int propertyId]
        {
            get { return this[GetPropertyTypeById(propertyId)]; }
            set { this[GetPropertyTypeById(propertyId)] = value; }
        }
        /// <summary>
        /// Gets or sets the value of the specified property.
        /// </summary>
        public object this[PropertyType propertyType]
        {
            get
            {
                AssertSeeOnly(propertyType);

                switch (propertyType.DataType)
                {
                    case DataType.Binary:
                        if (this.SavingState != ContentSavingState.Finalized)
                            throw new InvalidOperationException(SR.GetString(SR.Exceptions.General.Error_AccessToNotFinalizedBinary_2, this.Path, propertyType.Name));
                        return GetAccessor(propertyType);
                    case DataType.Reference:
                        return GetAccessor(propertyType);

                    default:
                        return _data.GetDynamicRawData(propertyType) ?? propertyType.DefaultValue;
                }
            }
            set
            {
                MakePrivateData();
                switch (propertyType.DataType)
                {
                    case DataType.Binary:
                        ChangeAccessor((BinaryData)value, propertyType);
                        break;
                    case DataType.Reference:
                        var nodeList = value as NodeList<Node>;
                        if (nodeList == null)
                            nodeList = value == null ? new NodeList<Node>() : new NodeList<Node>((IEnumerable<Node>)value);
                        ChangeAccessor(nodeList, propertyType);
                        break;
                    default:
                        _data.SetDynamicRawData(propertyType, value);
                        break;
                }

            }
        }

        private static readonly List<string> _propertyNames = new List<string>(new[] {
            "Id","NodeType","ContentListId","ContentListType","Parent","ParentId","Name","DisplayName","Path",
            "Index","IsModified","IsDeleted","IsInherited","IsSystem","CreationDate","CreatedBy","CreatedById","ModificationDate","ModifiedBy","ModifiedById", "Owner", "OwnerId",
            "Version","VersionId","VersionCreationDate","VersionCreatedBy","VersionCreatedById","VersionModificationDate","VersionModifiedBy","VersionModifiedById",
            "Locked","Lock","LockedById","LockedBy","ETag","LockType","LockTimeout","LockDate","LockToken","LastLockUpdate","Security",
            "SavingState", "ChangedData"});

        /// <summary>
        /// Gets the <see cref="PropertyType"/> collection of this <see cref="Node"/>.
        /// This is the dynamic signature of this <see cref="Node"/>.
        /// </summary>
        public TypeCollection<PropertyType> PropertyTypes { get { return _data.PropertyTypes; } }
        /// <summary>
        /// Returns true if this <see cref="Node"/> supports the property by the given name.
        /// </summary>
        public virtual bool HasProperty(string name)
        {
            if (PropertyTypes[name] != null)
                return true;
            return _propertyNames.Contains(name);
        }
        /// <summary>
        /// Returns true if this <see cref="Node"/> supports the given property.
        /// </summary>
        public bool HasProperty(PropertyType propType)
        {
            return HasProperty(propType.Id);
        }
        /// <summary>
        /// Returns true if this <see cref="Node"/> supports the property by the given property type id.
        /// </summary>
        public bool HasProperty(int propertyTypeId)
        {
            return PropertyTypes.GetItemById(propertyTypeId) != null;
        }

        private Dictionary<string, IDynamicDataAccessor> __accessors;
        private Dictionary<string, IDynamicDataAccessor> Accessors
        {
            get
            {
                if (__accessors == null)
                {
                    var accDict = new Dictionary<string, IDynamicDataAccessor>();

                    foreach (var propType in this.PropertyTypes)
                    {
                        IDynamicDataAccessor acc = null;
                        if (propType.DataType == DataType.Binary)
                            acc = new BinaryData();
                        if (propType.DataType == DataType.Reference)
                            acc = new NodeList<Node>();
                        if (acc == null)
                            continue;
                        acc.OwnerNode = this;
                        acc.PropertyType = propType;
                        accDict[propType.Name] = acc;
                    }

                    __accessors = accDict;
                }
                return __accessors;
            }
        }

        // ---------------- General axis

        /// <summary>
        /// Returns the value of the specified property converted to the desired type.
        /// An <see cref="InvalidCastException"/> will be thrown if the type parameter does not match 
        /// the type of the requested data.
        /// </summary>
        public T GetProperty<T>(string propertyName)
        {
            return (T)this[propertyName];
        }
        internal T GetProperty<T>(int propertyId)
        {
            return (T)this[propertyId];
        }
        /// <summary>
        /// Returns the value of the specified property converted to the desired type.
        /// An <see cref="InvalidCastException"/> will be thrown if the type parameter does not match 
        /// the type of the requested data.
        /// </summary>
        public T GetProperty<T>(PropertyType propertyType)
        {
            return (T)this[propertyType];
        }
        /// <summary>
        /// Returns the value of the specified property.
        /// The value is null if the requested property does not exist.
        /// </summary>
        public virtual object GetPropertySafely(string propertyName)
        {
            if (this.HasProperty(propertyName))
            {
                var result = this[propertyName];
                return result;
            }
            return null;
        }

        private IDynamicDataAccessor GetAccessor(PropertyType propType)
        {
            IDynamicDataAccessor value;
            if (!Accessors.TryGetValue(propType.Name, out value))
                throw NodeData.Exception_PropertyNotFound(propType.Name, this.NodeType.Name);
            return value;
        }
        private PropertyType GetPropertyTypeByName(string name)
        {
            var propType = PropertyTypes[name];
            if (propType == null)
                throw NodeData.Exception_PropertyNotFound(name, this.NodeType.Name);
            return propType;
        }
        private PropertyType GetPropertyTypeById(int id)
        {
            var propType = PropertyTypes.GetItemById(id);
            if (propType == null)
                throw NodeData.Exception_PropertyNotFound(id);
            return propType;
        }
        internal void ChangeAccessor(IDynamicDataAccessor newAcc, PropertyType propType)
        {
            var value = _data.GetDynamicRawData(propType);
            if (value != null)
            {
                var oldAcc = Accessors[propType.Name];
                if (oldAcc == null)
                    throw new NullReferenceException("Accessor not found: " + propType.Name);
                oldAcc.RawData = value;
                oldAcc.OwnerNode = null;
                oldAcc.PropertyType = null;
            }

            MakePrivateData();
            if (newAcc == null)
            {
                _data.SetDynamicRawData(propType, null);
            }
            else
            {
                _data.SetDynamicRawData(propType, newAcc.RawData);
                newAcc.OwnerNode = this;
                newAcc.PropertyType = propType;
                if (Accessors.ContainsKey(propType.Name))
                    Accessors[propType.Name] = newAcc;
                else
                    Accessors.Add(propType.Name, newAcc);
            }
        }

        // ---------------- Binary axis

        /// <summary>
        /// Returns the <see cref="BinaryData"/> property of this <see cref="Node"/> by the given name.
        /// </summary>
        public BinaryData GetBinary(string propertyName)
        {
            return (BinaryData)this[propertyName];
        }
        internal BinaryData GetBinary(int propertyId)
        {
            return (BinaryData)this[propertyId];
        }
        /// <summary>
        /// Returns the <see cref="BinaryData"/> property of this <see cref="Node"/> by the given <see cref="PropertyType"/>.
        /// </summary>
        public BinaryData GetBinary(PropertyType property)
        {
            return (BinaryData)this[property];
        }
        /// <summary>
        /// Assigns the given <see cref="BinaryData"/> value to the specified property of this <see cref="Node"/>.
        /// </summary>
        public void SetBinary(string propertyName, BinaryData data)
        {
            if (data == null)
                GetBinary(propertyName).Reset();
            else
                GetBinary(propertyName).CopyFrom(data);
        }

        // ---------------- Reference axes

        /// <summary>
        /// Returns the collection of the referenced <see cref="Node"/>s from the property identified by the given name.
        /// The return value is always a collection even if the requested property is a single reference.
        /// In that case the collection contains only one item.
        /// </summary>
        public IEnumerable<Node> GetReferences(string propertyName)
        {
            return (IEnumerable<Node>)this[propertyName];
        }
        internal IEnumerable<Node> GetReferences(int propertyId)
        {
            return (IEnumerable<Node>)this[propertyId];
        }
        /// <summary>
        /// Returns the collection of the referenced <see cref="Node"/>s from the property identified by the given <see cref="PropertyType"/>.
        /// The return value is always a collection even if the requested property is a single reference.
        /// In that case the collection contains only one item.
        /// </summary>
        public IEnumerable<Node> GetReferences(PropertyType property)
        {
            return (IEnumerable<Node>)this[property];
        }

        /// <summary>
        /// Assigns the specified collection to the reference property of this <see cref="Node"/>.
        /// The property is identified by the given name.
        /// </summary>
        /// <typeparam name="T">Node or any inherited type.</typeparam>
        public void SetReferences<T>(string propertyName, IEnumerable<T> nodes) where T : Node
        {
            ClearReference(propertyName);
            AddReferences(propertyName, nodes);
        }
        internal void SetReferences<T>(int propertyId, IEnumerable<T> nodes) where T : Node
        {
            ClearReference(propertyId);
            AddReferences(propertyId, nodes);
        }
        /// <summary>
        /// Assigns the specified collection to the reference property of this <see cref="Node"/>.
        /// The property is identified by the given <see cref="PropertyType"/>.
        /// </summary>
        /// <typeparam name="T">Node or any inherited type.</typeparam>
        public void SetReferences<T>(PropertyType property, IEnumerable<T> nodes) where T : Node
        {
            ClearReference(property);
            AddReferences(property, nodes);
        }

        /// <summary>
        /// Clears the value of the reference property that is identified by the given name.
        /// </summary>
        public void ClearReference(string propertyName)
        {
            GetNodeList(propertyName).Clear();
        }
        internal void ClearReference(int propertyId)
        {
            GetNodeList(propertyId).Clear();
        }
        /// <summary>
        /// Clears the value of the reference property that is identified by the given <see cref="PropertyType"/>.
        /// </summary>
        public void ClearReference(PropertyType property)
        {
            GetNodeList(property).Clear();
        }

        /// <summary>
        /// Adds the given <see cref="Node"/> to the reference property that is identified by the given name.
        /// Duplicates are allowed.
        /// </summary>
        public void AddReference(string propertyName, Node refNode)
        {
            GetNodeList(propertyName).Add(refNode);
        }
        internal void AddReference(int propertyId, Node refNode)
        {
            GetNodeList(propertyId).Add(refNode);
        }
        /// <summary>
        /// Adds the given <see cref="Node"/> to the reference property that is identified by the given <see cref="PropertyType"/>.
        /// Duplicates are allowed.
        /// </summary>
        public void AddReference(PropertyType property, Node refNode)
        {
            GetNodeList(property).Add(refNode);
        }

        /// <summary>
        /// Adds the given <see cref="Node"/>s to the reference property that is identified by the given name.
        /// Duplicates are allowed.
        /// </summary>
        /// <typeparam name="T">Node or any inherited type.</typeparam>
        public void AddReferences<T>(string propertyName, IEnumerable<T> refNodes) where T : Node
        {
            AddReferences<T>(propertyName, refNodes, false);
        }
        internal void AddReferences<T>(int propertyId, IEnumerable<T> refNodes) where T : Node
        {
            AddReferences<T>(propertyId, refNodes, false);
        }
        /// <summary>
        /// Adds the given <see cref="Node"/>s to the reference property that is identified by the given <see cref="PropertyType"/>.
        /// Duplicates are allowed.
        /// </summary>
        /// <typeparam name="T">Node or any inherited type.</typeparam>
        public void AddReferences<T>(PropertyType property, IEnumerable<T> refNodes) where T : Node
        {
            AddReferences<T>(property, refNodes, false);
        }
        /// <summary>
        /// Adds the given <see cref="Node"/>s to the reference property that is identified by the given name.
        /// If the "distinct" parameter is true, the duplicates will be filtered.
        /// </summary>
        /// <typeparam name="T">Node or any inherited type.</typeparam>
        public void AddReferences<T>(string propertyName, IEnumerable<T> refNodes, bool distinct) where T : Node
        {
            AddReferences<T>(GetNodeList(propertyName), refNodes, distinct);
        }
        internal void AddReferences<T>(int propertyId, IEnumerable<T> refNodes, bool distinct) where T : Node
        {
            AddReferences<T>(GetNodeList(propertyId), refNodes, distinct);
        }
        /// <summary>
        /// Adds the given <see cref="Node"/>s to the reference property that is identified by the given <see cref="PropertyType"/>.
        /// If the "distinct" parameter is true, the duplicates will be filtered.
        /// </summary>
        /// <typeparam name="T">Node or any inherited type.</typeparam>
        public void AddReferences<T>(PropertyType property, IEnumerable<T> refNodes, bool distinct) where T : Node
        {
            AddReferences<T>(GetNodeList(property), refNodes, distinct);
        }

        /// <summary>
        /// Returns true if the given <see cref="Node"/> is referenced by the specified property.
        /// </summary>
        public bool HasReference(string propertyName, Node refNode)
        {
            return GetNodeList(propertyName).Contains(refNode);
        }
        internal bool HasReference(int propertyId, Node refNode)
        {
            return GetNodeList(propertyId).Contains(refNode);
        }
        /// <summary>
        /// Returns true if the given <see cref="Node"/> is referenced by the specified property.
        /// </summary>
        public bool HasReference(PropertyType property, Node refNode)
        {
            return GetNodeList(property).Contains(refNode);
        }

        /// <summary>
        /// Removes the given <see cref="Node"/> from the specified property.
        /// If the reference does not exist, does nothing.
        /// </summary>
        public void RemoveReference(string propertyName, Node refNode)
        {
            GetNodeList(propertyName).Remove(refNode);
        }
        internal void RemoveReference(int propertyId, Node refNode)
        {
            GetNodeList(propertyId).Remove(refNode);
        }
        /// <summary>
        /// Removes the given <see cref="Node"/> from the specified property.
        /// If the reference does not exist, does nothing.
        /// </summary>
        public void RemoveReference(PropertyType property, Node refNode)
        {
            GetNodeList(property).Remove(refNode);
        }

        /// <summary>
        /// Returns the count of referenced <see cref="Node"/>s in the specified property.
        /// </summary>
        public int GetReferenceCount(string propertyName)
        {
            return GetNodeList(propertyName).Count;
        }
        internal int GetReferenceCount(int propertyId)
        {
            return GetNodeList(propertyId).Count;
        }
        /// <summary>
        /// Returns the count of referenced <see cref="Node"/>s in the specified property.
        /// </summary>
        public int GetReferenceCount(PropertyType property)
        {
            return GetNodeList(property).Count;
        }

        // ---------------- Single reference interface

        /// <summary>
        /// Returns the referred <see cref="Node"/> from the specified property.
        /// If the property has more than one referred <see cref="Node"/>s, returns the first one.
        /// </summary>
        /// <typeparam name="T">the desired type that can be Node or any inherited type.</typeparam>
        public T GetReference<T>(string propertyName) where T : Node
        {
            return GetNodeList(propertyName).GetSingleValue<T>();
        }
        internal T GetReference<T>(int propertyId) where T : Node
        {
            return GetNodeList(propertyId).GetSingleValue<T>();
        }
        /// <summary>
        /// Returns the referred <see cref="Node"/> from the specified property.
        /// If the property has more than one referred <see cref="Node"/>s, returns the first one.
        /// </summary>
        /// <typeparam name="T">the desired type that can be Node or any inherited type.</typeparam>
        public T GetReference<T>(PropertyType property) where T : Node
        {
            return GetNodeList(property).GetSingleValue<T>();
        }
        /// <summary>
        /// Assigns the given <see cref="Node"/> to the specified reference property.
        /// </summary>
        public void SetReference(string propertyName, Node node)
        {
            GetNodeList(propertyName).SetSingleValue<Node>(node);
        }
        internal void SetReference(int propertyId, Node node)
        {
            GetNodeList(propertyId).SetSingleValue<Node>(node);
        }
        /// <summary>
        /// Assigns the given <see cref="Node"/> to the specified reference property.
        /// </summary>
        public void SetReference(PropertyType property, Node node)
        {
            GetNodeList(property).SetSingleValue<Node>(node);
        }

        // ---------------- reference tools

        private NodeList<Node> GetNodeList(string propertyName)
        {
            return (NodeList<Node>)this[propertyName];
        }
        private NodeList<Node> GetNodeList(int propertyId)
        {
            return (NodeList<Node>)this[propertyId];
        }
        private NodeList<Node> GetNodeList(PropertyType property)
        {
            return (NodeList<Node>)this[property];
        }

        private static void AddReferences<T>(NodeList<Node> nodeList, IEnumerable<T> refNodes, bool distinct) where T : Node
        {
            if (refNodes == null)
                return;

            foreach (var node in refNodes)
                if (!distinct || !nodeList.Contains(node))
                    nodeList.Add(node);
        }

        #endregion


        #region // ================================================================================================= Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="Node"/> class.
        /// Do not use or override this constructor directly from your code.
        /// </summary>
        protected Node() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="Node"/> class.
        /// </summary>
        /// <param name="parent">The parent.</param>
        protected Node(Node parent) : this(parent, null) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="Node"/> class.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="nodeTypeName">Name of the <see cref="Schema.NodeType"/>.</param>
        protected Node(Node parent, string nodeTypeName)
        {
            if (nodeTypeName == null)
                nodeTypeName = this.GetType().Name;

            var nodeType = NodeTypeManager.Current.NodeTypes[nodeTypeName];
            if (nodeType == null)
            {
                nodeTypeName = this.GetType().FullName;
                nodeType = NodeTypeManager.Current.NodeTypes[nodeTypeName];

                if (nodeType == null)
                    throw new RegistrationException(String.Concat(SR.Exceptions.Schema.Msg_UnknownNodeType, ": ", nodeTypeName));
            }

            int listId = 0;
            ContentListType listType = null;
            if (parent != null && !nodeType.IsInstaceOfOrDerivedFrom("SystemFolder"))
            {
                listId = (parent.ContentListType != null && parent.ContentListId == 0) ? parent.Id : parent.ContentListId;
                listType = parent.ContentListType;
            }
            if (listType != null && this is IContentList)
            {
                throw new ApplicationException("Cannot create a ContentList under another ContentList");
            }

            var data = DataStore.CreateNewNodeData(parent, nodeType, listType, listId);
            SetNodeData(data);

        }
        /// <summary>
        /// Initializes a new instance of the <see cref="Node"/> class in the loading procedure. Do not use this constructor directly from your code.
        /// </summary>
        /// <param name="token">The token.</param>
        protected Node(NodeToken token)
        {
            // caller: CreateTargetClass()
            if (token == null)
                throw new ArgumentNullException("token");
            string typeName = this.GetType().FullName;
            if (token.NodeType.ClassName != typeName)
            {
                var message = String.Concat("Cannot create a ", typeName, " instance because type name is different in the passed token: ", token.NodeType.ClassName);
                throw new RegistrationException(message);
            }
            FillData(this, token);
            SetVersionInfo(token.NodeHead);
        }

        #endregion

        /// <summary>
        /// Refreshes the IsLastPublicVersion and IsLastVersion property values.
        /// </summary>
        [Obsolete("This method will be deleted in the future.")]
        public void RefreshVersionInfo()
        {
            SetVersionInfo(NodeHead.Get(this.Id));
        }
        internal void RefreshVersionInfo(NodeHead nodeHead)
        {
            SetVersionInfo(nodeHead);
        }
        private void SetVersionInfo(NodeHead nodeHead)
        {
            var versionId = this.VersionId;
            this.IsLastPublicVersion = nodeHead.LastMajorVersionId == versionId && this.Version.Status == VersionStatus.Approved;
            this.IsLatestVersion = nodeHead.LastMinorVersionId == versionId;
        }

        #region // ================================================================================================= Loader methods

        private static VersionNumber DefaultAbstractVersion { get { return VersionNumber.LastAccessible; } }



        // ----------------------------------------------------------------------------- Static batch loaders

        /// <summary>
        /// Loads <see cref="Node"/> instances by the provided ids. The current user has to have
        /// at least See permission for each item in the result set.
        /// </summary>
        /// <param name="idArray">The IEnumerable&lt;int&gt; that contains the requested ids.</param>
        public static List<Node> LoadNodes(IEnumerable<int> idArray)
        {
            return LoadNodes(DataStore.LoadNodeHeadsAsync(idArray, CancellationToken.None).GetAwaiter().GetResult(),
                VersionNumber.LastAccessible);
        }
        /// <summary>
        /// Loads <see cref="Node"/> instances by the provided ids. The current user has to have
        /// at least See permission for each item in the result set.
        /// </summary>
        /// <param name="idArray">The IEnumerable&lt;int&gt; that contains the requested ids.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        public static async Task<List<Node>> LoadNodesAsync(IEnumerable<int> idArray, CancellationToken cancellationToken)
        {
            var nodeHeads = await DataStore.LoadNodeHeadsAsync(idArray, cancellationToken).ConfigureAwait(false);
            return await LoadNodesAsync(nodeHeads, VersionNumber.LastAccessible, cancellationToken).ConfigureAwait(false);
        }
        private static List<Node> LoadNodes(IEnumerable<NodeHead> heads, VersionNumber version)
        {
            var headList = new List<NodeHead>();
            var versionIdList = new List<int>();
            var headonlyList = new List<int>();

            ResolveNodeHeadsAndVersionIds(heads, version, headList, versionIdList, headonlyList);

            // loading data
            var result = new List<Node>();
            var tokenArray = DataStore.LoadNodesAsync(headList.ToArray(), versionIdList.ToArray(), CancellationToken.None)
                .GetAwaiter().GetResult();
            for (int i = 0; i < tokenArray.Length; i++)
            {
                var token = tokenArray[i];
                var retry = 0;
                var isHeadOnly = headonlyList.Contains(token.NodeId);
                while (true)
                {
                    if (token.NodeData != null)
                    {
                        Node node;

                        try
                        {
                            node = CreateTargetClass(token);
                        }
                        catch (ApplicationException)
                        {
                            // Could not create an instance of the target class.
                            SnTrace.Repository.WriteError($"Could not create an instance of {token.NodeType.Name} " +
                                                          $"with class {token.NodeType.ClassName}. NodeId: {token.NodeId}, " +
                                                          $"Path: {token.NodeHead.Path}");
                            break;
                        }

                        if (isHeadOnly)
                        {
                            // if the user has Preview permissions, that means a broader access than headonly
                            if (PreviewProvider.HasPreviewPermission(headList.FirstOrDefault(h => h.Id == token.NodeId)))
                                node.IsPreviewOnly = true;
                            else
                                node.IsHeadOnly = true;
                        }

                        result.Add(node);
                        break;
                    }
                    else
                    {
                        // retrying with reload nodehead
                        if (++retry > 1) // one time
                            break;

                        SnTrace.Repository.Write("Version is lost. VersionId:{0}, path:{1}", token.VersionId, token.NodeHead.Path);
                        var head = NodeHead.Get(token.NodeHead.Id);
                        if (head == null) // deleted
                            break;

                        AccessLevel userAccessLevel;
                        try
                        {
                            userAccessLevel = GetUserAccessLevel(head);
                        }
                        catch (SenseNet.Security.SecurityStructureException) // deleted
                        {
                            break;
                        }

                        var acceptedLevel = GetAcceptedLevel(userAccessLevel, version);
                        if (acceptedLevel == AccessLevel.Header)
                            isHeadOnly = true;
                        var versionId = GetVersionId(head, acceptedLevel, version);
                        token =  DataStore.LoadNodeAsync(head, versionId, CancellationToken.None).GetAwaiter().GetResult();
                    }
                }
            }
            return result;
        }
        private static async Task<List<Node>> LoadNodesAsync(IEnumerable<NodeHead> heads, VersionNumber version, CancellationToken cancellationToken)
        {
            var headList = new List<NodeHead>();
            var versionIdList = new List<int>();
            var headonlyList = new List<int>();

            ResolveNodeHeadsAndVersionIds(heads, version, headList, versionIdList, headonlyList);
            
            // loading data
            var result = new List<Node>();
            var tokenArray = await DataStore.LoadNodesAsync(headList.ToArray(), versionIdList.ToArray(), cancellationToken).ConfigureAwait(false);

            for (var i = 0; i < tokenArray.Length; i++)
            {
                var token = tokenArray[i];
                var retry = 0;
                var isHeadOnly = headonlyList.Contains(token.NodeId);
                while (true)
                {
                    if (token.NodeData != null)
                    {
                        Node node;

                        try
                        {
                            node = CreateTargetClass(token);
                        }
                        catch (ApplicationException)
                        {
                            // Could not create an instance of the target class.
                            SnTrace.Repository.WriteError($"Could not create an instance of {token.NodeType.Name} " +
                                                          $"with class {token.NodeType.ClassName}. NodeId: {token.NodeId}, " +
                                                          $"Path: {token.NodeHead.Path}");
                            break;
                        }

                        if (isHeadOnly)
                        {
                            // if the user has Preview permissions, that means a broader access than headonly
                            if (PreviewProvider.HasPreviewPermission(headList.FirstOrDefault(h => h.Id == token.NodeId)))
                                node.IsPreviewOnly = true;
                            else
                                node.IsHeadOnly = true;
                        }

                        result.Add(node);
                        break;
                    }
                    else
                    {
                        // retrying with reload nodehead
                        if (++retry > 1) // one time
                            break;

                        SnTrace.Repository.Write("Version is lost. VersionId:{0}, path:{1}", token.VersionId, token.NodeHead.Path);
                        var head = await NodeHead.GetAsync(token.NodeHead.Id, cancellationToken).ConfigureAwait(false);
                        if (head == null) // deleted
                            break;

                        AccessLevel userAccessLevel;
                        try
                        {
                            userAccessLevel = GetUserAccessLevel(head);
                        }
                        catch (SecurityStructureException) // deleted
                        {
                            break;
                        }

                        var acceptedLevel = GetAcceptedLevel(userAccessLevel, version);
                        if (acceptedLevel == AccessLevel.Header)
                            isHeadOnly = true;
                        var versionId = GetVersionId(head, acceptedLevel, version);
                        token = await DataStore.LoadNodeAsync(head, versionId, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            return result;
        }
        private static void ResolveNodeHeadsAndVersionIds(IEnumerable<NodeHead> heads, VersionNumber version,
            List<NodeHead> headList, List<int> versionIdList, List<int> headonlyList)
        {
            // resolving versionid array
            foreach (var head in heads)
            {
                if (head == null)
                    continue;

                AccessLevel userAccessLevel;

                try
                {
                    userAccessLevel = GetUserAccessLevel(head);
                }
                catch (SecurityStructureException)
                {
                    // skip the non-existent item
                    continue;
                }
                catch (AccessDeniedException)
                {
                    // the user does not have permission to see/open this node
                    continue;
                }
                catch (SenseNetSecurityException)
                {
                    // the user does not have permission to see/open this node
                    continue;
                }

                var acceptedLevel = GetAcceptedLevel(userAccessLevel, version);
                if (acceptedLevel == AccessLevel.Header)
                    headonlyList.Add(head.Id);

                var versionId = GetVersionId(head, acceptedLevel, version);

                // if user has not enough permissions, skip the node
                if (versionId <= 0)
                    continue;

                headList.Add(head);
                versionIdList.Add(versionId);
            }
        }

        /// <summary>
        /// Loads multiple <see cref="Node"/>s as a batch operation using an identifier list.
        /// </summary>
        /// <param name="identifiers">A list of identifiers (represented by either a path or an id).</param>
        public static IEnumerable<Node> LoadNodes(IEnumerable<NodeIdentifier> identifiers)
        {
            return LoadNodes(identifiers.Where(identifier => identifier != null).Select(identifier =>
            {
                if (identifier.Path != null)
                {
                    var head = NodeHead.Get(identifier.Path);
                    return head?.Id ?? 0;
                }

                return identifier.Id;
            }));
        }

        // ----------------------------------------------------------------------------- Static single loaders

        /// <summary>
        /// Loads a <see cref="Node"/> by the provided id.
        /// </summary>
        /// <typeparam name="T">The requested return type.</typeparam>
        /// <param name="nodeId">Node id.</param>
        /// <returns>A node as the provided derived type.</returns>
        public static T Load<T>(int nodeId) where T : Node
        {
            return (T)LoadNode(nodeId);
        }
        /// <summary>
        /// Loads a <see cref="Node"/> by the provided id.
        /// </summary>
        /// <typeparam name="T">The requested return type.</typeparam>
        /// <param name="nodeId">Node id.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A node as the provided derived type.</returns>
        public static async Task<T> LoadAsync<T>(int nodeId, CancellationToken cancellationToken) where T : Node
        {
            return (T) await LoadNodeAsync(nodeId, cancellationToken).ConfigureAwait(false);
        }
        /// <summary>
        /// Loads a <see cref="Node"/> by the provided id and version.
        /// </summary>
        /// <typeparam name="T">The requested return type.</typeparam>
        /// <param name="nodeId">Node id.</param>
        /// <param name="version">The requested version. For example: new VersionNumber(2, 0).</param>
        /// <returns>A node as the provided derived type.</returns>
        public static T Load<T>(int nodeId, VersionNumber version) where T : Node
        {
            return (T)LoadNode(nodeId, version);
        }
        /// <summary>
        /// Loads a <see cref="Node"/> by the provided id and version.
        /// </summary>
        /// <typeparam name="T">The requested return type.</typeparam>
        /// <param name="nodeId">Node id.</param>
        /// <param name="version">The requested version. For example: new VersionNumber(2, 0).</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A node as the provided derived type.</returns>
        public static async Task<T> LoadAsync<T>(int nodeId, VersionNumber version, CancellationToken cancellationToken) where T : Node
        {
            return (T) await LoadNodeAsync(nodeId, version, cancellationToken).ConfigureAwait(false);
        }
        /// <summary>
        /// Loads a <see cref="Node"/> by the provided path.
        /// </summary>
        /// <typeparam name="T">The requested return type.</typeparam>
        /// <param name="path">Node path.</param>
        /// <returns>A node as the provided derived type.</returns>
        public static T Load<T>(string path) where T : Node
        {
            return (T)LoadNode(path);
        }
        /// <summary>
        /// Loads a <see cref="Node"/> by the provided path.
        /// </summary>
        /// <typeparam name="T">The requested return type.</typeparam>
        /// <param name="path">Node path.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A node as the provided derived type.</returns>
        public static async Task<T> LoadAsync<T>(string path, CancellationToken cancellationToken) where T : Node
        {
            return (T) await LoadNodeAsync(path, cancellationToken).ConfigureAwait(false);
        }
        /// <summary>
        /// Loads a <see cref="Node"/> by the provided path and version.
        /// </summary>
        /// <typeparam name="T">The requested return type.</typeparam>
        /// <param name="path">Node path.</param>
        /// <param name="version">The requested version. For example: new VersionNumber(2, 0).</param>
        /// <returns>A node as the provided derived type.</returns>
        public static T Load<T>(string path, VersionNumber version) where T : Node
        {
            return (T)LoadNode(path, version);
        }

        /// <summary>
        /// Loads a <see cref="Node"/> by the provided path and version.
        /// </summary>
        /// <typeparam name="T">The requested return type.</typeparam>
        /// <param name="path">Node path.</param>
        /// <param name="version">The requested version. For example: new VersionNumber(2, 0).</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A node as the provided derived type.</returns>
        public static async Task<T> LoadAsync<T>(string path, VersionNumber version, CancellationToken cancellationToken) where T : Node
        {
            return (T) await LoadNodeAsync(path, version, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Loads a <see cref="Node"/> by the provided path.
        /// </summary>
        /// <example>How to load a <see cref="Node"/> by passing the sensenet Content Repository path.
        /// In this case you will get a <see cref="Node"/> named "node" filled with the data of the latest 
        /// version of <see cref="Node"/> /Root/MyFavoriteNode.
        /// <code>
        /// Node node = Node.LoadNode("/Root/MyFavoriteNode");
        /// </code>
        /// </example>
        /// <param name="path">Node path.</param>
        /// <returns>The latest accessible version of the <see cref="Node"/> that has the given path, or null.</returns>
        public static Node LoadNode(string path)
        {
            return LoadNode(path, DefaultAbstractVersion);
        }
        /// <summary>
        /// Loads a <see cref="Node"/> by the provided path.
        /// </summary>
        /// <example>How to load a <see cref="Node"/> by passing the sensenet Content Repository path.
        /// In this case you will get a <see cref="Node"/> named "node" filled with the data of the latest 
        /// version of <see cref="Node"/> /Root/MyFavoriteNode.
        /// <code>
        /// Node node = Node.LoadNode("/Root/MyFavoriteNode");
        /// </code>
        /// </example>
        /// <param name="path">Node path.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The latest accessible version of the <see cref="Node"/> that has the given path, or null.</returns>
        public static Task<Node> LoadNodeAsync(string path, CancellationToken cancellationToken)
        {
            return LoadNodeAsync(path, DefaultAbstractVersion, cancellationToken);
        }
        /// <summary>
        /// Loads a <see cref="Node"/> by the provided path and version.
        /// </summary>
        /// <example>How to load version 2.0 of a <see cref="Node"/> by passing the sensenet Content Repository path.
        /// In this case you will get a <see cref="Node"/> named "node" filled with the data of the provided 
        /// version of <see cref="Node"/> /Root/MyFavoriteNode.
        /// <code>
        /// VersionNumber versionNumber = new VersionNumber(2, 0);
        /// Node node = Node.LoadNode("/Root/MyFavoriteNode", versionNumber);
        /// </code>
        /// </example>
        /// <param name="path">Node path.</param>
        /// <param name="version">The requested version. For example: new VersionNumber(2, 0).</param>
        /// <returns>The <see cref="Node"/> holds the data of the given version of the <see cref="Node"/> that has the given path.</returns>
        public static Node LoadNode(string path, VersionNumber version)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            return LoadNode(DataStore.LoadNodeHeadAsync(path, CancellationToken.None).GetAwaiter().GetResult(), version);
        }
        /// <summary>
        /// Loads a <see cref="Node"/> by the provided path and version.
        /// </summary>
        /// <example>How to load version 2.0 of a <see cref="Node"/> by passing the sensenet Content Repository path.
        /// In this case you will get a <see cref="Node"/> named "node" filled with the data of the provided 
        /// version of <see cref="Node"/> /Root/MyFavoriteNode.
        /// <code>
        /// VersionNumber versionNumber = new VersionNumber(2, 0);
        /// Node node = Node.LoadNode("/Root/MyFavoriteNode", versionNumber);
        /// </code>
        /// </example>
        /// <param name="path">Node path.</param>
        /// <param name="version">The requested version. For example: new VersionNumber(2, 0).</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The <see cref="Node"/> holds the data of the given version of the <see cref="Node"/> that has the given path.</returns>
        public static async Task<Node> LoadNodeAsync(string path, VersionNumber version, CancellationToken cancellationToken)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            var nodeHead = await DataStore.LoadNodeHeadAsync(path, cancellationToken).ConfigureAwait(false);
            return await LoadNodeAsync(nodeHead, version, cancellationToken).ConfigureAwait(false);
        }
        /// <summary>
        /// Loads a <see cref="Node"/> by the provided Id.
        /// </summary>
        /// <example>How to load the latest version of the <see cref="Node"/> identified by the Id 132. 
        /// In this case you will get a <see cref="Node"/> named node filled with the data of the latest version of <see cref="Node"/> 132.
        /// <code>
        /// Node node = Node.LoadNode(132);
        /// </code>
        /// </example>
        /// <param name="nodeId">Node id.</param>
        /// <returns>The latest version of the <see cref="Node"/> that has the given Id.</returns> 
        public static Node LoadNode(int nodeId)
        {
            return LoadNode(nodeId, DefaultAbstractVersion);
        }
        /// <summary>
        /// Loads a <see cref="Node"/> by the provided Id.
        /// </summary>
        /// <example>How to load the latest version of the <see cref="Node"/> identified by the Id 132. 
        /// In this case you will get a <see cref="Node"/> named node filled with the data of the latest version of <see cref="Node"/> 132.
        /// <code>
        /// Node node = Node.LoadNode(132);
        /// </code>
        /// </example>
        /// <param name="nodeId">Node id.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The latest version of the <see cref="Node"/> that has the given Id.</returns> 
        public static Task<Node> LoadNodeAsync(int nodeId, CancellationToken cancellationToken)
        {
            return LoadNodeAsync(nodeId, DefaultAbstractVersion, cancellationToken);
        }
        /// <summary>
        /// Loads a <see cref="Node"/> by the provided Id and version number.
        /// </summary>
        /// <example>How to load version 2.0 of the <see cref="Node"/> identified by the Id 132. In this case you will 
        /// get a <see cref="Node"/> named "node" filled with the data of the given version of <see cref="Node"/> 132.
        /// <code>
        /// VersionNumber versionNumber = new VersionNumber(2, 0);
        /// Node node = Node.LoadNode(132, versionNumber);
        /// </code>
        /// </example>
        /// <param name="nodeId">Node id.</param>
        /// <param name="version">The requested version. For example: new VersionNumber(2, 0).</param>
        /// <returns>The given version of the <see cref="Node"/> that has the given Id.</returns>
        public static Node LoadNode(int nodeId, VersionNumber version)
        {
            return LoadNode(DataStore.LoadNodeHeadAsync(nodeId, CancellationToken.None).GetAwaiter().GetResult(), version);
        }
        /// <summary>
        /// Loads a <see cref="Node"/> by the provided Id and version number.
        /// </summary>
        /// <example>How to load version 2.0 of the <see cref="Node"/> identified by the Id 132. In this case you will 
        /// get a <see cref="Node"/> named "node" filled with the data of the given version of <see cref="Node"/> 132.
        /// <code>
        /// VersionNumber versionNumber = new VersionNumber(2, 0);
        /// Node node = Node.LoadNode(132, versionNumber);
        /// </code>
        /// </example>
        /// <param name="nodeId">Node id.</param>
        /// <param name="version">The requested version. For example: new VersionNumber(2, 0).</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The given version of the <see cref="Node"/> that has the given Id.</returns>
        public static async Task<Node> LoadNodeAsync(int nodeId, VersionNumber version, CancellationToken cancellationToken)
        {
            var nodeHead = await DataStore.LoadNodeHeadAsync(nodeId, cancellationToken).ConfigureAwait(false);
            return await LoadNodeAsync(nodeHead, version, cancellationToken).ConfigureAwait(false);
        }
        /// <summary>
        /// Loads a <see cref="Node"/> by the provided <see cref="NodeHead"/>.
        /// </summary>
        /// <param name="head">Node head.</param>
        public static Node LoadNode(NodeHead head)
        {
            return LoadNode(head, null);
        }

        /// <summary>
        /// Loads a <see cref="Node"/> by the provided <see cref="NodeHead"/>.
        /// </summary>
        /// <param name="head">Node head.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        public static Task<Node> LoadNodeAsync(NodeHead head, CancellationToken cancellationToken)
        {
            return LoadNodeAsync(head, null, cancellationToken);
        }

        /// <summary>
        /// Loads a <see cref="Node"/> by the provided <see cref="NodeHead"/> and <see cref="VersionNumber"/>.
        /// </summary>
        /// <param name="head">Node head.</param>
        /// <param name="version">The requested version. For example: new VersionNumber(2, 0).</param>
        public static Node LoadNode(NodeHead head, VersionNumber version)
        {
            if (version == null)
                version = DefaultAbstractVersion;

            if (version == VersionNumber.LastFinalized)
                return LoadLastFinalizedVersion(head);

            var retry = 0;
            while (true)
            {
                if (head == null)
                    return null;

                AccessLevel userAccessLevel;
                try
                {
                    userAccessLevel = GetUserAccessLevel(head);
                }
                catch (SenseNet.Security.SecurityStructureException)
                {
                    return null;
                }

                var acceptedLevel = GetAcceptedLevel(userAccessLevel, version);
                var versionId = GetVersionId(head, acceptedLevel != AccessLevel.Header ? acceptedLevel : AccessLevel.Major, version);

                // if the requested version does not exist, return immediately
                if (versionId == 0)
                    return null;

                // <L2Cache>
                var l2cacheKey = GetL2CacheKey(versionId, acceptedLevel);
                var cachedNode = StorageContext.L2Cache.Get(l2cacheKey);
                if (cachedNode != null)
                    return (Node)cachedNode;
                // </L2Cache>

                var token =  DataStore.LoadNodeAsync(head, versionId, CancellationToken.None).GetAwaiter().GetResult();
                if (token.NodeData != null)
                {
                    var node = CreateTargetClass(token);
                    if (acceptedLevel == AccessLevel.Header)
                    {
                        // if the user has Preview permissions, that means a broader access than headonly
                        if (PreviewProvider.HasPreviewPermission(head))
                            node.IsPreviewOnly = true;
                        else
                            node.IsHeadOnly = true;
                    }

                    // <L2Cache>
                    StorageContext.L2Cache.Set(l2cacheKey, node);
                    // </L2Cache>

                    return node;
                }
                // lost version
                if (++retry > 1)
                    return null;
                // retry
                SnTrace.Repository.Write("Version is lost. VersionId:{0}, path:{1}", versionId, head.Path);
                head = NodeHead.Get(head.Id);
            }
        }
        /// <summary>
        /// Loads a <see cref="Node"/> by the provided <see cref="NodeHead"/> and <see cref="VersionNumber"/>.
        /// </summary>
        /// <param name="head">Node head.</param>
        /// <param name="version">The requested version. For example: new VersionNumber(2, 0).</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        public static async Task<Node> LoadNodeAsync(NodeHead head, VersionNumber version, CancellationToken cancellationToken)
        {
            if (version == null)
                version = DefaultAbstractVersion;

            if (version == VersionNumber.LastFinalized)
                return await LoadLastFinalizedVersionAsync(head, cancellationToken).ConfigureAwait(false);

            var retry = 0;
            while (true)
            {
                if (head == null)
                    return null;

                AccessLevel userAccessLevel;
                try
                {
                    userAccessLevel = GetUserAccessLevel(head);
                }
                catch (SecurityStructureException)
                {
                    return null;
                }

                var acceptedLevel = GetAcceptedLevel(userAccessLevel, version);
                var versionId = GetVersionId(head, acceptedLevel != AccessLevel.Header ? acceptedLevel : AccessLevel.Major, version);

                // if the requested version does not exist, return immediately
                if (versionId == 0)
                    return null;

                // <L2Cache>
                var l2CacheKey = GetL2CacheKey(versionId, acceptedLevel);
                var cachedNode = StorageContext.L2Cache.Get(l2CacheKey);
                if (cachedNode != null)
                    return (Node)cachedNode;
                // </L2Cache>

                var token = await DataStore.LoadNodeAsync(head, versionId, CancellationToken.None).ConfigureAwait(false);
                if (token.NodeData != null)
                {
                    var node = CreateTargetClass(token);
                    if (acceptedLevel == AccessLevel.Header)
                    {
                        // if the user has Preview permissions, that means a broader access than head-only
                        if (PreviewProvider.HasPreviewPermission(head))
                            node.IsPreviewOnly = true;
                        else
                            node.IsHeadOnly = true;
                    }

                    // <L2Cache>
                    StorageContext.L2Cache.Set(l2CacheKey, node);
                    // </L2Cache>

                    return node;
                }
                // lost version
                if (++retry > 1)
                    return null;
                // retry
                SnTrace.Repository.Write("Version is lost. VersionId:{0}, path:{1}", versionId, head.Path);
                head = await NodeHead.GetAsync(head.Id, cancellationToken).ConfigureAwait(false);
            }
        }
        /// <summary>
        /// Loads a <see cref="Node"/> by the provided parameter that can be a path or an id as a string.
        /// </summary>
        /// <param name="idOrPath">Id (e.g. "42") or path (e.g. "/Root/System").</param>
        public static Node LoadNodeByIdOrPath(string idOrPath)
        {
            if (string.IsNullOrEmpty(idOrPath))
                return null;

            int nodeId;
            if (Int32.TryParse(idOrPath, out nodeId))
                return Node.LoadNode(nodeId);

            if (RepositoryPath.IsValidPath(idOrPath) == RepositoryPath.PathResult.Correct)
                return Node.LoadNode(idOrPath);

            return null;
        }

        /// <summary>
        /// Loads a <see cref="Node"/> by the provided parameter that can be a path or an id as a string.
        /// </summary>
        /// <param name="idOrPath">Id (e.g. "42") or path (e.g. "/Root/System").</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        public static Task<Node> LoadNodeByIdOrPathAsync(string idOrPath, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(idOrPath))
                return null;

            if (int.TryParse(idOrPath, out var nodeId))
                return Node.LoadNodeAsync(nodeId, cancellationToken);

            if (RepositoryPath.IsValidPath(idOrPath) == RepositoryPath.PathResult.Correct)
                return Node.LoadNodeAsync(idOrPath, cancellationToken);

            return null;
        }
        private static Node LoadLastFinalizedVersion(NodeHead head)
        {
            var node = Node.LoadNode(head, VersionNumber.LastAccessible);
            if (node.SavingState == ContentSavingState.Finalized)
                return node;
            if (node.Security.HasPermission(PermissionType.RecallOldVersion))
                return LoadNodeByLastBeforeVersionId(head);
            else
                return Node.LoadNode(head, VersionNumber.LastMajor);
        }
        private static async Task<Node> LoadLastFinalizedVersionAsync(NodeHead head, CancellationToken cancellationToken)
        {
            var node = await LoadNodeAsync(head, VersionNumber.LastAccessible, cancellationToken).ConfigureAwait(false);
            if (node.SavingState == ContentSavingState.Finalized)
                return node;
            if (node.Security.HasPermission(PermissionType.RecallOldVersion))
                return await LoadNodeByLastBeforeVersionIdAsync(head, cancellationToken).ConfigureAwait(false);

            return await LoadNodeAsync(head, VersionNumber.LastMajor, cancellationToken).ConfigureAwait(false);
        }
        private static Node LoadNodeByLastBeforeVersionId(NodeHead head)
        {
            var versions = head.Versions;
            if (versions.Length < 2)
                return null;
            return Node.LoadNodeByVersionId(versions[versions.Length - 2].VersionId);
        }
        private static async Task<Node> LoadNodeByLastBeforeVersionIdAsync(NodeHead head, CancellationToken cancellationToken)
        {
            var versions = head.Versions;
            if (versions.Length < 2)
                return null;

            return await LoadNodeByVersionIdAsync(versions[versions.Length - 2].VersionId, cancellationToken).ConfigureAwait(false);
        }

        // <L2Cache>
        private static string GetL2CacheKey(int versionId, AccessLevel accessLevel)
        {
            // access level is part of the key because otherwise in elevated mode we would cache 'full' nodes
            // even if the user has only see-only or preview-only permissions for this content
            return String.Concat("node|", versionId, "|", AccessProvider.Current.GetCurrentUser().Id, "|", accessLevel.ToString());
        }
        // </L2Cache>

        // ----------------------------------------------------------------------------- Load algorithm steps

        private static AccessLevel GetUserAccessLevel(NodeHead head)
        {
            var userId = AccessProvider.Current.GetCurrentUser().Id;
            var isOwner = head.CreatorId == userId;
            switch (SecurityHandler.GetPermittedLevel(head))
            {
                case PermittedLevel.None:
                    throw new SenseNetSecurityException(head.Path, PermissionType.See, AccessProvider.Current.GetCurrentUser(), "Access denied.");
                case PermittedLevel.HeadOnly:
                    return AccessLevel.Header;
                case PermittedLevel.PublicOnly:
                    return AccessLevel.Major;
                case PermittedLevel.All:
                    return AccessLevel.Minor;
                default:
                    throw new SnNotSupportedException();
            }
        }
        private static AccessLevel GetAcceptedLevel(AccessLevel userAccessLevel, VersionNumber requestedVersion)
        {
            //          HO  Ma  Mi
            //  -------------------
            //  LA      HO  Ma  Mi
            //  -------------------
            //  HO      HO  HO  HO
            //  LMa     X   Ma  Ma
            //  VMa     X   Ma  Ma
            //  LMi     X   X   Mi
            //  VMi     X   X   Mi

            var definedIsMajor = false;
            var definedIsMinor = false;
            if (!requestedVersion.IsAbstractVersion)
            {
                definedIsMajor = requestedVersion.IsMajor;
                definedIsMinor = !requestedVersion.IsMajor;
            }

            if (requestedVersion == VersionNumber.LastAccessible || requestedVersion == VersionNumber.LastFinalized)
                return userAccessLevel;
            if (requestedVersion == VersionNumber.Header)
                return AccessLevel.Header;
            if (requestedVersion == VersionNumber.LastMajor || definedIsMajor)
            {
                // In case the user has only head access, we should still enable loading 
                // major versions: it will be a head-only or preview-only node.
                if (userAccessLevel < AccessLevel.Major)
                    return userAccessLevel;

                return AccessLevel.Major;
            }
            if (requestedVersion == VersionNumber.LastMinor || definedIsMinor)
            {
                if (userAccessLevel < AccessLevel.Minor)
                    throw new SenseNetSecurityException("");
                return AccessLevel.Minor;
            }
            throw new SnNotSupportedException("####");
        }
        private static int GetVersionId(NodeHead nodeHead, AccessLevel acceptedLevel, VersionNumber version)
        {
            if (version.IsAbstractVersion)
            {
                switch (acceptedLevel)
                {
                    // get from last major/minor slot of nodeHead
                    case AccessLevel.Header:
                    case AccessLevel.Major:
                        return nodeHead.LastMajorVersionId;
                    case AccessLevel.Minor:
                        return nodeHead.LastMinorVersionId;
                    default:
                        throw new SnNotSupportedException();
                }
            }
            else
            {
                // lookup versionlist of node from nodeHead or read from DB
                return nodeHead.GetVersionId(version);
            }
        }
        private static Node CreateTargetClass(NodeToken token)
        {
            if (token == null)
                return null;

            var node = token.NodeType.CreateInstance(token);

            node.FireOnLoaded();
            return node;
        }

        /// <summary>
        /// Returns a cached value by the provided name. You can store any instance-related cacheable
        /// object using the <see cref="SetCachedData"/> method.
        /// </summary>
        public object GetCachedData(string name)
        {
            return this.Data.GetExtendedSharedData(name);
        }
        /// <summary>
        /// Associates the given object to this <see cref="Node"/> instance with the specified name.
        /// </summary>
        public void SetCachedData(string name, object value)
        {
            this.Data.SetExtendedSharedData(name, value);
        }
        /// <summary>
        /// Removes the cached named object if it exists.
        /// </summary>
        public void ResetCachedData(string name)
        {
            this.Data.ResetExtendedSharedData(name);
        }

        private static void FillData(Node node, NodeToken token)
        {
            string typeName = node.GetType().FullName;
            string typeNameInHead = NodeTypeManager.Current.NodeTypes.GetItemById(token.NodeData.NodeTypeId).ClassName;
            if (typeNameInHead != typeName)
            {
                var message = String.Concat("Cannot create a ", typeName, " instance because type name is different in the passed head: ", typeNameInHead);
                throw new RegistrationException(message);
            }

            node.SetNodeData(token.NodeData);
            node._data.IsShared = true;
        }

        /// <summary>
        /// Gets the list of avaliable versions of the <see cref="Node"/> identified by Id.
        /// </summary>
        /// <returns>A list of version numbers.</returns>
        public static List<VersionNumber> GetVersionNumbers(int nodeId)
        {
            return DataStore.GetVersionNumbersAsync(nodeId, CancellationToken.None).GetAwaiter().GetResult()
                .Select(x => x.VersionNumber)
                .ToList();
        }
        /// <summary>
        /// Gets the list of avaliable versions of the <see cref="Node"/> identified by path.
        /// </summary>
        /// <returns>A list of version numbers.</returns>
        public static List<VersionNumber> GetVersionNumbers(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            return DataStore.GetVersionNumbersAsync(path, CancellationToken.None).GetAwaiter().GetResult()
                .Select(x => x.VersionNumber)
                .ToList();
        }

        /// <summary>
        /// Returns collection of all permitted versions of this <see cref="Node"/> identified by Id.
        /// The current user need to have minimum <see cref="PermissionType.Open"/> and 
        /// <see cref="PermissionType.RecallOldVersion"/> permissions. In that case the collection
        /// contains all public versions. If <see cref="PermissionType.OpenMinor"/> is also 
        /// permitted, all versions will be returned. If the user does not have the required
        /// set of permissions, a <see cref="SenseNetSecurityException"/> will be thrown. 
        /// </summary>
        public IEnumerable<Node> LoadVersions()
        {
            Security.Assert(PermissionType.RecallOldVersion, PermissionType.Open);
            if (Security.HasPermission(PermissionType.OpenMinor))
                return LoadAllVersions();
            return LoadPublicVersions();
        }
        private IEnumerable<Node> LoadPublicVersions()
        {
            var head = NodeHead.Get(this.Id);
            var x = head.Versions.Where(v => v.VersionNumber.Status == VersionStatus.Approved).Select(v => Node.LoadNode(this.Id, v.VersionNumber)).Where(n => n != null).ToArray();
            return x;
        }
        private IEnumerable<Node> LoadAllVersions()
        {
            var head = NodeHead.Get(this.Id);
            var x = head.Versions.Select(v => Node.LoadNode(this.Id, v.VersionNumber)).Where(n => n != null).ToArray();
            return x;
        }

        /// <summary>
        /// Loads a <see cref="Node"/> by the provided versionId.
        /// </summary>
        /// <param name="versionId">Version id.</param>
        public static Node LoadNodeByVersionId(int versionId)
        {

            NodeHead head = NodeHead.GetByVersionId(versionId);
            if (head == null)
                return null;

            SecurityHandler.Assert(head, PermissionType.RecallOldVersion, PermissionType.Open);

            var token =  DataStore.LoadNodeAsync(head, versionId, CancellationToken.None).GetAwaiter().GetResult();

            Node node = null;
            if (token.NodeData != null)
                node = CreateTargetClass(token);
            return node;
        }
        /// <summary>
        /// Loads a <see cref="Node"/> by the provided versionId.
        /// </summary>
        /// <param name="versionId">Version id.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        public static async Task<Node> LoadNodeByVersionIdAsync(int versionId, CancellationToken cancellationToken)
        {
            var head = await NodeHead.GetByVersionIdAsync(versionId, cancellationToken).ConfigureAwait(false);
            if (head == null)
                return null;

            SecurityHandler.Assert(head, PermissionType.RecallOldVersion, PermissionType.Open);

            var token = await DataStore.LoadNodeAsync(head, versionId, cancellationToken).ConfigureAwait(false);

            Node node = null;
            if (token.NodeData != null)
                node = CreateTargetClass(token);
            return node;
        }

        internal void Refresh()
        {
            if (!this.IsDirty)
                return;

            var nodeHead = NodeHead.Get(Id);

            if (nodeHead != null)
            {
                //  Reload by nodeHead.LastMinorVersionId
                var token =  DataStore.LoadNodeAsync(nodeHead, nodeHead.LastMinorVersionId, CancellationToken.None)
                    .GetAwaiter().GetResult();
                var sharedData = token.NodeData;
                sharedData.IsShared = true;
                SetNodeData(sharedData);
                __accessors = null;
            }

            this.IsDirty = false;
        }
        internal void Reload()
        {
            var nodeHead = NodeHead.GetByVersionId(this.VersionId);
            if (nodeHead == null)
                throw new ContentNotFoundException(String.Format("Version of a content was not found. VersionId: {0}, old Path: {1}", this.VersionId, this.Path));

            var token =  DataStore.LoadNodeAsync(nodeHead, this.VersionId, CancellationToken.None).GetAwaiter().GetResult();
            var sharedData = token.NodeData;
            sharedData.IsShared = true;
            SetNodeData(sharedData);
            __accessors = null;
            this.IsDirty = false;
        }
        #endregion

        #region // ================================================================================================= Save methods

        /// <summary>
        /// Stores the modifications of this <see cref="Node"/> instance to the database.
        /// </summary>
        public virtual void Save()
        {
            var settings = new NodeSaveSettings
            {
                Node = this,
                HasApproving = false,
                VersioningMode = VersioningMode.None,
            };
            settings.ExpectedVersionId = settings.CurrentVersionId;
            settings.Validate();
            this.Save(settings);
        }
        private static IDictionary<string, object> CollectAllProperties(NodeData data)
        {
            return data.GetAllValues();
        }
        private static IDictionary<string, object> CollectChangedProperties(object[] args)
        {
            var sb = new StringBuilder();
            foreach (var changedValue in (IEnumerable<ChangedData>)args[2])
            {
                var name = changedValue.Name ?? string.Empty;
                if (name.StartsWith("#"))
                    name = name.Replace("#", "ContentListField-");

                var old = changedValue.Original.ToString();
                if (old.Contains("\""))
                    old = old.Replace("\"", "\\\"");

                var @new = changedValue.Value.ToString();
                if (@new.Contains("\""))
                    @new = @new.Replace("\"", "\\\"");

                sb.Append("{").AppendFormat(" name: \"{0}\", oldValue: \"{1}\", newValue: \"{2}\"", name, old, @new).AppendLine("}");
            }
            return new Dictionary<string, object>
            {
                {"Id", args[0]},
                {"Path", args[1]},
                {"ChangedData", sb.ToString()}
            };
        }

        private void SaveCopied(NodeSaveSettings settings)
        {
            using (var op = SnTrace.ContentOperation.StartOperation("Node.SaveCopied"))
            {
                var currentUser = AccessProvider.Current.GetOriginalUser();
                var currentUserId = currentUser.Id;

                var currentUserNode = currentUser as Node;
                if (currentUserNode == null)
                    throw new InvalidOperationException("Cannot save the content because the current user account representation is not a Node.");

                var thisList = this as IContentList;
                if (thisList != null)
                {
                    var newListType = thisList.GetContentListType();
                    if (this.ContentListType != null || newListType != null)
                    {
                        if (this.ContentListType == null)
                        {
                            // AssignNewContentListType
                            this.ContentListType = newListType;
                        }
                        else if (newListType == null)
                        {
                            // AssignNullContentListType
                            throw new NotSupportedException();
                        }
                        else if (this.ContentListType.Id != newListType.Id)
                        {
                            // Change ContentListType
                            throw new NotSupportedException();
                        }
                    }
                }

                if (this.Id != 0)
                    throw new InvalidOperationException("Id of copied node must be 0.");

                if (IsDeleted)
                    throw new InvalidOperationException("Cannot save deleted node.");

                // Check permissions: got to have AddNew permission on the parent
                SecurityHandler.Assert(this.ParentId, PermissionType.AddNew);

                RepositoryPath.CheckValidName(this.Name);

                // Validate
                if (this.ParentId == 0)
                    throw new InvalidPathException(SR.Exceptions.General.Msg_ParentNodeDoesNotExists); // parent Node does not exists
                if (this.Name.Trim().Length == 0)
                    throw new InvalidPathException(SR.Exceptions.General.Msg_NameCannotBeEmpty);
                if (this.IsModified)
                    this.Name = this.Name.Trim();

                // Update the modification
                
                // update to current
                var now = DataStore.RoundDateTime(DateTime.UtcNow);
                this.ModificationDate = now;
                this.Data.ModifiedById = currentUserId;
                this.VersionModificationDate = now;
                this.Data.VersionModifiedById = currentUserId;

                // collect data for populator
                var parentPath = RepositoryPath.GetParentPath(this.Path);
                var thisPath = RepositoryPath.Combine(parentPath, this.Name);

                // save
                SaveNodeData(this, settings, SearchManager.GetIndexPopulator(), thisPath, thisPath);

                // <L2Cache>
                StorageContext.L2Cache.Clear();
                // </L2Cache>

                // refresh data (e.g. in case of undo checkout)
                if (settings.ForceRefresh)
                {
                    this.IsDirty = true;
                    Refresh();
                }

                // make it as shared (flatten the data)
                NodeData.MakeSharedData(this._data);

                // insert node data into cache after save
                CacheNodeAfterSave();

                // events
                FireOnCreated(null);

                op.Successful = true;
            }
        }

        /// <summary>
        /// Stores the modifications of this <see cref="Node"/> instance to the database.
        /// The generated <see cref="VersionNumber"/> depends on the requested 
        /// <see cref="VersionRaising"/> mode and the given <see cref="VersionStatus"/>.
        /// </summary>
        public void Save(VersionRaising versionRaising, VersionStatus versionStatus)
        {
            Save(versionRaising, versionStatus, false);
        }
        internal void Save(VersionRaising versionRaising, VersionStatus versionStatus, bool takingLockOver)
        {
            var settings = new NodeSaveSettings { Node = this, HasApproving = false, TakingLockOver = takingLockOver };
            var curVer = settings.CurrentVersion;
            var history = NodeHead.Get(this.Id).Versions;
            var biggest = history.OrderBy(v => v.VersionNumber.VersionString).LastOrDefault();
            var biggestVer = biggest == null ? curVer : biggest.VersionNumber;

            switch (versionRaising)
            {
                case VersionRaising.None:
                    settings.VersioningMode = VersioningMode.None;
                    settings.ExpectedVersion = curVer.ChangeStatus(versionStatus);
                    settings.ExpectedVersionId = settings.CurrentVersionId;
                    break;
                case VersionRaising.NextMinor:
                    settings.VersioningMode = VersioningMode.Full;
                    settings.ExpectedVersion = new VersionNumber(biggestVer.Major, biggestVer.Minor + 1, versionStatus);
                    settings.ExpectedVersionId = 0;
                    break;
                case VersionRaising.NextMajor:
                    settings.VersioningMode = VersioningMode.Full;
                    settings.ExpectedVersion = new VersionNumber(biggestVer.Major + 1, 0, versionStatus);
                    settings.ExpectedVersionId = 0;
                    break;
                default:
                    break;
            }

            Save(settings);
        }
        #endregion

        /// <summary>
        /// Stores the modifications of this <see cref="Node"/> instance to the database.
        /// The tasks of storing depend on the given <see cref="NodeSaveSettings"/>.
        /// </summary>
        /// <param name="settings">Describes the tasks and algorithms for persisting the node.</param>
        public virtual void Save(NodeSaveSettings settings)
        {
            var isNew = this.IsNew;
            var previousSavingState = this.SavingState;

            if (_data != null)
                _data.SavingTimer.Restart();

            var lockBefore = this.Version == null ? false : this.Version.Status == VersionStatus.Locked;
            if (isNew)
                CreatingInProgress = true;
            var creating = CreatingInProgress;
            if (settings.ExpectedVersion.Status != VersionStatus.Locked)
                CreatingInProgress = false;

            if (_copying)
            {
                SaveCopied(settings);
                if (_data != null)
                    _data.SavingTimer.Stop();
                return;
            }

            // If this is a regular save (like in most cases), saving state will be Finalized. Otherwise 
            // it can be Creating if it is new or Modifying if it already exists. ModifyingLocked state
            // was created to let the finalizing code know that it should not check in the content
            // at the end of the multistep saving process.
            this.SavingState = settings.MultistepSaving
                                   ? (isNew
                                        ? ContentSavingState.Creating
                                        : (this.Locked
                                            ? ContentSavingState.ModifyingLocked
                                            : ContentSavingState.Modifying))
                                   : ContentSavingState.Finalized;

            settings.Validate();
            ApplySettings(settings);

            ChecksBeforeSave();

            if (!settings.TakingLockOver)
                AssertLock();

            var isElevatedMode = AccessProvider.Current.GetCurrentUser().Id == -1;
            var currentUser = AccessProvider.Current.GetOriginalUser();
            var currentUserId = currentUser.Id;

            bool isNewNode = (this.Id == 0);

            // No changes -> return
            if (!settings.NodeChanged())
            {
                if (_data != null)
                    _data.SavingTimer.Stop();

                SnTrace.ContentOperation.Write("Node is not saved because it has no changes. Path: {0}/{1}", this.ParentPath, this.Name);
                return;
            }


            // Rename?
            string thisName = this.Name;
            var originalName = (this.Data.SharedData == null) ? thisName : this.Data.SharedData.Name;
            var renamed = originalName.ToLower() != thisName.ToLower();

            var msg = renamed
                ? string.Format("NODE.RENAME Id: {0}): {1} -> {2}, ParentPath: {3}", Id, originalName, thisName, ParentPath)
                : string.Format("NODE.SAVE Id: {0}, VersionId: {1}, Version: {2}, Name: {3}, ParentPath: {4}", Id, VersionId, Version, thisName, ParentPath);
            using (var audit = new AuditBlock(new AuditEvent("ContentSaved", 1), msg, new Dictionary<string, object>
            { { "Id", this.Id }, { "Path", this.Path } }))
            {
                using (var op = SnTrace.ContentOperation.StartOperation(msg))
                {
                    var parentPath = RepositoryPath.GetParentPath(this.Path);
                    var newPath = RepositoryPath.Combine(parentPath, thisName);
                    var originalPath = renamed ? RepositoryPath.Combine(parentPath, originalName) : newPath;

                    // Update the modification
                    if (!isElevatedMode ||
                        ElevatedModificationVisibilityRule.EvaluateRule(this))
                    {
                        var now = DataStore.RoundDateTime(DateTime.UtcNow);
                        if (!_data.VersionModificationDateChanged)
                            this.VersionModificationDate = now;
                        if (!_data.VersionModifiedByIdChanged)
                            this.Data.VersionModifiedById = currentUserId;
                        if (!_data.ModificationDateChanged)
                            this.ModificationDate = now;
                        if (!_data.ModifiedByIdChanged)
                            this.Data.ModifiedById = currentUserId;
                    }

                    // Update the creator if the Visitor is creating this content
                    if (isNewNode && currentUserId == Identifiers.VisitorUserId)
                    {
                        using (new SystemAccount())
                        {
                            var list = this.LoadContentList();
                            if (list != null)
                            {
                                var ownerWhenVsitor = list.GetReference<Node>("OwnerWhenVisitor");
                                var adminId = ownerWhenVsitor != null
                                                ? ownerWhenVsitor.Id
                                                : Identifiers.AdministratorUserId;

                                this.Data.VersionCreatedById = adminId;
                                this.Data.VersionModifiedById = adminId;
                                this.Data.CreatedById = adminId;
                                this.Data.ModifiedById = adminId;

                                // Re-set the owner only if it has not been changed (for example during
                                // templated userprofile creation the new user should be the owner of the
                                // content and this is set in the upper layer).
                                if (this.Data.OwnerId == Identifiers.VisitorUserId)
                                    this.Data.OwnerId = adminId;
                            }
                        }
                    }

                    // collect changed field values for logging and info for nodeobservers
                    IEnumerable<ChangedData> changedData = null;
                    if (!isNewNode)
                        changedData = this.Data.GetChangedValues();

                    if (settings.MultistepSaving)
                    {
                        this.ChangedData = changedData;
                    }
                    else
                    {
                        if (previousSavingState == ContentSavingState.Modifying || previousSavingState == ContentSavingState.ModifyingLocked)
                        {
                            changedData = changedData == null
                                ? this.ChangedData
                                : this.ChangedData.Union(changedData).ToList();
                        }

                        this.ChangedData = null;
                    }

                    IDictionary<string, object> customData = null;
                    if (!lockBefore)
                    {
                        CancellableNodeEventArgs args = null;
                        if (creating)
                        {
                            args = new CancellableNodeEventArgs(this, CancellableNodeEvent.Creating);
                            FireOnCreating(args);
                        }
                        else
                        {
                            args = new CancellableNodeEventArgs(this, CancellableNodeEvent.Modifying, changedData);
                            FireOnModifying(args);
                        }
                        if (args.Cancel)
                        {
                            throw new CancelNodeEventException(args.CancelMessage, args.EventType, this);
                        }
                        customData = args.GetCustomData();
                    }

                    AssertSaving(changedData);

                    BenchmarkCounter.IncrementBy(BenchmarkCounter.CounterName.BeforeSaveToDb, _data != null ? _data.SavingTimer.ElapsedTicks : 0);
                    if (_data != null)
                        _data.SavingTimer.Restart();

                    // save
                    TreeLock treeLock = null;
                    if (renamed)
                        treeLock = TreeLock.AcquireAsync(CancellationToken.None,this.ParentPath + "/" + this.Name, originalPath).GetAwaiter().GetResult();
                    else
                        TreeLock.AssertFreeAsync(CancellationToken.None, this.ParentPath + "/" + this.Name).GetAwaiter().GetResult();

                    try
                    {
                        this.Data.PreloadTextProperties();
                        SaveNodeData(this, settings, SearchManager.GetIndexPopulator(), originalPath, newPath);
                    }
                    finally
                    {
                        treeLock?.Dispose();
                    }

                    // <L2Cache>
                    StorageContext.L2Cache.Clear();
                    // </L2Cache>

                    BenchmarkCounter.IncrementBy(BenchmarkCounter.CounterName.CommitPopulateNode, _data != null ? _data.SavingTimer.ElapsedTicks : 0);
                    if (_data != null)
                        _data.SavingTimer.Restart();

                    // log
                    if (!settings.MultistepSaving && !settings.TakingLockOver)
                    {
                        if (Logging.AuditEnabled)
                        {
                            if (isNewNode || previousSavingState == ContentSavingState.Creating)
                                SnLog.WriteAudit(AuditEvent.ContentCreated, CollectAllProperties(this.Data));
                            else
                                SnLog.WriteAudit(AuditEvent.ContentUpdated, CollectChangedProperties(new object[] { this.Id, this.Path, changedData }));
                        }
                    }

                    BenchmarkCounter.IncrementBy(BenchmarkCounter.CounterName.Audit, _data != null ? _data.SavingTimer.ElapsedTicks : 0);
                    if (_data != null)
                        _data.SavingTimer.Restart();

                    // refresh data (e.g. in case of undo checkout)
                    if (settings.ForceRefresh)
                    {
                        this.IsDirty = true;
                        Refresh();
                    }

                    // make it as shared (flatten the data)
                    NodeData.MakeSharedData(this._data);

                    // remove too big items
                    this._data.RemoveStreamsAndLongTexts();

                    // insert node data into cache after save
                    CacheNodeAfterSave();

                    // events
                    if (!settings.MultistepSaving)
                    {
                        if (this.Version.Status != VersionStatus.Locked)
                        {
                            if (creating)
                                FireOnCreated(customData);
                            else
                                FireOnModified(originalPath, customData, changedData);
                        }
                    }
                    op.Successful = true;
                }
                audit.Successful = true;
            }

            BenchmarkCounter.IncrementBy(BenchmarkCounter.CounterName.FinalizingSave, _data != null ? _data.SavingTimer.ElapsedTicks : 0);
            if (_data != null)
                _data.SavingTimer.Restart();
            if (_data != null)
                _data.SavingTimer.Stop();
        }

        /// <summary>
        /// Ends the multistep saving process and makes the Content available for modification.
        /// </summary>
        public virtual void FinalizeContent()
        {
            if (SavingState == ContentSavingState.Finalized)
                throw new InvalidOperationException("Cannot finalize the content " + this.Path);

            using (var op = SnTrace.ContentOperation.StartOperation("FinalizeContent.  Path: {0}/{1}", this.ParentPath, this.Name))
            {
                // log
                if (Logging.AuditEnabled)
                {
                    if (SavingState == ContentSavingState.Creating)
                        SnLog.WriteAudit(AuditEvent.ContentCreated, CollectAllProperties(this.Data));
                    else
                        SnLog.WriteAudit(AuditEvent.ContentUpdated, CollectChangedProperties(new object[] { this.Id, this.Path, this.ChangedData }));
                }

                this.SavingState = ContentSavingState.Finalized;
                var changedData = this.ChangedData;
                this.ChangedData = null;

                var settings = new NodeSaveSettings
                {
                    Node = this,
                    ExpectedVersion = this.Version,
                    ExpectedVersionId = this.VersionId,
                    MultistepSaving = false
                };
                SaveNodeData(this, settings, SearchManager.GetIndexPopulator(), Path, Path);

                // events
                if (this.Version.Status != VersionStatus.Locked)
                {
                    if (SavingState == ContentSavingState.Creating)
                        FireOnCreated(null);
                    else
                        FireOnModified(this.Path, null, changedData);
                }

                op.Successful = true;
            }
        }

        private void ApplySettings(NodeSaveSettings settings)
        {
            if (settings.ExpectedVersion != null && settings.ExpectedVersion.ToString() != this.Version.ToString())
            {
                this.Version = settings.ExpectedVersion;

                MakePrivateData();
                _data.VersionCreationDate = DataStore.RoundDateTime(DateTime.UtcNow);
            }

            if (settings.LockerUserId != null)
            {
                if (settings.LockerUserId != 0)
                {
                    if (!this.Locked)
                    {
                        // Lock
                        LockToken = Guid.NewGuid().ToString();
                        LockedById = settings.LockerUserId.Value; // AccessProvider.Current.GetCurrentUser().Id;
                        LockDate = DateTime.UtcNow;
                        LastLockUpdate = DateTime.UtcNow;
                        LockTimeout = RepositoryEnvironment.DefaultLockTimeout;
                    }
                    else
                    {
                        // RefreshLock
                        if (AccessProvider.Current.GetCurrentUser().Id != Identifiers.SystemUserId
                            && !settings.TakingLockOver
                            && this.LockedById != AccessProvider.Current.GetCurrentUser().Id)
                            throw new SenseNetSecurityException(this.Id, "Node is locked by another user");
                        LastLockUpdate = DateTime.UtcNow;
                    }
                }
                else
                {
                    // Unlock
                    if (Locked)
                    {
                        this.LockedById = 0;
                        this.LockToken = string.Empty;
                        this.LockTimeout = 0;
                        this.LockDate = new DateTime(1800, 1, 1);
                        this.LastLockUpdate = new DateTime(1800, 1, 1);
                        this.LockType = 0;
                    }
                }
            }
        }

        private void ChecksBeforeSave()
        {
            var currentUser = AccessProvider.Current.GetOriginalUser();

            var currentUserNode = currentUser as Node;
            if (currentUserNode == null && !(currentUser is StartupUser))
                throw new InvalidOperationException("Cannot save the content because the current user account representation is not a Node.");

            var thisList = this as IContentList;
            if (thisList != null)
            {
                var newListType = thisList.GetContentListType();
                if (this.ContentListType != null || newListType != null)
                {
                    if (this.ContentListType == null)
                    {
                        // AssignNewContentListType
                        this.ContentListType = newListType;
                    }
                    else if (newListType == null)
                    {
                        // AssignNullContentListType
                        throw new NotSupportedException();
                    }
                    else if (this.ContentListType.Id != newListType.Id)
                    {
                        // Change ContentListType
                        throw new NotSupportedException();
                    }
                }
            }

            if (IsDeleted)
                throw new InvalidOperationException("Cannot save deleted node.");

            // Check permissions
            if (this.Id == 0)
            {
                var parent = this.Parent;
                parent.Security.Assert(PermissionType.AddNew);
            }
            else
                Security.Assert(PermissionType.Save);


            RepositoryPath.CheckValidName(this.Name);

            // Validate
            if (this.ParentId == 0 && this.Id != Identifiers.PortalRootId)
                throw new InvalidPathException(SR.Exceptions.General.Msg_ParentNodeDoesNotExists);
            if (this.Name.Trim().Length == 0)
                throw new InvalidPathException(SR.Exceptions.General.Msg_NameCannotBeEmpty);
            if (this.IsModified)
                this.Name = this.Name.Trim();

            AssertMimeTypes();
        }
        private void AssertMimeTypes()
        {
            foreach (var item in this.Accessors)
            {
                var binProp = item.Value as BinaryData;
                if (binProp != null)
                    if (binProp.Size > 0 && string.IsNullOrEmpty(binProp.ContentType))
                        binProp.ContentType = MimeTable.GetMimeType(String.Empty);
            }
        }

        private void CacheNodeAfterSave()
        {
            // don't insert into cache if node is a content type
            if (this.IsContentType)
                return;

            switch (CacheConfiguration.CacheContentAfterSaveMode)
            {
                case CacheConfiguration.CacheContentAfterSaveOption.None:
                    // do nothing
                    break;
                case CacheConfiguration.CacheContentAfterSaveOption.Containers:
                    // cache IFolders only
                    if (this is IFolder)
                        DataStore.CacheNodeData(this._data);
                    break;
                case CacheConfiguration.CacheContentAfterSaveOption.All:
                    // cache every node
                    DataStore.CacheNodeData(this._data);
                    break;
            }
        }

        #region /* ------------------------------------------------------------------------- SaveNodeData */
        private const int maxDeadlockIterations = 3;
        private const int sleepIfDeadlock = 1000;

        private static void SaveNodeData(Node node, NodeSaveSettings settings, IIndexPopulator populator, string originalPath, string newPath)
        {
            var isNewNode = node.Id == 0;
            var isOwnerChanged = node.Data.IsPropertyChanged("OwnerId");
            if (!isNewNode && isOwnerChanged)
                node.Security.Assert(PermissionType.TakeOwnership);

            var data = node.Data;
            var attempt = 0;

            using (var op = SnTrace.Database.StartOperation("SaveNodeData"))
            {
                while (true)
                {
                    attempt++;

                    var deadlockException = SaveNodeDataAttempt(node, settings, populator, originalPath, newPath);
                    if (deadlockException == null)
                        break;

                    SnTrace.Database.Write("DEADLOCK detected. Attempt: {0}/{1}, NodeId:{2}, Version:{3}, Path:{4}",
                        attempt, maxDeadlockIterations, node.Id, node.Version, node.Path);

                    if (attempt >= maxDeadlockIterations)
                        throw new DataException(string.Format("Error saving node. Id: {0}, Path: {1}", node.Id, node.Path), deadlockException);

                    SnLog.WriteWarning("Deadlock detected in SaveNodeData", properties:
                        new Dictionary<string, object>
                        {
                            {"Id: ", node.Id},
                            {"Path: ", node.Path},
                            {"Version: ", node.Version},
                            {"Attempt: ", attempt}
                        });

                    System.Threading.Thread.Sleep(sleepIfDeadlock);
                }
                op.Successful = true;
            }

            try
            {
                if (isNewNode)
                {
                    SecurityHandler.CreateSecurityEntity(node.Id, node.ParentId, node.OwnerId);
                }
                else if (isOwnerChanged)
                {
                    SecurityHandler.ModifyEntityOwner(node.Id, node.OwnerId);
                }
            }
            catch (EntityNotFoundException e)
            {
                SnLog.WriteException(e, $"Error during creating or modifying security entity: {node.Id}. Original message: {e}",
                    EventId.Security);
            }
            catch (SecurityStructureException) // suppressed
            {
                // no need to log this: somebody else already created or modified this security entity
            }

            if (isNewNode)
                SnTrace.ContentOperation.Write("Node created. Id:{0}, Path:{1}", data.Id, data.Path);
            else
                SnTrace.ContentOperation.Write("Node updated. Id:{0}, Path:{1}", data.Id, data.Path);
        }
        private static Exception SaveNodeDataAttempt(Node node, NodeSaveSettings settings, IIndexPopulator populator, string originalPath, string newPath)
        {
            IndexDocumentData indexDocument = null;
            bool hasBinary = false;

            var data = node.Data;
            var isNewNode = data.Id == 0;

            var msg = "Saving Node#" + node.Id + ", " + node.ParentPath + "/" + node.Name;

            using (var op = SnTrace.Database.StartOperation(msg))
            {
                try
                {
                    // collect data for populator
                    var populatorData = populator.BeginPopulateNode(node, settings, originalPath, newPath);

                    if (settings.NodeHead != null)
                    {
                        settings.LastMajorVersionIdBefore = settings.NodeHead.LastMajorVersionId;
                        settings.LastMinorVersionIdBefore = settings.NodeHead.LastMinorVersionId;
                    }

                    // Finalize path
                    string path;

                    if (data.Id != Identifiers.PortalRootId)
                    {
                        var parent = NodeHead.Get(data.ParentId);
                        if (parent == null)
                            throw new ContentNotFoundException(data.ParentId.ToString());
                        path = RepositoryPath.Combine(parent.Path, data.Name);
                    }
                    else
                    {
                        path = Identifiers.RootPath;
                    }
                    Node.AssertPath(path);
                    data.Path = path;

                    // Store in the database
                    int lastMajorVersionId, lastMinorVersionId;

                    var head = DataStore.SaveNodeAsync(data, settings, CancellationToken.None).GetAwaiter().GetResult();
                    lastMajorVersionId = settings.LastMajorVersionIdAfter;
                    lastMinorVersionId = settings.LastMinorVersionIdAfter;
                    node.RefreshVersionInfo(head);

                    // here we re-create the node head to insert it into the cache and refresh the version info);
                    if (lastMajorVersionId > 0 || lastMinorVersionId > 0)
                    {
                        if (!settings.DeletableVersionIds.Contains(node.VersionId))
                        {
                            // Elevation: we need to create the index document with full
                            // control to avoid field access errors (indexing must be independent
                            // from the current users permissions).
                            using (new SystemAccount())
                            {
                                var result = DataStore.SaveIndexDocumentAsync(node, true, isNewNode, CancellationToken.None)
                                    .GetAwaiter().GetResult();
                                indexDocument = result.IndexDocumentData;
                                hasBinary = result.HasBinary;
                            }
                        }
                    }

                    // populate index only if it is enabled on this content (e.g. preview images will be skipped)
                    using (var op2 = SnTrace.Index.StartOperation("Indexing node"))
                    {
                        if (node.IsIndexingEnabled)
                        {
                            using (new SystemAccount())
                                populator.CommitPopulateNodeAsync(populatorData, indexDocument, CancellationToken.None).GetAwaiter().GetResult();
                        }

                        if (indexDocument != null && hasBinary)
                        {
                            using (new SystemAccount())
                            {
                                indexDocument = DataStore.SaveIndexDocumentAsync(node, indexDocument, CancellationToken.None)
                                    .GetAwaiter().GetResult();
                                populator.FinalizeTextExtractingAsync(populatorData, indexDocument, CancellationToken.None).GetAwaiter().GetResult();
                            }
                        }
                        op2.Successful = true;
                    }
                }
                catch (TransactionDeadlockedException tde)
                {
                    return tde;
                }
                catch (AggregateException ae)
                {
                    if (ae.InnerException is TransactionDeadlockedException)
                        return ae.InnerException;
                    throw;
                }
                catch (Exception e)
                {
                    var ee = SavingExceptionHelper(data, e);
                    if (ee == e)
                        throw;
                    throw ee;
                }
                op.Successful = true;
            }
            return null;
        }
        private static Exception SavingExceptionHelper(NodeData data, Exception catchedEx)
        {
            var message = "The content cannot be saved.";
            if (catchedEx.Message.StartsWith("Cannot insert duplicate key"))
            {
                message += " A content with the name you specified already exists.";

                var appExc = new NodeAlreadyExistsException(message, catchedEx); // new ApplicationException(message, catchedEx);
                appExc.Data.Add("NodeId", data.Id);
                appExc.Data.Add("Path", data.Path);
                appExc.Data.Add("OriginalPath", data.OriginalPath);

                appExc.Data.Add("ErrorCode", "ExistingNode");
                return appExc;
            }
            return catchedEx;
        }
        #endregion

        #region // ================================================================================================= Move methods

        //TODO: Node.GetChildTypesToAllow(int nodeId): check SQL procedure algorithm. See issue #259
        /// <summary>
        /// Gets all the types that can be found in a subtree under a node defined by an Id.
        /// </summary>
        public static IEnumerable<NodeType> GetChildTypesToAllow(int nodeId)
        {
            return DataStore.LoadChildTypesToAllowAsync(nodeId, CancellationToken.None).GetAwaiter().GetResult();
        }
        //TODO: Node.GetChildTypesToAllow(): check SQL procedure algorithm. See issue #259
        /// <summary>
        /// Gets all the types that can be found in a subtree under the current node.
        /// </summary>
        public IEnumerable<NodeType> GetChildTypesToAllow()
        {
            return DataStore.LoadChildTypesToAllowAsync(this.Id, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Moves the <see cref="Node"/> identified by the source path to another location. 
        /// The destination <see cref="Node"/> is also identified by path. 
        /// </summary>
        /// <remarks>Use this method if you do not want to instantiate the <see cref="Node"/>s.</remarks>
        public static void Move(string sourcePath, string targetPath)
        {
            Node sourceNode = Node.LoadNode(sourcePath);
            if (sourceNode == null)
                throw new InvalidOperationException(SR.GetString(SR.Exceptions.Operations.MoveFailed_SouceDoesNotExistWithPath_1, sourcePath));
            var sourceTimestamp = sourceNode.NodeTimestamp;
            Node targetNode = Node.LoadNode(targetPath);
            if (targetNode == null)
                throw new InvalidOperationException(SR.GetString(SR.Exceptions.Operations.MoveFailed_TargetDoesNotExistWithPath_1, targetPath));
            var targetTimestamp = targetNode.NodeTimestamp;
            targetNode.AssertLock();
            sourceNode.MoveTo(targetNode);
        }
        /// <summary>
        /// Moves the <see cref="Node"/> instance to another location. The new location is a <see cref="Node"/> instance 
        /// that will be the parent <see cref="Node"/>.
        /// </summary>
        public virtual void MoveTo(Node target)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            this.AssertLock();
            AssertMoving(target);

            // check permissions
            this.Security.AssertSubtree(PermissionType.Delete);
            target.Security.Assert(PermissionType.Open);
            target.Security.Assert(PermissionType.AddNew);

            var originalPath = this.Path;
            var correctTargetPath = RepositoryPath.Combine(target.Path, RepositoryPath.PathSeparator);
            var correctCurrentPath = RepositoryPath.Combine(this.Path, RepositoryPath.PathSeparator);

            if (correctTargetPath.IndexOf(correctCurrentPath, StringComparison.Ordinal) != -1)
                throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture, "Node cannot be moved under itself."));

            if (target.Id == this.ParentId)
                throw new InvalidOperationException("Node cannot be moved to its parent.");

            var targetPath = RepositoryPath.Combine(target.Path, this.Name);

            Node.AssertPath(targetPath);

            if (Node.Exists(targetPath))
                throw new NodeAlreadyExistsException(String.Concat("Cannot move the content because the target folder already contains a content named '", this.Name, "'."));

            using (var audit = new AuditBlock(AuditEvent.ContentMoved, "Trying to move content.", new Dictionary<string, object>
            {                { "Id", Id }, {"Path", Path }, {"Target", targetPath }            }))
            {
                IDictionary<string, object> customData;
                using (TreeLock.AcquireAsync(CancellationToken.None, this.Path, RepositoryPath.Combine(target.Path, this.Name)).GetAwaiter().GetResult())
                {
                    var args = new CancellableNodeOperationEventArgs(this, target, CancellableNodeEvent.Moving);
                    ContentProtector.AssertIsDeletable(this.Path);
                    FireOnMoving(args);
                    if (args.Cancel)
                        throw new CancelNodeEventException(args.CancelMessage, args.EventType, this);
                    customData = args.GetCustomData();

                    var pathToInvalidate = String.Concat(this.Path, "/");

                    try
                    {
                        DataStore.MoveNodeAsync(this.Data, target.Id, CancellationToken.None)
                            .GetAwaiter().GetResult();
                    }
                    catch (DataOperationException e) // rethrow
                    {
                        if (e.Result == DataOperationResult.DataTooLong)
                            throw new RepositoryPathTooLongException(targetPath);

                        throw new ApplicationException("Cannot move", e);
                    }

                    SecurityHandler.MoveEntity(this.Id, target.Id);

                    PathDependency.FireChanged(pathToInvalidate);
                    PathDependency.FireChanged(this.Path);

                    var populator = SearchManager.GetIndexPopulator();
                    populator.DeleteTreeAsync(this.Path, this.Id, CancellationToken.None).GetAwaiter().GetResult();

                    // <L2Cache>
                    StorageContext.L2Cache.Clear();
                    // </L2Cache>

                    try
                    {
                        var nodeHead = NodeHead.Get(Id);
                        var userAccessLevel = GetUserAccessLevel(nodeHead);
                        var acceptedLevel = GetAcceptedLevel(userAccessLevel, VersionNumber.LastAccessible);
                        var versionId = GetVersionId(nodeHead, acceptedLevel != AccessLevel.Header ? acceptedLevel : AccessLevel.Major, VersionNumber.LastAccessible);

                        var sharedData =  DataStore.LoadNodeAsync(nodeHead, versionId, CancellationToken.None)
                            .GetAwaiter().GetResult();
                        var privateData = NodeData.CreatePrivateData(sharedData.NodeData);
                        SetNodeData(privateData);
                    }
                    catch (Exception e) // logged
                    {
                        SnLog.WriteException(e);
                    }

                    using (new SystemAccount())
                        populator.AddTreeAsync(targetPath, this.Id, CancellationToken.None).GetAwaiter().GetResult();

                } // end lock

                SnLog.WriteAudit(AuditEvent.ContentMoved, GetLoggerPropertiesAfterMove(new object[] { this, originalPath, targetPath }));

                FireOnMoved(target, customData, originalPath);

                audit.Successful = true;
            }
        }

        /// <summary>
        /// Moves the <see cref="Node"/>s identified by the given ids (nodeList) into the target container.
        /// After execution the errors parameter provides <see cref="Exception"/> instances if there were any.
        /// </summary>
        /// <param name="nodeList">List of ids that defines the source <see cref="Node"/>s.</param>
        /// <param name="targetPath">Path of the target container.</param>
        /// <param name="errors">List of <see cref="Exception"/>s to collect errors.</param>
        public static void MoveMore(List<Int32> nodeList, string targetPath, ref List<Exception> errors)
        {
            MoveMoreInternal2(new NodeList<Node>(nodeList), Node.LoadNode(targetPath), ref errors);
            return;
        }

        private static void MoveMoreInternal2(NodeList<Node> sourceNodes, Node target, ref  List<Exception> errors)
        {
            if (target == null)
                throw new ArgumentNullException("target");
            foreach (var sourceNode in sourceNodes)
            {
                try
                {
                    sourceNode.MoveTo(target);
                }
                catch (Exception e) // not logged, not thrown
                {
                    errors.Add(e);
                }
            }
        }

        private static IDictionary<string, object> GetLoggerPropertiesAfterMove(object[] args)
        {
            var node = (Node)args[0];
            return new Dictionary<string, object>
            {
                {"Id", node.Id},
                {"Path", node.Path},
                {"OriginalPath", args[1]},
            }; 
        }

        #endregion

        #region // ================================================================================================= Copy methods

        /// <summary>
        /// Copy the <see cref="Node"/> identified by the source path to another location. 
        /// The destination <see cref="Node"/> is also identified by path. 
        /// </summary>
        /// <remarks>Use this method if you do not want to instantiate the <see cref="Node"/>s.</remarks>
        public static void Copy(string sourcePath, string targetPath)
        {
            Node sourceNode = Node.LoadNode(sourcePath);
            if (sourceNode == null)
                throw new InvalidOperationException(SR.GetString(SR.Exceptions.Operations.CopyFailed_SouceDoesNotExistWithPath_1, sourcePath));
            Node targetNode = Node.LoadNode(targetPath);
            if (targetNode == null)
                throw new InvalidOperationException(SR.GetString(SR.Exceptions.Operations.CopyFailed_TargetDoesNotExistWithPath_1, targetPath));
            sourceNode.CopyTo(targetNode);
        }

        /// <summary>
        /// Copies the <see cref="Node"/>s identified by the given ids (nodeList) into the target container.
        /// After execution the errors parameter provides <see cref="Exception"/> instances if there were any.
        /// </summary>
        /// <param name="nodeList">List of ids that defines the source <see cref="Node"/>s.</param>
        /// <param name="targetPath">Path of the target container.</param>
        /// <param name="errors">List of <see cref="Exception"/>s to collect errors.</param>
        public static void Copy(List<int> nodeList, string targetPath, ref List<Exception> errors)
        {
            if (nodeList == null)
                throw new ArgumentNullException("nodeList");
            if (nodeList.Count == 0)
                return;

            CopyMoreInternal(nodeList, targetPath, ref errors);
        }
        private static void CopyMoreInternal(IEnumerable<int> nodeList, string targetNodePath, ref List<Exception> errors)
        {
            var col2 = new List<Node>();

            var targetNode = LoadNode(targetNodePath);
            if (targetNode == null)
                throw new InvalidOperationException(SR.GetString(SR.Exceptions.Operations.CopyFailed_TargetDoesNotExistWithPath_1, targetNodePath));

            using (var op = SnTrace.ContentOperation.StartOperation("Node.CopyMoreInternal"))
            {
                // check copy conditions
                foreach (var nodeId in nodeList)
                {
                    var n = LoadNode(nodeId);
                    if (n == null)    // node has already become unavailable
                        continue;
                    var msg = n.CheckListAndItemCopyingConditions(targetNode);
                    if (msg == null)
                    {
                        col2.Add(n);
                        continue;
                    }
                    errors.Add(new InvalidOperationException(msg));
                }
                var nodesToRemove = new List<int>();
                string correctTargetPath;
                foreach (var node in col2)
                {
                    correctTargetPath = RepositoryPath.Combine(targetNode.Path, RepositoryPath.PathSeparator);
                    var correctCurrentPath = RepositoryPath.Combine(node.Path, RepositoryPath.PathSeparator);

                    if (correctTargetPath.IndexOf(correctCurrentPath) == -1)
                        continue;
                    errors.Add(new InvalidOperationException(String.Format("Node cannot be copied under itself: {0}.", correctCurrentPath)));
                    nodesToRemove.Add(node.Id);
                }
                col2.RemoveAll(n => nodesToRemove.Contains(n.Id));
                nodesToRemove.Clear();

                targetNode.AssertLock();

                // fire copying and cancel events
                var customDataDictionary = new Dictionary<int, IDictionary<string, object>>();
                foreach (var node in col2)
                {
                    var args = new CancellableNodeOperationEventArgs(node, targetNode, CancellableNodeEvent.Copying);
                    node.FireOnCopying(args);
                    if (!args.Cancel)
                    {
                        customDataDictionary.Add(node.Id, args.GetCustomData());
                        continue;
                    }
                    errors.Add(new CancelNodeEventException(args.CancelMessage, args.EventType, node));
                    nodesToRemove.Add(node.Id);
                }
                col2.RemoveAll(n => nodesToRemove.Contains(n.Id));
                nodesToRemove.Clear();

                //  copying
                var targetChildren = targetNode.GetChildren();
                var targetNodeId = targetNode.Id;
                foreach (var node in col2)
                {
                    var originalPath = node.Path;
                    var newName = node.Name;
                    var targetName = newName;

                    int i = 0;

                    try
                    {
                        while (NameExists(targetChildren, targetName))
                        {
                            if (targetNodeId != node.ParentId)
                                throw new ApplicationException(String.Concat("This folder already contains a content named '", newName, "'."));
                            targetName = node.GenerateCopyName(i++);
                        }
                        correctTargetPath = RepositoryPath.Combine(targetNode.Path, RepositoryPath.PathSeparator);
                        var newPath = correctTargetPath + targetName;

                        node.DoCopy(newPath, targetName);

                        SnTrace.ContentOperation.Write($"Node copied. NodeId:{node.Id}, Path:{node.Path}, OriginalPath:{originalPath}, NewPath:{newPath}");

                        node.FireOnCopied(targetNode, customDataDictionary[node.Id]);
                    }
                    catch (Exception e)
                    {
                        errors.Add(e);
                    }
                }

                op.Successful = true;
            }
        }

        /// <summary>
        /// Copies the <see cref="Node"/> instance to another location. The new location is 
        /// a <see cref="Node"/> instance that will be the parent <see cref="Node"/>.
        /// </summary>
        public virtual void CopyTo(Node target)
        {
            CopyTo(target, this.Name);
        }
        /// <summary>
        /// Copies the <see cref="Node"/> instance to another location with the provided new name. 
        /// The new location is the target <see cref="Node"/> instance that will be the parent <see cref="Node"/>.
        /// </summary>
        public virtual void CopyTo(Node target, string newName)
        {
            CopyToAndGetCopy(target, newName);
        }

        /// <summary>
        /// Copies the <see cref="Node"/> instance to another location. The new location is 
        /// a <see cref="Node"/> instance that will be the parent <see cref="Node"/>.
        /// </summary>
        /// <returns>The new node instance.</returns>
        public Node CopyToAndGetCopy(Node target)
        {
            return CopyToAndGetCopy(target, this.Name);
        }
        /// <summary>
        /// Copies the <see cref="Node"/> instance to another location with the provided new name. 
        /// The new location is the target <see cref="Node"/> instance that will be the parent <see cref="Node"/>.
        /// </summary>
        /// <returns>The new node instance.</returns>
        public Node CopyToAndGetCopy(Node target, string newName)
        {

            using (var op = SnTrace.ContentOperation.StartOperation("Node.SaveCopied"))
            {
                if (target == null)
                {
                    throw new ArgumentNullException("target");
                }
                string msg = CheckListAndItemCopyingConditions(target);
                if (msg != null)
                {
                    throw new InvalidOperationException(msg);
                }
                var originalPath = this.Path;
                string newPath;
                var correctTargetPath = RepositoryPath.Combine(target.Path, RepositoryPath.PathSeparator);
                var correctCurrentPath = RepositoryPath.Combine(this.Path, RepositoryPath.PathSeparator);

                if (correctTargetPath.IndexOf(correctCurrentPath) != -1)
                {
                    throw new InvalidOperationException("Node cannot be copied under itself.");
                }
                target.AssertLock();

                var args = new CancellableNodeOperationEventArgs(this, target, CancellableNodeEvent.Copying);
                FireOnCopying(args);

                if (args.Cancel)
                {
                    throw new CancelNodeEventException(args.CancelMessage, args.EventType, this);
                }
                var customData = args.GetCustomData();
                var targetName = newName;
                int i = 0;
                var nodeList = target.GetChildren();
                while (NameExists(nodeList, targetName))
                {
                    if (target.Id != this.ParentId)
                        throw new NodeAlreadyExistsException(String.Concat("Cannot copy the content because the target folder already contains a content named '", this.Name, "'."));
                    targetName = GenerateCopyName(i++);
                }
                newPath = correctTargetPath + targetName;

                var copyOfSource = DoCopyAndGetCopy(newPath, targetName);

                SnTrace.ContentOperation.Write($"Node copied. NodeId:{this.Id}, Path:{this.Path}, OriginalPath:{originalPath}, NewPath:{newPath}");
                FireOnCopied(target, customData);
                op.Successful = true;
                return copyOfSource;
            }
        }

        private string CheckListAndItemCopyingConditions(Node target)
        {
            var sourceIsOuter = this.ContentListType == null;
            var sourceIsList = !sourceIsOuter && this.ContentListId == 0;
            var targetIsOuter = target.ContentListType == null;
            if (sourceIsList && !targetIsOuter)
                return "Cannot copy a list into an another list. ";
            return null;
        }

        private string GenerateCopyName(int index)
        {
            if (index == 0)
                return String.Concat("Copy of ", this.Name);
            return String.Concat("Copy (", index, ") of ", this.Name);
        }
        private void DoCopy(string targetPath, string newName)
        {
            DoCopyAndGetCopy(targetPath, newName);
        }

        private Node DoCopyAndGetCopy(string targetPath, string newName)
        {
            bool first = true;
            var sourcePath = this.Path;
            Node copyOfSource = null;
            if (!Node.Exists(sourcePath))
                throw new ContentNotFoundException(sourcePath);
            foreach (var sourceNode in NodeEnumerator.GetNodes(sourcePath, ExecutionHint.ForceRelationalEngine))
            {
                var targetNodePath = targetPath + sourceNode.Path.Substring(sourcePath.Length);
                targetNodePath = RepositoryPath.GetParentPath(targetNodePath);
                var targetNode = Node.LoadNode(targetNodePath);
                var copy = sourceNode.MakeCopy(targetNode, newName);
                copy.Save();
                CopyExplicitPermissionsTo(sourceNode, copy);
                if (first)
                {
                    copyOfSource = copy;
                    newName = null;
                    first = false;
                }
            }
            return copyOfSource;
        }

        private void CopyExplicitPermissionsTo(Node sourceNode, Node targetNode)
        {
            AccessProvider.ChangeToSystemAccount();
            try
            {
                var entriesToCopy = SecurityHandler.GetExplicitEntriesAsSystemUser(sourceNode.Id, null, EntryType.Normal);
                if (entriesToCopy.Count == 0)
                    return;

                var aclEd = SecurityHandler.CreateAclEditor();
                foreach (var entry in entriesToCopy)
                    aclEd.Set(targetNode.Id, entry.IdentityId, entry.LocalOnly, entry.AllowBits, entry.DenyBits);
                aclEd.Apply();

                if (!targetNode.IsInherited)
                    targetNode.Security.RemoveBreakInheritance();
            }
            finally
            {
                AccessProvider.RestoreOriginalUser();
            }
        }

        /// <summary>
        /// Make a new node instance under the provided parent based on the property values of the current node.
        /// </summary>
        public Node MakeTemplatedCopy(Node target, string newName)
        {
            var copy = MakeCopy(target, newName);
            copy._copying = false;

            return copy;
        }

        /// <summary>
        /// Make a new node instance under the provided parent based on the property values of the current node.
        /// </summary>
        public virtual Node MakeCopy(Node target, string newName)
        {
            var copy = this.NodeType.CreateInstance(target);
            copy._copying = true;
            var name = newName ?? this.Name;
            var path = RepositoryPath.Combine(target.Path, name);

            Node.AssertPath(path);

            copy.Data.Name = name;
            copy.Data.Path = path;
            this.Data.CopyGeneralPropertiesTo(copy.Data);
            CopyDynamicProperties(copy);

            // These properties must be copied this way. 
            copy["VersioningMode"] = this["VersioningMode"];
            copy["InheritableVersioningMode"] = this["InheritableVersioningMode"];
            copy["ApprovingMode"] = this["ApprovingMode"];
            copy["InheritableApprovingMode"] = this["InheritableApprovingMode"];

            var now = DateTime.UtcNow;
            copy.SetCreationDate(now);
            copy.ModificationDate = now;
            copy.SetVersionCreationDate(now);
            copy.VersionModificationDate = now;

            return copy;
        }
        /// <summary>
        /// Copy dynamic properties of the current node to the inner data storage of the provided target node.
        /// </summary>
        protected virtual void CopyDynamicProperties(Node target)
        {
            this.Data.CopyDynamicPropertiesTo(target.Data);
        }
        #endregion

        #region // ==========================================================================Templated creation

        private Node _template;
        /// <summary>
        /// Gets or sets the original template that this node was based on.
        /// </summary>
        public Node Template
        {
            get { return _template; }
            set { _template = value; }
        }

        #endregion

        #region // ================================================================================================= Delete methods

        /// <summary>
        /// Deletes a <see cref="Node"/> and all of its contents from the database. This operation removes all child <see cref="Node"/>s too.
        /// </summary>
        /// <param name="sourcePath">The path of the <see cref="Node"/> that will be deleted.</param>
        [Obsolete("DeletePhysical is obsolete. Use ForceDelete to delete Node permanently.")]
        public static void DeletePhysical(string sourcePath)
        {
            var sourceNode = Node.LoadNode(sourcePath);
            if (sourceNode == null)
                throw new InvalidOperationException(SR.GetString(SR.Exceptions.Operations.DeleteFailed_ContentDoesNotExistWithPath_1, sourcePath));
            sourceNode.Delete();
        }
        /// <summary>
        /// Deletes a <see cref="Node"/> and all of its contents from the database. This operation removes all child <see cref="Node"/>s too.
        /// </summary>
        /// <param name="nodeId">Identifier of the <see cref="Node"/> that will be deleted.</param>
        [Obsolete("DeletePhysical is obsolete. Use ForceDelete to delete Node permanently.")]
        public static void DeletePhysical(int nodeId)
        {
            var sourceNode = Node.LoadNode(nodeId);
            if (sourceNode == null)
                throw new InvalidOperationException(SR.GetString(SR.Exceptions.Operations.DeleteFailed_ContentDoesNotExistWithId_1, nodeId));
            sourceNode.Delete();
        }
        /// <summary>
        /// Deletes the <see cref="Node"/> instance and all of its contents. This operation removes the appropriate <see cref="Node"/>s from the database.
        /// </summary>
        [Obsolete("The DeletePhysical is obsolete. Use ForceDelete to delete Node permanently.")]
        public virtual void DeletePhysical()
        {
            Delete();
        }

        /// <summary>
        /// Deletes the <see cref="Node"/> specified by the given path and its whole subtree physically.
        /// </summary>
        public static void ForceDelete(string sourcePath)
        {
            var sourceNode = Node.LoadNode(sourcePath);
            if (sourceNode == null)
                throw new InvalidOperationException(SR.GetString(SR.Exceptions.Operations.DeleteFailed_ContentDoesNotExistWithPath_1, sourcePath));
            sourceNode.ForceDelete();
        }

        /// <summary>
        /// Deletes the <see cref="Node"/> specified by the given id and its whole subtree physically.
        /// </summary>
        public static void ForceDelete(int nodeId)
        {
            var sourceNode = Node.LoadNode(nodeId);
            if (sourceNode == null)
                throw new InvalidOperationException(SR.GetString(SR.Exceptions.Operations.DeleteFailed_ContentDoesNotExistWithId_1, nodeId));
            sourceNode.ForceDelete();
        }

        /// <summary>
        /// This method deletes the <see cref="Node"/> permanently.
        /// </summary>
        public virtual void ForceDelete()
        {
            using (var op = SnTrace.ContentOperation.StartOperation("Node.ForceDelete: Id:{0}, Path:{1}", Id, Path))
            {
                this.Security.AssertSubtree(PermissionType.Delete);

                this.AssertLock();
                AssertDeletion();

                var myPath = Path;
                using (var audit = new AuditBlock(AuditEvent.ContentDeleted, "Trying to delete the content.",
                    new Dictionary<string, object> { { "Id", this.Id }, { "Path", myPath } }))
                {

                    var args = new CancellableNodeEventArgs(this, CancellableNodeEvent.DeletingPhysically);
                    FireOnDeletingPhysically(args);
                    if (args.Cancel)
                        throw new CancelNodeEventException(args.CancelMessage, args.EventType, this);
                    var customData = args.GetCustomData();

                    var contentListTypesInTree = (this is IContentList)
                        ? new List<ContentListType>(new[] {this.ContentListType})
                        : DataStore.GetContentListTypesInTreeAsync(this.Path, CancellationToken.None)
                        .GetAwaiter().GetResult();

                    var logProps = CollectAllProperties(this.Data);
                    var oldPath = this.Path;
                    var deletingAttempt = 0;
                    while (true)
                    {
                        if (this.Path != oldPath)
                            // content was moved
                            throw new ContentNotFoundException(oldPath);
                        try
                        {
                            // prevent concurrency problems
                            using (TreeLock.AcquireAsync(CancellationToken.None, this.Path).GetAwaiter().GetResult())
                            {
                                // main work
                                DataStore.DeleteNodeAsync(Data, CancellationToken.None).GetAwaiter().GetResult();
                            }

                            // successful
                            break;
                        }
                        catch (NodeIsOutOfDateException)
                        {
                            // this node is obsolete
                            if (deletingAttempt++ > 3)
                                // exit if there were too many attempts
                                throw;
                            System.Threading.Thread.Sleep(10);
                            // getting a newer version.
                            Reload();
                        }
                        catch (Exception e) // rethrow
                        {
                            if (e.Message.Contains("DELETE statement conflicted with the REFERENCE constraint"))
                            {
                                int totalCountOfReferrers;
                                var referrers = GetReferrers(5, out totalCountOfReferrers);
                                throw new CannotDeleteReferredContentException(referrers, totalCountOfReferrers);
                            }
                            throw new ApplicationException("You cannot delete this content", e);
                        }
                    }

                    MakePrivateData();
                    this._data.IsDeleted = true;

                    var hadContentList = RemoveContentListTypesInTree(contentListTypesInTree) > 0;

                    if (this.Id > 0)
                        SecurityHandler.DeleteEntity(this.Id);

                    SearchManager.GetIndexPopulator().DeleteTreeAsync(myPath, this.Id, CancellationToken.None).GetAwaiter().GetResult();

                    if (hadContentList)
                        FireAnyContentListDeleted();

                    PathDependency.FireChanged(myPath);

                    SnLog.WriteAudit(AuditEvent.ContentDeleted, logProps);
                    FireOnDeletedPhysically(customData);

                    audit.Successful = true;
                }
                op.Successful = true;
            }
        }
        /// <summary>
        /// This method deletes the <see cref="Node"/> permanently.
        /// </summary>
        [Obsolete("Use parameterless ForceDelete method.")]
        public virtual void ForceDelete(long timestamp)
        {
            ForceDelete();
        }
        /// <summary>
        /// Provides a customizable base method for querying all referrers.
        /// In this class returns null, and totalCount output is 0.
        /// </summary>
        /// <param name="top">Defines the maximum count of items in the result set.</param>
        /// <param name="totalCount">Provides total count of hits independently from the value of the "top" parameter.</param>
        /// <returns></returns>
        protected virtual IEnumerable<Node> GetReferrers(int top, out int totalCount)
        {
            totalCount = 0;
            return null;
        }

        private int RemoveContentListTypesInTree(List<ContentListType> contentListTypesInTree)
        {
            // retry is necessary here because under heavy load the data layer
            // may throw an exception if the storage schema is out of date.
            return Retrier.Retry(10, 200, typeof (DataException), () =>
            {
                var count = 0;
                if (contentListTypesInTree.Count > 0)
                {
                    var editor = new SchemaEditor();
                    editor.Load();
                    foreach (var t in contentListTypesInTree)
                    {
                        if (t != null)
                        {
                            editor.DeleteContentListType(editor.ContentListTypes[t.Name]);
                            count++;
                        }
                    }
                    editor.Register();
                }
                return count;
            });
        }

        /// <summary>
        /// Deletes the node.
        /// </summary>
        public static void Delete(string sourcePath)
        {
            var sourceNode = Node.LoadNode(sourcePath);
            if (sourceNode == null)
                throw new InvalidOperationException(SR.GetString(SR.Exceptions.Operations.DeleteFailed_ContentDoesNotExistWithPath_1, sourcePath));
            sourceNode.Delete();
        }

        /// <summary>
        /// Deletes the node.
        /// </summary>
        public static void Delete(int nodeId)
        {
            var sourceNode = Node.LoadNode(nodeId);
            if (sourceNode == null)
                throw new InvalidOperationException(SR.GetString(SR.Exceptions.Operations.DeleteFailed_ContentDoesNotExistWithId_1, nodeId));
            sourceNode.Delete();
        }

        /// <summary>
        /// Batch delete.
        /// </summary>
        /// <param name="nodeList">Represents an Id collection which holds the identifiers of the <see cref="Node"/>s will be deleted.</param>
        /// <param name="errors">If any error occurres, it is added to the errors collection passed by errors parameter.</param>
        /// <exception cref="ArgumentNullException">You must specify a list collection instance.</exception>
        public static void Delete(List<int> nodeList, ref List<Exception> errors)
        {
            if (nodeList == null)
                throw new ArgumentNullException("nodeList");
            if (nodeList.Count == 0)
                return;
            Node.DeleteMoreInternal(nodeList, ref errors);
        }

        // TODO: need to consider> method based upon the original DeleteInternal, this contains duplicated source code
        private static void DeleteMoreInternal(ICollection<Int32> nodeList, ref List<Exception> errors)
        {
            if (nodeList == null)
                throw new ArgumentNullException("nodeList");
            if (nodeList.Count == 0)
                return;

            var col2 = new List<Node>();
            var col3 = new List<Node>();

            foreach (Int32 n in nodeList)
            {
                var node = LoadNode(n);
                try
                {
                    node.Security.AssertSubtree(PermissionType.Delete);
                    node.AssertLock();
                    node.AssertDeletion();
                }
                catch (Exception e)
                {
                    errors.Add(e);
                    continue;
                }
                col2.Add(node);
            }

            var customDataDictionary = new Dictionary<int, IDictionary<string, object>>();
            foreach (var nodeRef in col2)
            {
                var internalError = false;
                var myPath = nodeRef.Path;

                var args = new CancellableNodeEventArgs(nodeRef, CancellableNodeEvent.DeletingPhysically);
                nodeRef.FireOnDeletingPhysically(args);
                if (args.Cancel)
                    throw new CancelNodeEventException(args.CancelMessage, args.EventType, nodeRef);
                customDataDictionary.Add(nodeRef.Id, args.GetCustomData());

                try
                {
                    using (TreeLock.AcquireAsync(CancellationToken.None, nodeRef.Path).GetAwaiter().GetResult())
                        DataStore.DeleteNodeAsync(nodeRef.Data, CancellationToken.None).GetAwaiter().GetResult();
                }
                catch (Exception e) // rethrow
                {
                    var msg = new StringBuilder("You cannot delete this content ");
                    if (e.Message.Contains("DELETE statement conflicted with the REFERENCE constraint"))
                        msg.Append("because it is referenced by another content.");
                    internalError = true;
                    errors.Add(new ApplicationException(msg.ToString(), e));
                }
                if (internalError)
                    continue;

                col3.Add(nodeRef);

                nodeRef.MakePrivateData();
                nodeRef._data.IsDeleted = true;

                if (nodeRef is IContentList)
                {
                    if (nodeRef.ContentListType != null)
                    {
                        var editor = new SchemaEditor();
                        editor.Load();
                        editor.DeleteContentListType(editor.ContentListTypes[nodeRef.ContentListType.Name]);
                        editor.Register();
                    }
                }

                if (nodeRef.Id > 0)
                    SecurityHandler.DeleteEntity(nodeRef.Id);
            }

            var ids = new List<Int32>();
            for (int index = 0; index < col3.Count; index++)
            {
                var n = col3[index];
                ids.Add(n.Id);
            }
            try
            {
                SearchManager.GetIndexPopulator().DeleteForestAsync(ids, CancellationToken.None).GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                errors.Add(e);
            }


            for (int index = 0; index < col3.Count; index++)
            {
                var n = col3[index];
                PathDependency.FireChanged(n.Path);
                n.FireOnDeletedPhysically(customDataDictionary[n.Id]);
            }
        }

        /// <summary>
        /// Deletes the current node.
        /// </summary>
        public virtual void Delete()
        {
            ForceDelete();
        }

        #endregion

        /// <summary>
        /// Provides an algorithm based on the creation date for generating a password salt for users. 
        /// Derived classes may provide a custom algorithm here.
        /// </summary>
        public virtual string GetPasswordSalt()
        {
            return this.CreationDate.ToString("yyyyMMddHHmmssff", CultureInfo.InvariantCulture);
        }

        #region // ================================================================================================= Events

        private IEventDistributor EventDistributor => Providers.Instance.EventDistributor;

        private List<Type> _disabledObservers;
        /// <summary>
        /// Gets the collection of the disabled <see cref="NodeObserver"/> types.
        /// </summary>
        public IEnumerable<Type> DisabledObservers { get { return _disabledObservers; } }
        /// <summary>
        /// Disables a <see cref="NodeObserver"/> specified by its type.
        /// </summary>
        public void DisableObserver(Type observerType)
        {
            if (observerType == null)
                return;
            if (!observerType.IsSubclassOf(typeof(NodeObserver)))
                throw new InvalidOperationException("You can only disable types that are derived from NodeObserver.");

            if (_disabledObservers == null)
                _disabledObservers = new List<Type>();
            if (!_disabledObservers.Contains(observerType))
                _disabledObservers.Add(observerType);
        }

        /// <summary>
        /// Occurs before this <see cref="Node"/> instance is saved for the first time.
        /// </summary>
        public event CancellableNodeEventHandler Creating;
        /// <summary>
        /// Occurs after this <see cref="Node"/> instance is saved for the first time.
        /// </summary>
        public event EventHandler<NodeEventArgs> Created;
        /// <summary>
        /// Occurs before this <see cref="Node"/> instance is saved.
        /// </summary>
        public event CancellableNodeEventHandler Modifying;
        /// <summary>
        /// Occurs after this <see cref="Node"/> instance is saved.
        /// </summary>
        public event EventHandler<NodeEventArgs> Modified;
        /// <summary>
        /// Occurs before this <see cref="Node"/> instance is deleted temporarily.
        /// </summary>
        public event CancellableNodeEventHandler Deleting;
        /// <summary>
        /// Occurs after this <see cref="Node"/> instance is deleted temporarily.
        /// </summary>
        public event EventHandler<NodeEventArgs> Deleted;
        /// <summary>
        /// Occurs before this <see cref="Node"/> instance is deleted physically.
        /// </summary>
        public event CancellableNodeEventHandler DeletingPhysically;
        /// <summary>
        /// Occurs after this <see cref="Node"/> instance is deleted physically.
        /// </summary>
        public event EventHandler<NodeEventArgs> DeletedPhysically;
        /// <summary>
        /// Occurs before this <see cref="Node"/> instance is moved to another place.
        /// </summary>
        public event CancellableNodeOperationEventHandler Moving;
        /// <summary>
        /// Occurs after this <see cref="Node"/> instance is moved to another place.
        /// </summary>
        public event EventHandler<NodeOperationEventArgs> Moved;
        /// <summary>
        /// Occurs before this <see cref="Node"/> instance is copied to another place.
        /// </summary>
        public event CancellableNodeOperationEventHandler Copying;
        /// <summary>
        /// Occurs after this <see cref="Node"/> instance is copied to another place.
        /// </summary>
        public event EventHandler<NodeOperationEventArgs> Copied;
        /// <summary>
        /// Occurs before this <see cref="Node"/> instance's permission setting is changed.
        /// </summary>
#pragma warning disable 67
        [Obsolete("Do not use this event anymore.")]
        public event CancellableNodeEventHandler PermissionChanging;
#pragma warning restore 67
        /// <summary>
        /// Occurs after this <see cref="Node"/> instance's permission setting is changed.
        /// </summary>
#pragma warning disable 67
        [Obsolete("Do not use this event anymore.")]
        public event EventHandler<PermissionChangedEventArgs> PermissionChanged;
#pragma warning restore 67

        //TODO: public event EventHandler Undeleted;
        //TODO: public event EventHandler Locked;
        //TODO: public event EventHandler Unlocked;
        //TODO: public event EventHandler LockRemoved;

        private void FireOnCreating(CancellableNodeEventArgs e)
        {
            OnCreating(this, e);
            if (e.Cancel)
                return;
            NodeObserver.FireOnNodeCreating(Creating, this, e, _disabledObservers);

            var canceled = EventDistributor.FireCancellableNodeObserverEventEventAsync(
                    new NodeCreatingEvent(e), _disabledObservers)
                .ConfigureAwait(false).GetAwaiter().GetResult();
            if (canceled)
                e.Cancel = true;

        }
        private void FireOnCreated(IDictionary<string, object> customData)
        {
            NodeEventArgs e = new NodeEventArgs(this, NodeEvent.Created, customData);
            OnCreated(this, e);
            NodeObserver.FireOnNodeCreated(Created, this, e, _disabledObservers);

            EventDistributor.FireNodeObserverEventEventAsync(new NodeCreatedEvent(e), _disabledObservers)
                .ConfigureAwait(false).GetAwaiter().GetResult();
        }
        private void FireOnModifying(CancellableNodeEventArgs e)
        {
            OnModifying(this, e);
            if (e.Cancel)
                return;
            NodeObserver.FireOnNodeModifying(Modifying, this, e, _disabledObservers);

            var canceled = EventDistributor.FireCancellableNodeObserverEventEventAsync(
                    new NodeModifyingEvent(e), _disabledObservers)
                .ConfigureAwait(false).GetAwaiter().GetResult();
            if(canceled)
                e.Cancel = true;
        }
        private void FireOnModified(string originalSourcePath, IDictionary<string, object> customData, IEnumerable<ChangedData> changedData)
        {
            NodeEventArgs e = new NodeEventArgs(this, NodeEvent.Modified, customData, originalSourcePath, changedData);
            OnModified(this, e);
            NodeObserver.FireOnNodeModified(Modified, this, e, _disabledObservers);

            EventDistributor.FireNodeObserverEventEventAsync(new NodeModifiedEvent(e), _disabledObservers)
                .ConfigureAwait(false).GetAwaiter().GetResult();
        }
        private void FireOnDeleting(CancellableNodeEventArgs e)
        {
            OnDeleting(this, e);
            if (e.Cancel)
                return;
            NodeObserver.FireOnNodeDeleting(Deleting, this, e, _disabledObservers);

            var canceled = EventDistributor.FireCancellableNodeObserverEventEventAsync(
                    new NodeDeletingEvent(e), _disabledObservers)
                .ConfigureAwait(false).GetAwaiter().GetResult();
            if (canceled)
                e.Cancel = true;
        }
        private void FireOnDeleted(IDictionary<string, object> customData)
        {
            NodeEventArgs e = new NodeEventArgs(this, NodeEvent.Deleted, customData);
            OnDeleted(this, e);
            NodeObserver.FireOnNodeDeleted(Deleted, this, e, _disabledObservers);

            EventDistributor.FireNodeObserverEventEventAsync(new NodeDeletedEvent(e), _disabledObservers)
                .ConfigureAwait(false).GetAwaiter().GetResult();
        }
        private void FireOnDeletingPhysically(CancellableNodeEventArgs e)
        {
            OnDeletingPhysically(this, e);
            if (e.Cancel)
                return;
            NodeObserver.FireOnNodeDeletingPhysically(DeletingPhysically, this, e, _disabledObservers);

            var canceled = EventDistributor.FireCancellableNodeObserverEventEventAsync(
                    new NodeForcedDeletingEvent(e), _disabledObservers)
                .ConfigureAwait(false).GetAwaiter().GetResult();
            if (canceled)
                e.Cancel = true;
        }
        private void FireOnDeletedPhysically(IDictionary<string, object> customData)
        {
            NodeEventArgs e = new NodeEventArgs(this, NodeEvent.DeletedPhysically, customData);
            OnDeletedPhysically(this, e);
            NodeObserver.FireOnNodeDeletedPhysically(DeletedPhysically, this, e, _disabledObservers);

            EventDistributor.FireNodeObserverEventEventAsync(new NodeForcedDeletedEvent(e), _disabledObservers)
                .ConfigureAwait(false).GetAwaiter().GetResult();
        }
        private void FireOnMoving(CancellableNodeOperationEventArgs e)
        {
            OnMoving(this, e);
            if (e.Cancel)
                return;
            NodeObserver.FireOnNodeMoving(Moving, this, e, _disabledObservers);

            var canceled = EventDistributor.FireCancellableNodeObserverEventEventAsync(
                    new NodeMovingEvent(e), _disabledObservers)
                .ConfigureAwait(false).GetAwaiter().GetResult();
            if (canceled)
                e.Cancel = true;
        }
        private void FireOnMoved(Node targetNode, IDictionary<string, object> customData, string originalSourcePath)
        {
            NodeOperationEventArgs e = new NodeOperationEventArgs(this, targetNode, NodeEvent.Moved, customData, originalSourcePath);
            OnMoved(this, e);
            NodeObserver.FireOnNodeMoved(Moved, this, e, _disabledObservers);

            EventDistributor.FireNodeObserverEventEventAsync(new NodeMovedEvent(e), _disabledObservers)
                .ConfigureAwait(false).GetAwaiter().GetResult();
        }
        private void FireOnCopying(CancellableNodeOperationEventArgs e)
        {
            OnCopying(this, e);
            if (e.Cancel)
                return;
            NodeObserver.FireOnNodeCopying(Copying, this, e, _disabledObservers);

            var canceled = EventDistributor.FireCancellableNodeObserverEventEventAsync(
                    new NodeCopyingEvent(e), _disabledObservers)
                .ConfigureAwait(false).GetAwaiter().GetResult();
            if (canceled)
                e.Cancel = true;
        }
        private void FireOnCopied(Node targetNode, IDictionary<string, object> customData)
        {
            NodeOperationEventArgs e = new NodeOperationEventArgs(this, targetNode, NodeEvent.Copied, customData);
            OnCopied(this, e);
            NodeObserver.FireOnNodeCopied(Copied, this, e, _disabledObservers);

            EventDistributor.FireNodeObserverEventEventAsync(new NodeCopiedEvent(e), _disabledObservers)
                .ConfigureAwait(false).GetAwaiter().GetResult();
        }
        private void FireOnLoaded()
        {
            var e = new NodeEventArgs(this, NodeEvent.Loaded, null);
            OnLoaded(this, e);
        }

        /// <summary>
        /// Raises the <see cref="Creating"/> event.
        /// </summary>
        protected virtual void OnCreating(object sender, CancellableNodeEventArgs e) { }
        /// <summary>
        /// Raises the <see cref="Created"/> event.
        /// </summary>
        protected virtual void OnCreated(object sender, NodeEventArgs e) { }
        /// <summary>
        /// Raises the <see cref="Modifying"/> event.
        /// </summary>
        protected virtual void OnModifying(object sender, CancellableNodeEventArgs e) { }
        /// <summary>
        /// Raises the <see cref="Modified"/> event.
        /// </summary>
        protected virtual void OnModified(object sender, NodeEventArgs e) { }
        /// <summary>
        /// Raises the <see cref="Deleting"/> event.
        /// </summary>
        protected virtual void OnDeleting(object sender, CancellableNodeEventArgs e) { }
        /// <summary>
        /// Raises the <see cref="Deleted"/> event.
        /// </summary>
        protected virtual void OnDeleted(object sender, NodeEventArgs e) { }
        /// <summary>
        /// Raises the <see cref="DeletingPhysically"/> event.
        /// </summary>
        protected virtual void OnDeletingPhysically(object sender, CancellableNodeEventArgs e) { }
        /// <summary>
        /// Raises the <see cref="DeletedPhysically"/> event.
        /// </summary>
        protected virtual void OnDeletedPhysically(object sender, NodeEventArgs e) { }
        /// <summary>
        /// Raises the <see cref="Moving"/> event.
        /// </summary>
        protected virtual void OnMoving(object sender, CancellableNodeOperationEventArgs e) { }
        /// <summary>
        /// Raises the <see cref="Moved"/> event.
        /// </summary>
        protected virtual void OnMoved(object sender, NodeOperationEventArgs e) { }
        /// <summary>
        /// Raises the <see cref="Copying"/> event.
        /// </summary>
        protected virtual void OnCopying(object sender, CancellableNodeOperationEventArgs e) { }
        /// <summary>
        /// Raises the <see cref="Copied"/> event.
        /// </summary>
        protected virtual void OnCopied(object sender, NodeOperationEventArgs e) { }
        /// <summary>
        /// Provides an overridable method that is called immediately after this instance is loaded in memory.
        /// Note that the data is not filled before calling.
        /// </summary>
        protected virtual void OnLoaded(object sender, NodeEventArgs e) { }
        /// <summary>
        /// Raises the <see cref="PermissionChanging"/> event.
        /// </summary>
        [Obsolete("Do not use this method anymore.")]
        protected virtual void OnPermissionChanging(object sender, CancellablePermissionChangingEventArgs e) { }
        /// <summary>
        /// Raises the <see cref="PermissionChanged"/> event.
        /// </summary>
        [Obsolete("Do not use this method anymore.")]
        protected virtual void OnPermissionChanged(object sender, PermissionChangedEventArgs e) { }

        #endregion

        /// <summary>
        /// Occurs after any ContentList is deleted.
        /// </summary>
        public static event EventHandler AnyContentListDeleted;
        private void FireAnyContentListDeleted()
        {
            if (AnyContentListDeleted != null)
                AnyContentListDeleted(this, EventArgs.Empty);
        }

        private void AssertSaving(IEnumerable<ChangedData> changedData)
        {
            var validators = NodeOperationValidators;
            if (validators == null)
                return;
            var data = changedData?.ToArray();
            foreach (var validator in validators)
                if (!validator.CheckSaving(this, data, out var errorMessage))
                    throw new InvalidOperationException(errorMessage);
        }
        private void AssertMoving(Node target)
        {
            ContentProtector.AssertIsDeletable(this.Path);

            var validators = NodeOperationValidators;
            if (validators == null)
                return;

            foreach (var validator in validators)
                if (!validator.CheckMoving(this, target, out var errorMessage))
                    throw new InvalidOperationException(errorMessage);
        }
        private void AssertDeletion()
        {
            ContentProtector.AssertIsDeletable(this.Path);

            var validators = NodeOperationValidators;
            if (validators == null)
                return;

            foreach (var validator in validators)
                if (!validator.CheckDeletion(this, out var errorMessage))
                    throw new InvalidOperationException(errorMessage);
        }


        #region // ================================================================================================= Public Tools

        /// <summary>
        /// Returns count of level of the given path. For example the "/Root" is 0, "/Root/System" is 1 etc.
        /// </summary>
        public static int GetDepth(string path)
        {
            return path.Count(c => c == '/') - 1;
        }

        /// <summary>
        /// Returns true if the requested property has changed since last loading.
        /// Note that all properties of a newly created <see cref="Node"/> are changed.
        /// </summary>
        /// <param name="propertyName">Name of the requested property.</param>
        public bool IsPropertyChanged(string propertyName)
        {
            return Data.IsPropertyChanged(propertyName);
        }

        /// <summary>
        /// Returns true if the <see cref="Node"/> identified by the given path exists in the database.
        /// </summary>
        public static bool Exists(string path)
        {
            return DataStore.NodeExistsAsync(path, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Returns the ContentList that is a parent of this instance.
        /// The return value is null if this instance is not a list item.
        /// </summary>
        /// <returns></returns>
        public Node LoadContentList()
        {
            if (this.ContentListId == 0)
                return null;
            return Node.LoadNode(this.ContentListId);
        }

        /// <summary>
        /// Returns the nearest <see cref="Node"/> instance on the parent chain that is instance of or derived from the given type.
        /// Note that the type is NodeType and/or ContentType (both types build similar inheritance structures).
        /// </summary>
        /// <param name="child">The <see cref="Node"/> to search a parent for.</param>
        /// <param name="typeName">The name of the desired NodeType and/or ContentType.</param>
        public static Node GetAncestorOfNodeType(Node child, string typeName)
        {
            var node = child;
            while ((node != null) && (!node.NodeType.IsInstaceOfOrDerivedFrom(typeName)))
            {
                var head = NodeHead.Get(node.ParentPath);
                if (head == null)
                    return null;
                if (!SecurityHandler.HasPermission(head, PermissionType.See))
                    return null;
                node = node.Parent;
            }
            return node;
        }

        /// <summary>
        /// Returns the nearest <see cref="Node"/> instance on the parent chain that is instance of or derived from the given type.
        /// </summary>
        /// <typeparam name="T">The desired type that is inherited from <see cref="Node"/>.</typeparam>
        /// <param name="child">The <see cref="Node"/> instance to search an ancestor for.</param>
        /// <returns></returns>
        public static T GetAncestorOfType<T>(Node child) where T : Node
        {
            T ancestor = null;
            var node = child;

            while ((node != null) && ((ancestor = node as T) == null))
            {
                var head = NodeHead.Get(node.ParentPath);
                if (head == null)
                    return null;
                if (!SecurityHandler.HasPermission(head, PermissionType.See))
                    return null;
                node = node.Parent;
            }

            return ancestor;
        }

        /// <summary>
        /// Returns a <see cref="Node"/> on the parent chain from the given position.
        /// 0 means this <see cref="Node"/>, 1 is the parent, 2 is the grandparent, etc.
        /// </summary>
        /// <param name="ancestorIndex">The position in the ancestor chain.</param>
        /// <exception cref="NotSupportedException">Thrown if the parameter's value is less than 0.</exception>
        /// <exception cref="ApplicationException">Thrown if the parameter's value is greater than the number of elements in the ancestor chain.</exception>
        public Node GetAncestor(int ancestorIndex)
        {
            if (ancestorIndex == 0) return this;
            if (ancestorIndex < 0)
                throw new NotSupportedException("AncestorIndex < 0");

            string[] path = this.Path.Split('/');
            if (ancestorIndex >= path.Length)
                throw new ApplicationException("ancestorIndex overflow");

            string ancestorPath = string.Join("/", path, 0, path.Length - ancestorIndex);

            Node ancestor = Node.LoadNode(ancestorPath);

            return ancestor;
        }
        /// <summary>
        /// Returns true if the given <see cref="Node"/> is in the parent chain of the current <see cref="Node"/>
        /// (meaning the current node is in the subtree of the provided ancestor).
        /// </summary>
        public bool IsDescendantOf(Node ancestor)
        {
            return (this.Path.StartsWith(ancestor.Path + "/"));
        }
        /// <summary>
        /// Returns true if the given <see cref="Node"/> is in the parent chain of the current <see cref="Node"/>
        /// (meaning the current node is in the subtree of the provided ancestor).
        /// It also calculates the depth difference between the two nodes.
        /// </summary>
        public bool IsDescendantOf(Node ancestor, out int distance)
        {
            distance = -1;
            if (!IsDescendantOf(ancestor))
                return false;
            distance = this.NodeLevel() - ancestor.NodeLevel();
            return true;
        }

        /// <summary>
        /// Returns the level of hierarchy the <see cref="Node"/> is located at. The virtual Root <see cref="Node"/> has a level of 0.
        /// </summary>
        /// <param name="node"></param>
        public int NodeLevel()
        {
            return this.Path.Split('/').Length - 2;
        }

        /// <summary>
        /// Returns size of this <see cref="Node"/>'s all blobs in bytes.
        /// </summary>
        public long GetFullSize()
        {
            return Node.GetTreeSize(this.Path, false);
        }

        /// <summary>
        /// Returns size of all blobs of all <see cref="Node"/>s in this subtree in bytes.
        /// </summary>
        public long GetTreeSize()
        {
            return Node.GetTreeSize(this.Path, true);
        }

        /// <summary>
        /// Returns size of all blobs of all <see cref="Node"/> in the subtree that is identified by the given path.
        /// </summary>
        public static long GetTreeSize(string path, bool includeChildren = true)
        {
            return DataStore.GetTreeSizeAsync(path, includeChildren, CancellationToken.None).GetAwaiter().GetResult();
        }

        #endregion

        #region // ================================================================================================= Private Tools

        private void AssertSeeOnly(PropertyType propertyType)
        {
            if (IsPreviewOnly && propertyType.DataType == DataType.Binary)
            {
                throw new InvalidOperationException(SR.GetString(SR.Exceptions.General.Error_Preview_BinaryAccess_2, this.Path, propertyType.Name));
            }
            else if (IsHeadOnly && !SeeEnabledProperties.Contains(propertyType.Name)) // && AccessProvider.Current.GetCurrentUser().Id != -1)
            {
                SnTrace.Repository.WriteError($"Invalid property access attempt on a See-only node: {Path}");

                throw new InvalidOperationException(
                    "Invalid property access attempt on a See-only node. " +
                    $"The accessible properties are: {string.Join(", ", SeeEnabledProperties)}.");
            }
        }
        private void AssertLock()
        {
            var userId = AccessProvider.Current.GetCurrentUser().Id;
            if (userId != -1 && (Lock.LockedBy != null && Lock.LockedBy.Id != userId) && Lock.Locked)
                throw new LockedNodeException(Lock);
        }
        private static bool NameExists(IEnumerable<Node> nodeList, string name)
        {
            foreach (Node node in nodeList)
                if (node.Name == name)
                    return true;
            return false;
        }
        private Exception Exception_ReferencedNodeCouldNotBeLoadedException(string referenceCategory, int referenceId, Exception innerException)
        {
            return new InvalidOperationException(String.Format(CultureInfo.InvariantCulture, "The '{0}' could not be loaded because it has been deleted, or the actual user hasn't got sufficient rights to see that node.\nThe NodeId of this node is {1}, the reference id is {2}.", referenceCategory, this.Id, referenceId), innerException);
        }

        private IUser LoadRefUserOrSomebody(int nodeId, string propertyName)
        {
            Node node = null;
            IUser user = null;
            if (nodeId == 0)
                return null;
            var head = NodeHead.Get(nodeId);
            if (head == null)
                return null;
            if (!SecurityHandler.HasPermission(head, PermissionType.See))
                return LoadSomebody();

            try
            {
                node = Node.LoadNode(nodeId);
                user = node as IUser;
            }
            catch (Exception e) // rethrow
            {
                throw Exception_ReferencedNodeCouldNotBeLoadedException(propertyName, nodeId, e);
            }
            if (user != null)
                return user;

            if (node == null)
                throw new ApplicationException("User not found. Content: " + this.Path + ", " + propertyName);
            else
                throw new ApplicationException("'" + propertyName + "' should refer to an IUser.");
        }
        private static IUser LoadSomebody()
        {
            using (new SystemAccount())
            {
                return (IUser)LoadNode(Identifiers.SomebodyUserId);
            }
        }

        internal static void AssertPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;

            if (path.Length > Identifiers.MaximumPathLength)
                throw new RepositoryPathTooLongException(path);
        }

        #endregion

        /*================================================================================================= Linq Tools */

        /// <summary>
        /// Returns true if the given path is the path of this <see cref="Node"/>'s parent's.
        /// </summary>
        public bool InFolder(string path)
        {
            return path.Equals(this.ParentPath, StringComparison.InvariantCultureIgnoreCase);
        }
        /// <summary>
        /// Returns true if this <see cref="Node"/>'s parent is the given <see cref="Node"/>.
        /// </summary>
        public bool InFolder(Node node)
        {
            return InFolder(node.Path);
        }
        /// <summary>
        /// Returns true if this <see cref="Node"/> is in the <see cref="Node"/>'s subtree that is identified by the given path.
        /// </summary>
        public bool InTree(string path)
        {
            if (Path.Equals(path, StringComparison.InvariantCultureIgnoreCase))
                return true;
            if (!path.EndsWith("/"))
                path += "/";
            return Path.StartsWith(path, StringComparison.InvariantCultureIgnoreCase);
        }
        /// <summary>
        /// Returns true if this <see cref="Node"/> is in the given <see cref="Node"/>'s subtree.
        /// </summary>
        public bool InTree(Node node)
        {
            return InTree(node.Path);
        }
        /// <summary>
        /// Returns true if this <see cref="Node"/>'s type is exactly the same as the one identified by the given name.
        /// Note that the type is NodeType and/or ContentType (both types build similar inheritance structures).
        /// </summary>
        public bool Type(string contentTypeName)
        {
            return NodeType.Name == contentTypeName;
        }
        /// <summary>
        /// Returns true if this <see cref="Node"/>'s type is the same or equals to one of the descendands of the one identified by the given name.
        /// Note that the type is NodeType and/or ContentType (both types build similar inheritance structures).
        /// </summary>
        public bool TypeIs(string contentTypeName)
        {
            return NodeType.IsInstaceOfOrDerivedFrom(contentTypeName);
        }
    }
    public class BenchmarkCounter
    {
        public enum CounterName
        {
            GetParentPath,
            LoadParent,
            ContentCreate,
            BinarySet,
            FullSave,
            BeforeSaveToDb,
            SaveNodeBaseData,
            SaveNodeBinaries,
            SaveNodeFlatProperties,
            SaveNodeTextProperties,
            SaveNodeReferenceProperties,
            CommitPopulateNode,
            Audit,
            FinalizingSave
        }

        public static readonly string TraceCounterDataSlotName = "XCounterDataSlot";
        private static readonly int EnumLength;
        public static void IncrementBy(CounterName counterName, long value)
        {
            var slot = System.Threading.Thread.GetNamedDataSlot(TraceCounterDataSlotName);
            var xcounter = (BenchmarkCounter)System.Threading.Thread.GetData(slot);
            if (xcounter == null)
            {
                xcounter = new BenchmarkCounter();
                System.Threading.Thread.SetData(slot, xcounter);
            }
            xcounter[counterName] += value;
        }

        public static void Reset()
        {
            var slot = System.Threading.Thread.GetNamedDataSlot(TraceCounterDataSlotName);
            System.Threading.Thread.SetData(slot, new BenchmarkCounter());
        }
        public static long[] GetAll()
        {
            var slot = System.Threading.Thread.GetNamedDataSlot(TraceCounterDataSlotName);
            var xcounter = (BenchmarkCounter)System.Threading.Thread.GetData(slot);
            if (xcounter == null)
                return new long[EnumLength];
            return xcounter._counters;
        }

        // ==========================================================================================

        static BenchmarkCounter()
        {
            EnumLength = Enum.GetNames(typeof(CounterName)).Length;
        }

        private long[] _counters = new long[EnumLength];
        private long this[CounterName counterName]
        {
            get { return _counters[(int)counterName]; }
            set { _counters[(int)counterName] = value; }
        }

    }
}
