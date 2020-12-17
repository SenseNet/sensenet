using System;
using System.Collections.Generic;
using System.Linq;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Search;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.ContentRepository.Versioning;
using SenseNet.Diagnostics;
using System.Text;
using SenseNet.Search;
using SenseNet.Search.Indexing;
using SenseNet.ContentRepository.Linq;
using SenseNet.ContentRepository.Search;
using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.Preview;
using SenseNet.ContentRepository.Storage.Events;
using SenseNet.Search.Querying;
using SenseNet.Tools;
using System.Runtime.CompilerServices;
using SenseNet.ContentRepository.Sharing;

// ReSharper disable ArrangeThisQualifier
// ReSharper disable VirtualMemberCallInConstructor
// ReSharper disable RedundantBaseQualifier
// ReSharper disable InconsistentNaming
#pragma warning disable 618

namespace SenseNet.ContentRepository
{
    /// <summary>
    /// Defines constants for Content-saving algorithm selection.
    /// </summary>
    public enum SavingMode
    {
        /// <summary>After saving, the Content will have a new higher version.
        /// The value of the new version depends on the current VersioningMode.</summary>
        RaiseVersion,
        /// <summary>After saving, the Content will have a new higher version and will be locked for the current user.
        /// The value of the new version depends on the current VersioningMode.</summary>
        RaiseVersionAndLock,
        /// <summary>After saving, the Content's version will not be changed.</summary>
        KeepVersion,
        /// <summary>After saving, the Content's version will not be changed but will be locked for the current user.</summary>
        KeepVersionAndLock,
        /// <summary>After saving, the Content will be in multistep saving state.</summary>
        StartMultistepSave
    }

    /// <summary>
    /// Defines constants for the checkin comment policy of the current Content during execution of the CheckIn action.
    /// </summary>
    public enum CheckInCommentsMode
    {
        /// <summary>The policy is not defined.</summary>
        None,
        /// <summary>The checkin comment is not required but recommended.</summary>
        Recommended,
        /// <summary>The checkin comment is required.</summary>
        Compulsory
    }

    /// <summary>
    /// Defines constants for the Path interpretation mode in programmed Content queries.
    /// </summary>
    public enum PathUsageMode
    {
        /// <summary>Shallow search concatenated with AND operator. Pattern: ({original query}) AND InFolder:{Path}.</summary>
        InFolderAnd,
        /// <summary>Deep search concatenated with AND operator. Pattern: ({original query}) AND InTree:{Path}.</summary>
        InTreeAnd,
        /// <summary>Shallow search concatenated with OR operator. Pattern: ({original query}) OR InFolder:{Path}.</summary>
        InFolderOr,
        /// <summary>Deep search concatenated with OR operator. Pattern: ({original query}) OR InTree:{Path}.</summary>
        InTreeOr,
        /// <summary>Not defined</summary>
        NotUsed
    }

    /// <summary>
    /// Represents an abstraction of the current Content's children collection.
    /// </summary>
    public class ChildrenDefinition
    {
        /// <summary>
        /// Shortcut for general usage.
        /// </summary>
        public static ChildrenDefinition Default => new ChildrenDefinition { PathUsage = PathUsageMode.InFolderAnd };

        /// <summary>
        /// Gets or sets the value of the owner Content's path interpretation mode.
        /// </summary>
        public PathUsageMode PathUsage { get; set; }
        /// <summary>
        /// Gets or sets the CQL text if the children are produced by a Content query.
        /// Not used if the BaseCollection property is not null.
        /// </summary>
        public string ContentQuery { get; set; }

        /// <summary>
        /// Gets or sets a predefined children collection.
        /// If provided, this collection will be used as child items instead of 
        /// executing a query in the ContentQuery property.
        /// </summary>
        public IEnumerable<Node> BaseCollection { get; set; }

        /// <summary>
        /// Gets or sets the extension value of the query result maximization for 
        /// the Content Query property.
        /// </summary>
        public int Top { get; set; }
        /// <summary>
        /// Gets or sets the extension value of the skipped items for the Content Query property.
        /// </summary>
        public int Skip { get; set; }
        /// <summary>
        /// Gets or sets the sorting extension for the Content Query property.
        /// </summary>
        public IEnumerable<SortInfo> Sort { get; set; }
        /// <summary>
        /// Gets or sets the value for the ContentQuery property that is true if the 
        /// Count property of the query result should contain the total count of hits.
        /// </summary>
        public bool? CountAllPages { get; set; }
        /// <summary>
        /// Gets or sets the auto filter extension value for the Content Query property. See: <see cref="FilterStatus"/>.
        /// </summary>
        public FilterStatus EnableAutofilters { get; set; }
        /// <summary>
        /// Gets or sets the lifespan filter extension value for the Content Query property. See: <see cref="FilterStatus"/>.
        /// </summary>
        public FilterStatus EnableLifespanFilter { get; set; }
        /// <summary>
        /// Gets or sets the <see cref="QueryExecutionMode"/> extension value for the Content Query property.
        /// </summary>
        public QueryExecutionMode QueryExecutionMode { get; set; }

        /// <summary>
        /// Gets or sets the value that determines if the query should be executed only on direct children 
        /// or on the whole subtree (the value is true if the PathUsage property is InTreeAnd or InTreeOr, 
        /// otherwise false).
        /// </summary>
        public bool AllChildren
        {
            get => PathUsage == PathUsageMode.InTreeAnd || PathUsage == PathUsageMode.InTreeOr;
            set
            {
                switch (PathUsage)
                {
                    case PathUsageMode.InFolderAnd:
                        if (value)
                            PathUsage = PathUsageMode.InTreeAnd;
                        break;
                    case PathUsageMode.InTreeAnd:
                        if (!value)
                            PathUsage = PathUsageMode.InFolderAnd;
                        break;
                    case PathUsageMode.InFolderOr:
                        if (value)
                            PathUsage = PathUsageMode.InTreeOr;
                        break;
                    case PathUsageMode.InTreeOr:
                        if (!value)
                            PathUsage = PathUsageMode.InFolderOr;
                        break;
                    case PathUsageMode.NotUsed:
                        break;
                    default:
                        throw new SnNotSupportedException("Unknown PathUsageMode: " + PathUsage);
                }
            }
        }

        internal ChildrenDefinition Clone()
        {
            return new ChildrenDefinition
            {
                PathUsage = this.PathUsage,
                ContentQuery = this.ContentQuery,
                BaseCollection = this.BaseCollection,
                Top = this.Top,
                Skip = this.Skip,
                Sort = this.Sort,
                CountAllPages = this.CountAllPages,
                EnableAutofilters = this.EnableAutofilters,
                EnableLifespanFilter = this.EnableLifespanFilter,
                QueryExecutionMode = this.QueryExecutionMode
            };
        }
    }

    /// <summary>
    /// Represents a collection of all <see cref="ContentType"/>s.
    /// </summary>
    public class AllContentTypes : IEnumerable<ContentType>
    {
        /// <summary>
        /// Returns count of all <see cref="ContentType"/>s.
        /// </summary>
        /// <returns></returns>
        public int Count()
        {
            return ContentTypeManager.Instance.ContentTypes.Count;
        }
        /// <summary>
        /// Returns with true.
        /// </summary>
        public bool Contains(ContentType item)
        {
            return true;
        }
        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        public IEnumerator<ContentType> GetEnumerator()
        {
            return ContentTypeManager.Instance.ContentTypes.Values.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    /// <summary>
    /// Represents a collection of all <see cref="ContentType"/> names.
    /// </summary>
    public class AllContentTypeNames : IEnumerable<string>
    {
        /// <summary>
        /// Returns count of all <see cref="ContentType"/>s.
        /// </summary>
        /// <returns></returns>
        public int Count()
        {
            return ContentTypeManager.Instance.ContentTypes.Count;
        }
        /// <summary>
        /// Returns with true.
        /// </summary>
        public bool Contains(ContentType item)
        {
            return true;
        }
        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        public IEnumerator<string> GetEnumerator()
        {
            return ContentTypeManager.Instance.ContentTypes.Keys.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    /// <summary>
    /// Defines a class that can handle all types of Content (except <see cref="Schema.ContentType"/>) 
    /// in the sensenet Content Repository. Custom content handlers should inherit from this or
    /// one of its derived classes (e.g. <see cref="Folder"/> or <see cref="Workspaces.Workspace"/>).
    /// </summary>
    [ContentHandler]
    public class GenericContent : Node, IIndexableDocument
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GenericContent"/> class.
        /// </summary>
        /// <param name="parent">The parent.</param>
        protected GenericContent(Node parent)
            : base(parent)
        {
            VersionSetup();
            Initialize();
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="GenericContent"/> class.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="nodeTypeName">Name of the node type.</param>
        public GenericContent(Node parent, string nodeTypeName)
            : base(parent, nodeTypeName)
        {
            VersionSetup();
            Initialize();
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="GenericContent"/> class during the loading process.
        /// Do not use this constructor directly in your code.
        /// </summary>
        protected GenericContent(NodeToken nt)
            : base(nt)
        {
            VersionSetup();
        }

        private Content _content;
        /// <summary>
        /// Gets the wrapper <see cref="Content"/> of this instance.
        /// </summary>
        public Content Content => _content ?? (_content = Content.Create(this));

        protected override void PropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (_content == null)
                return;

            _content.PropertyChanged(propertyName);
            base.PropertyChanged(propertyName);
        }

        /// <summary>
        /// Initializes default field values in case of a new instance that is not yet saved to the database.
        /// </summary>
        protected virtual void Initialize()
        {
            if (this.Id > 0)
                return;
            var content = this.Content;
            
            // Elevation: we need to set default values (possibly reference fields) and
            // set 'is system' status regardless of the current user's permissions.
            using (new SystemAccount())
            {
                foreach (var item in content.Fields)
                    item.Value.SetDefaultValue();

                IsSystem = Parent != null && Parent.IsSystem;
            }
        }

        /// <summary>
        /// Gets the <see cref="Schema.ContentType"/> of this instance.
        /// </summary>
        public ContentType ContentType => ContentType.GetByName(this.NodeType.Name);

        /// <summary>
        /// Gets whether this class is <see cref="Schema.ContentType"/>.
        /// The value should be false in case of this and all derived classes.
        /// </summary>
        public override bool IsContentType => false;

        /// <summary>
        /// Gets or sets the user friendly name of this instance.
        /// The value is the same for every version of this content.
        /// </summary>
        public override string DisplayName
        {
            get
            {
                var result = base.DisplayName;
                if (string.IsNullOrEmpty(result) && this.Id != 0)
                    result = this.Name;
                return result;
            }
            set => base.DisplayName = value;
        }

        /// <summary>
        /// Gets or sets the description of this instance. Persisted as <see cref="RepositoryDataType.Text"/>.
        /// </summary>
        [RepositoryProperty("Description", RepositoryDataType.Text)]
        public virtual string Description
        {
            get => base.GetProperty<string>("Description");
            set => this["Description"] = value;
        }

        /// <summary>
        /// Gets or sets whether this instance is hidden or not. Persisted as <see cref="RepositoryDataType.Int"/>.
        /// </summary>
        [RepositoryProperty("Hidden", RepositoryDataType.Int)]
        public virtual bool Hidden
        {
            get => base.GetProperty<int>("Hidden") != 0;
            set => this["Hidden"] = value ? 1 : 0;
        }

        /// <summary>
        /// Defines a constant value for the name of the VersioningMode property.
        /// </summary>
        public const string VERSIONINGMODE = "VersioningMode";
        /// <summary>
        /// Gets or sets the versioning mode of this instance.
        /// See the <see cref="VersioningType"/> enumeration.
        /// Persisted as <see cref="RepositoryDataType.Int"/>.
        /// </summary>
        [RepositoryProperty(VERSIONINGMODE, RepositoryDataType.Int)]
        public virtual VersioningType VersioningMode
        {
            get
            {
                var vt = base.GetProperty<VersioningType>(VERSIONINGMODE);
                if (vt > VersioningType.Inherited)
                    return vt;

                using (new SystemAccount())
                {
                    return !(this.Parent is GenericContent parent)
                        ? VersioningType.None
                        : (VersioningType)parent.InheritableVersioningMode;
                }
            }
            set => this[VERSIONINGMODE] = value;
        }

        /// <summary>
        /// Gets or sets the versioning mode of child Content items in this container.
        /// See the <see cref="VersioningType"/> enumeration.
        /// Persisted as <see cref="RepositoryDataType.Int"/>.
        /// </summary>
        [RepositoryProperty("InheritableVersioningMode", RepositoryDataType.Int)]
        public virtual InheritableVersioningType InheritableVersioningMode
        {
            get
            {
                if (!HasProperty("InheritableVersioningMode"))
                    return InheritableVersioningType.None;

                var ivt = (InheritableVersioningType)this["InheritableVersioningMode"];
                if (ivt > InheritableVersioningType.Inherited)
                    return ivt;

                using (new SystemAccount())
                {
                    return !(this.Parent is GenericContent parent)
                        ? InheritableVersioningType.None
                        : parent.InheritableVersioningMode;
                }
            }
            set
            {
                if (!HasProperty("InheritableVersioningMode"))
                    return;

                if (value != base.GetProperty<InheritableVersioningType>("InheritableVersioningMode"))
                {
                    this["InheritableVersioningMode"] = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the approving mode of this instance.
        /// See the <see cref="ApprovingType"/> enumeration.
        /// Persisted as <see cref="RepositoryDataType.Int"/>.
        /// </summary>
        [RepositoryProperty("ApprovingMode", RepositoryDataType.Int)]
        public virtual ApprovingType ApprovingMode
        {
            get
            {
                if (!HasProperty("ApprovingMode"))
                    return ApprovingType.False;
                var at = base.GetProperty<ApprovingType>("ApprovingMode");
                if (at > ApprovingType.Inherited)
                    return at;

                using (new SystemAccount())
                {
                    return !(this.Parent is GenericContent parent)
                        ? ApprovingType.False
                        : parent.InheritableApprovingMode;
                }
            }
            set
            {
                if (HasProperty("ApprovingMode"))
                    this["ApprovingMode"] = value;
            }
        }

        /// <summary>
        /// Gets or sets the approving mode of child Content items of this container.
        /// See the <see cref="ApprovingType"/> enumeration.
        /// Persisted as <see cref="RepositoryDataType.Int"/>.
        /// </summary>
        [RepositoryProperty("InheritableApprovingMode", RepositoryDataType.Int)]
        public virtual ApprovingType InheritableApprovingMode
        {
            get
            {
                if (!HasProperty("InheritableApprovingMode"))
                    return ApprovingType.False;

                var at = base.GetProperty<ApprovingType>("InheritableApprovingMode");
                if (at > ApprovingType.Inherited)
                    return at;

                using (new SystemAccount())
                {
                    return !(this.Parent is GenericContent parent)
                        ? ApprovingType.False
                        : parent.InheritableApprovingMode;
                }
            }
            set
            {
                if (HasProperty("InheritableApprovingMode"))
                    this["InheritableApprovingMode"] = value;
            }
        }

        /// <summary>
        /// Defines a constant value for the name of the AllowedChildTypes property.
        /// </summary>
        public const string ALLOWEDCHILDTYPES = "AllowedChildTypes";
        /// <summary>
        /// Gets or sets the collection of allowed <see cref="Schema.ContentType"/>.
        /// The type inheritance is ignored in this case, all necessary exact types have to be in the collection.
        /// The <see cref="Schema.ContentType"/> of a new child Content needs to be in the collection.
        /// Persisted as <see cref="RepositoryDataType.Text"/>.
        /// </summary>
        [RepositoryProperty(ALLOWEDCHILDTYPES, RepositoryDataType.Text)]
        public virtual IEnumerable<ContentType> AllowedChildTypes
        {
            get
            {
                var value = this.GetProperty<string>(ALLOWEDCHILDTYPES);
                if (string.IsNullOrEmpty(value))
                    return ContentType.EmptyAllowedChildTypes;
                var names = value.Split(ContentType.XmlListSeparators, StringSplitOptions.RemoveEmptyEntries)
                    .Distinct().ToArray();
                var result = new List<ContentType>(names.Length);
                result.AddRange(names.Select(ContentType.GetByName).Where(t => t != null));
                return result;
            }
            set
            {
                var names = value == null ? null : string.Join(" ", value.Select(x => x.Name).Distinct());
                this[ALLOWEDCHILDTYPES] = names;
            }
        }

        /// <summary>
        /// Gets the effective collection of the allowed <see cref="Schema.ContentType"/>. Folders and
        /// Pages will inherit their list from their parents.
        /// Contains all allowed types except that id permitted for the current user.
        /// The type inheritance is ignored in this case, all necessary exact types have to be in the collection.
        /// The <see cref="Schema.ContentType"/> of a new child Content need to be in the collection.
        /// To modify this list please either modify it on the Content Type Definition globally,
        /// or locally using the <see cref="AllowedChildTypes"/> field on this instance.
        /// </summary>
        public virtual IEnumerable<ContentType> EffectiveAllowedChildTypes => GetAllowedChildTypes();

        /// <summary>
        /// Gets the <see cref="SenseNet.ContentRepository.CheckInCommentsMode"/> policy of this class.
        /// Returns with <see cref="SenseNet.ContentRepository.CheckInCommentsMode.None"/> in this case.
        /// Override this property in the inherited Content handlers to customize this behavior.
        /// </summary>
        public virtual CheckInCommentsMode CheckInCommentsMode => CheckInCommentsMode.None;

        /// <summary>
        /// Gets the <see cref="User"/> who locked exclusively this Content.
        /// This property is similar to the LockedBy property.
        /// </summary>
        public User CheckedOutTo => this.LockedBy as User;

        /// <summary>
        /// Gets a value that is true if the value of the VersioningMode is <see cref="VersioningType.Inherited"/>.
        /// </summary>
        public bool InheritedVersioning
        {
            get
            {
                var vt = base.GetProperty<VersioningType>(VERSIONINGMODE);
                return vt <= VersioningType.Inherited;
            }
        }

        /// <summary>
        /// Gets a value that is true if the value of the InheritableVersioningMode is <see cref="VersioningType.Inherited"/>.
        /// </summary>
        public bool InheritedInheritableVersioning
        {
            get
            {
                var vt = base.GetProperty<InheritableVersioningType>("InheritableVersioningMode");
                return vt <= InheritableVersioningType.Inherited;
            }
        }

        /// <summary>
        /// Gets a value that is true if the value of the ApprovingMode is <see cref="ApprovingType.Inherited"/>.
        /// </summary>
        public bool InheritedApproving
        {
            get
            {
                var at = base.GetProperty<ApprovingType>("ApprovingMode");
                return at <= ApprovingType.Inherited;
            }
        }

        /// <summary>
        /// Gets a value that is true if the value of the InheritableApprovingMode is <see cref="ApprovingType.Inherited"/>.
        /// </summary>
        public bool InheritedInheritableApproving
        {
            get
            {
                var at = base.GetProperty<ApprovingType>("InheritableApprovingMode");
                return at <= ApprovingType.Inherited;
            }
        }

        /// <summary>
        /// Gets a value that is true if the value of the ApprovingMode is <see cref="ApprovingType.True"/>.
        /// </summary>
        public bool HasApproving => this.ApprovingMode == ApprovingType.True;

        private GenericContent _workspace;
        /// <summary>
        /// Gets the nearest Content in the parent chain that is a Workspace.
        /// Returns null if there is no Workspace in the parent chain.
        /// </summary>
        public GenericContent Workspace => _workspace ?? (_workspace = GetAncestorOfNodeType(this, "Workspace") as GenericContent);

        /// <summary>
        /// Gets the name of the Workspace if exists or null.
        /// </summary>
        public string WorkspaceName => this.Workspace == null ? string.Empty : this.Workspace.Name;

        /// <summary>
        /// Gets the title of the Workspace if exists or null.
        /// </summary>
        public string WorkspaceTitle => this.Workspace == null ? string.Empty : this.Workspace.DisplayName;

        /// <summary>
        /// Gets the path of the Workspace if exists or null.
        /// </summary>
        public string WorkspacePath => this.Workspace == null ? string.Empty : this.Workspace.Path;

        /// <summary>
        /// Gets or sets a value that is true if this Content instance cannot be moved to the Trash.
        /// Persisted as <see cref="RepositoryDataType.Int"/>.
        /// </summary>
        [RepositoryProperty("TrashDisabled", RepositoryDataType.Int)]
        public bool TrashDisabled
        {
            get => base.GetProperty<int>("TrashDisabled") != 0;
            set => this["TrashDisabled"] = value ? 1 : 0;
        }

        /// <summary>
        /// Gets or sets whether this instance is under lifespan control or not.
        /// Persisted as <see cref="RepositoryDataType.Int"/>.
        /// </summary>
        [RepositoryProperty("EnableLifespan", RepositoryDataType.Int)]
        public bool EnableLifespan
        {
            get => base.GetProperty<int>("EnableLifespan") != 0;
            set => this["EnableLifespan"] = value ? 1 : 0;
        }

        /// <summary>
        /// Gets or sets the DateTime of the start of  this instance's lifespan. 
        /// If <see cref="EnableLifespan"/> is set to true, this content will appear 
        /// in query results only after the date set here.
        /// Persisted as <see cref="RepositoryDataType.DateTime"/>.
        /// </summary>
        [RepositoryProperty("ValidFrom", RepositoryDataType.DateTime)]
        public DateTime ValidFrom
        {
            get => base.GetProperty<DateTime>("ValidFrom");
            set => this["ValidFrom"] = value;
        }

        /// <summary>
        /// Gets or sets the DateTime of the end of  this instance's lifespan.
        /// If <see cref="EnableLifespan"/> is set to true, this content will appear 
        /// in query results only before the date set here.
        /// Persisted as <see cref="RepositoryDataType.DateTime"/>.
        /// </summary>
        [RepositoryProperty("ValidTill", RepositoryDataType.DateTime)]
        public DateTime ValidTill
        {
            get => base.GetProperty<DateTime>("ValidTill");
            set => this["ValidTill"] = value;
        }

        /// <summary>
        /// Defines a constant value for the name of the Aspects property.
        /// </summary>
        public const string ASPECTS = "Aspects";
        /// <summary>
        /// Gets or sets the collection of the associated <see cref="Aspect"/>s.
        /// </summary>
        [RepositoryProperty(ASPECTS, RepositoryDataType.Reference)]
        public IEnumerable<Aspect> Aspects
        {
            get => base.GetReferences(ASPECTS).Cast<Aspect>();
            set => this.SetReferences(ASPECTS, value);
        }

        /// <summary>
        /// Defines a constant value for the name of the AspectData property.
        /// </summary>
        public const string ASPECTDATA = "AspectData";
        /// <summary>
        /// Gets or sets all field values of all associated <see cref="Aspect"/>s.
        /// Persisted as <see cref="RepositoryDataType.Text"/>.
        /// </summary>
        [RepositoryProperty(ASPECTDATA, RepositoryDataType.Text)]
        public string AspectData
        {
            get => base.GetProperty<string>(ASPECTDATA);
            set => this[ASPECTDATA] = value;
        }

        /// <summary>
        /// Returns true if the current class implements the <see cref="IFolder"/> interface.
        /// </summary>
        public bool IsFolder => this is IFolder;

        /// <summary>
        /// Gets the URL of the currently associated "Browse" action or an empty string.
        /// </summary>
        public string BrowseUrl
        {
            get
            {
                try
                {
                    return ActionFramework.GetActionUrl(this.Path, "Browse");
                }
                catch (InvalidContentActionException)
                {
                    // Browse action is missing
                    return string.Empty;
                }
                catch (ContentNotFoundException)
                {
                    // Browse action is missing
                    return string.Empty;
                }
            }
        }

        /// <summary>
        /// Returns true if indexing is enabled on this instance.
        /// This is a shortcut for a similar property of the current ContentType. 
        /// Inherited content handlers may customize this value.
        /// </summary>
        protected override bool IsIndexingEnabled => this.ContentType.IndexingEnabled;

        private readonly object _sharingHandlerSync = new object();
        private SharingHandler _sharingHandler;

        /// <summary>
        /// Gets the API entry point for managing content sharing.
        /// </summary>
        public SharingHandler Sharing
        {
            get
            {
                if (_sharingHandler == null)
                    lock (_sharingHandlerSync)
                        if (_sharingHandler == null)
                            _sharingHandler = new SharingHandler(this);
                return _sharingHandler;
            }
        }

        [RepositoryProperty(nameof(SharingData), RepositoryDataType.Text)]
        internal string SharingData
        {
            get => this.GetProperty<string>(nameof(SharingData));
            set => SetSharingData(value);
        }

        internal void SetSharingData(string data, bool resetItems = true)
        {
            this.SetProperty(nameof(SharingData), data);

            if (resetItems)
                this.Sharing.ItemsChanged();
        }

        /// <summary>
        /// Returns a property value by name. Well-known and dynamic properties can also be accessed here.
        /// In derived content handlers this should be overridden and in case of local strongly typed
        /// properties return their value - otherwise call the base implementation.
        /// </summary>
        public virtual object GetProperty(string name)
        {
            switch (name)
            {
                case ASPECTS:
                    return this.Aspects;
                case ASPECTDATA:
                    return this.AspectData;
                case "Icon":
                    return this.Icon;
                case "Depth":
                    return this.Depth;
                case "DisplayName":
                    return this.DisplayName;
                case "Hidden":
                    return this.Hidden;
                case "TrashDisabled":
                    return this.TrashDisabled;
                case VERSIONINGMODE:
                    return this.VersioningMode;
                case "InheritableVersioningMode":
                    return this.InheritableVersioningMode;
                case "ApprovingMode":
                    return this.ApprovingMode;
                case "InheritableApprovingMode":
                    return this.InheritableApprovingMode;
                case ALLOWEDCHILDTYPES:
                    return this.AllowedChildTypes;
                case "EffectiveAllowedChildTypes":
                    return this.EffectiveAllowedChildTypes;
                case "EnableLifespan":
                    return this.EnableLifespan;
                case "ValidFrom":
                    return this.ValidFrom;
                case "ValidTill":
                    return this.ValidTill;
                case "Workspace":
                    return this.Workspace;
                case "WorkspaceName":
                    return this.WorkspaceName;
                case "WorkspacePath":
                    return this.WorkspacePath;
                case "WorkspaceTitle":
                    return this.WorkspaceTitle;
                case "BrowseApplication":
                    return this.BrowseApplication;
                case "Approvable":
                    return this.Approvable;
                case "Publishable":
                    return this.Publishable;
                case "Versions":
                    return this.Versions;
                case "CheckedOutTo":
                    return this.CheckedOutTo;
                case "IsFolder":
                    return this.IsFolder;
                case "BrowseUrl":
                    return this.BrowseUrl;
                case "Sharing":
                    return null;
                default:
                    return base[name];
            }
        }
        /// <summary>
        /// Assigns the given value to the named property. Well-known and dynamic properties can also be set.
        /// In derived content handlers this should be overridden and in case of local strongly typed
        /// properties set their value - otherwise call the base implementation.
        /// </summary>
        public virtual void SetProperty(string name, object value)
        {
            switch (name)
            {
                case ASPECTS:
                    this.Aspects = ((System.Collections.IEnumerable)value).Cast<Aspect>();
                    break;
                case ASPECTDATA:
                    this.AspectData = (string)value;
                    break;
                case "Icon":
                    this.Icon = (string)value;
                    break;
                case "DisplayName":
                    this.DisplayName = (string)value;
                    break;
                case "TrashDisabled":
                    this.TrashDisabled = (bool)value;
                    break;
                case "Hidden":
                    this.Hidden = (bool)value;
                    break;
                case ALLOWEDCHILDTYPES:
                    this.AllowedChildTypes = (IEnumerable<ContentType>)value;
                    break;
                case "BrowseApplication":
                    this.BrowseApplication = value as Node;
                    break;
                case "EnableLifespan":
                    this.EnableLifespan = (bool)value;
                    break;
                case "ValidFrom":
                    this.ValidFrom = (DateTime?) value ?? DateTime.MinValue;
                    break;
                case "ValidTill":
                    this.ValidTill = (DateTime?) value ?? DateTime.MinValue;
                    break;
                case "Workspace":
                case "WorkspaceName":
                case "WorkspacePath":
                case "WorkspaceTitle":
                case "Approvable":
                case "Publishable":
                case "Versions":
                case "CheckedOutTo":
                case "Sharing":
                    // do nothing, these props are readonly
                    break;
                default:
                    base[name] = value;
                    break;
            }
        }

        /// <summary>
        /// Returns Content items that refer this instance in one of the following properties:
        /// CreatedById, ModifiedById, VersionCreatedById, VersionModifiedById, LockedById.
        /// The collection does not contain any referrers in dynamic reference properties.
        /// This Content is also excluded from the collection.
        /// </summary>
        /// <param name="top">Maximum count of the collection</param>
        /// <param name="totalCount">Output value of the total count of referrers regardless the value of the "top" parameter.</param>
        /// <returns>An IEnumerable&lt;<see cref="Node"/>&gt;</returns>
        protected override IEnumerable<Node> GetReferrers(int top, out int totalCount)
        {
            var result = ContentQuery.Query("-Id:@0 +(CreatedById:@0 ModifiedById:@0 VersionCreatedById:@0 VersionModifiedById:@0 LockedById:@0)",
                new QuerySettings { Top = top, EnableAutofilters = FilterStatus.Disabled },
                this.Id);
            totalCount = result.Count;
            return result.Nodes;
        }

        // ============================================================================================= Allowed child types API

        /// <summary>
        /// Returns the effective collection of the allowed <see cref="Schema.ContentType"/> names.
        /// Contains all allowed type names except the ones that are not permitted for the current user.
        /// The type inheritance is ignored in this case, all necessary exact types have to be in the collection.
        /// The <see cref="Schema.ContentType"/> of a new child Content need to be in the collection.
        /// </summary>
        public IEnumerable<string> GetAllowedChildTypeNames()
        {
            return GetAllowedChildTypeNames(true);
        }
        private IEnumerable<string> GetAllowedChildTypeNames(bool withSystemFolder)
        {
            // in case of folders and pages inherit settings from parent
            if (this.NodeType.Name == "Folder" || this.NodeType.Name == "Page")
            {
                var parent = SystemAccount.Execute(() => Parent as GenericContent);
                if (parent == null)
                    return ContentType.EmptyAllowedChildTypeNames;
                return parent.GetAllowedChildTypeNames();
            }

            // collect types set on local instance
            var names = new List<string>();        
            foreach (var ct in this.AllowedChildTypes)
                if (ct.Security.HasPermission(PermissionType.See))
                    names.Add(ct.Name);

            // Indicates that the local list has items. The length of list is not enough because the permission filters
            // the list and if the filter skips all elements, the user gets the list that declared on the Content type.
            var hasLocalItems = names.Count > 0;

            // SystemFolder or TrashBag allows every type if there is no setting on local instance
            var systemFolderName = "SystemFolder";
            if (!hasLocalItems && (this.NodeType.Name == systemFolderName || this.NodeType.Name == "TrashBag"))
                return new AllContentTypeNames(); // new string[0];

            // settings come from CTD if no local setting is present
            if (!hasLocalItems)
            {
                foreach (var ct in this.ContentType.AllowedChildTypes)
                {
                    if (ct.Security.HasPermission(PermissionType.See))
                        if (!names.Contains(ct.Name))
                            names.Add(ct.Name);
                }
            }

            if (withSystemFolder)
            {
                // SystemFolder can be created anywhere if the user has the necessary permissions on the CTD
                var systemFolderType = ContentType.GetByName(systemFolderName);
                if (systemFolderType.Security.HasPermission(PermissionType.See))
                    if (!names.Contains(systemFolderName))
                        names.Add(systemFolderName);
            }

            return names;
        }
        /// <summary>
        /// Returns the effective collection of the allowed <see cref="Schema.ContentType"/>. Folders and
        /// Pages will inherit their list from their parents.
        /// Contains all allowed types except the ones that are not permitted for the current user.
        /// The type inheritance is ignored in this case, all necessary exact types have to be in the collection.
        /// The <see cref="Schema.ContentType"/> of a new child Content need to be in the collection.
        /// </summary>
        public IEnumerable<ContentType> GetAllowedChildTypes()
        {
            // in case of folders and pages inherit settings from parent
            if (this.NodeType.Name == "Folder" || this.NodeType.Name == "Page")
            {
                GenericContent parent;
                using (new SystemAccount())
                    parent = Parent as GenericContent;
                if (parent == null)
                    return ContentType.EmptyAllowedChildTypes;
                return parent.GetAllowedChildTypes();
            }

            // collect types set on local instance
            var types = new List<ContentType>();
            var hiddenLocalItems = false;
            foreach (var ct in this.AllowedChildTypes)
            {
                if (ct.Security.HasPermission(PermissionType.See))
                    types.Add(ct);
                else
                    hiddenLocalItems = true;
            }

            // Indicates that the local list has items. The length of list is not enough because the permission filters
            // the list and if the filter skips all elements, the user gets the list that declared on the Content type.
            var hasLocalItems = types.Count > 0 || hiddenLocalItems;

            // SystemFolder or TrashBag allows every type if there is no setting on local instance
            var systemFolderName = "SystemFolder";
            if (!hasLocalItems && (this.NodeType.Name == systemFolderName || this.NodeType.Name == "TrashBag"))
                return new AllContentTypes();

            // settings come from CTD if no local setting is present
            if (!hasLocalItems)
            {
                foreach (var ct in this.ContentType.AllowedChildTypes)
                {
                    if (ct.Security.HasPermission(PermissionType.See))
                        if (!types.Contains(ct))
                            types.Add(ct);
                }
            }

            // SystemFolder can be created anywhere if the user has the necessary permissions on the CTD
            var systemFolderType = ContentType.GetByName(systemFolderName);
            if (systemFolderType.Security.HasPermission(PermissionType.See))
                if (!types.Contains(systemFolderType))
                    types.Add(systemFolderType);

            return types;
        }
        /// <summary>
        /// Returns true if the current user can create a Content by the given <see cref="Schema.ContentType"/> name 
        /// under this container.
        /// </summary>
        public bool IsAllowedChildType(string contentTypeName)
        {
            if (this is SystemFolder || this is TrashBag)
                return true;
            var list = GetAllowedChildTypeNames();
            return list.Contains(contentTypeName);
        }
        /// <summary>
        /// Returns true if the current user can create a Content by the given <see cref="Schema.ContentType"/>
        /// under this container.
        /// </summary>
        public bool IsAllowedChildType(ContentType contentType)
        {
            return IsAllowedChildType(contentType.Name);
        }

        /// <summary>
        /// Defines constants for the verification of the child type.
        /// </summary>
        [Obsolete("This enum will become private in the future.")]
        public enum TypeAllow
        {
            /// <summary>The type is allowed and permitted.</summary>
            Allowed,
            /// <summary>The type is allowed but not permitted.</summary>
            TypeIsNotPermitted,
            /// <summary>The type is not allowed.</summary>
            NotAllowed
        }

        internal void AssertAllowedChildType(Node node, bool move = false)
        {
            switch (CheckAllowedChildType(node))
            {
                case TypeAllow.Allowed:
                    return;
                case TypeAllow.TypeIsNotPermitted:
                    throw new SenseNetSecurityException(node.Path, PermissionType.See, User.Current);
                case TypeAllow.NotAllowed:
                    if (move)
                        throw GetNotAllowedContentTypeExceptionOnMove(node, this);
                    else
                        throw GetNotAllowedContentTypeExceptionOnCreate(node, this);
            }
        }
        internal TypeAllow CheckAllowedChildType(Node node)
        {
            var contentTypeName = node.NodeType.Name;

            // Ok if the new node is exactly TrashBag
            if (contentTypeName == "TrashBag")
                return TypeAllow.Allowed;

            // Ok if the new node is exactly SystemFolder and it is permitted
            if (contentTypeName == typeof(SystemFolder).Name)
            {
                if (node.CopyInProgress)
                    return TypeAllow.Allowed;

                var contentType = ContentType.GetByName(contentTypeName);
                if(contentType.Security.HasPermission(PermissionType.See))
                    return TypeAllow.Allowed;
                return TypeAllow.TypeIsNotPermitted;
            }

            // Get parent if this is Folder or Page. Exit if current is SystemFolder or it is not a GenericContent
            var current = this;
            using (new SystemAccount())
            {
                // using as object when it is unknown (Page)
                while (current != null && (current.NodeType.Name == "Folder" || current.NodeType.Name == "Page"))
                    current = current.Parent as GenericContent;
            }

            if (current != null && current.NodeType.Name == "SystemFolder")
                return TypeAllow.Allowed;
            if (current == null)
                return TypeAllow.Allowed;

            if(current.IsAllowedChildType(contentTypeName))
                return TypeAllow.Allowed;
            return TypeAllow.NotAllowed;
        }

        private Exception GetNotAllowedContentTypeExceptionOnCreate(Node node, GenericContent parent)
        {
            var ancestor = parent;
            using (new SystemAccount())
            {
                while (ancestor.NodeType.Name == "Folder" || ancestor.NodeType.Name == "Page")
                {
                    var p = ancestor.Parent as GenericContent;
                    if (p == null)
                        break;
                    ancestor = p;
                }
            }

            var contentTypeName = node.NodeType.Name;
            var nodePath = string.Concat(node.ParentPath, "/", node.Name);

            return new InvalidOperationException(
                $"Cannot save the content '{nodePath}' because its ancestor does not allow the type '{contentTypeName}'." +
                $" Ancestor: {ancestor.Path} ({ancestor.NodeType.Name}). Allowed types: {string.Join(", ", parent.GetAllowedChildTypeNames())}");
        }
        private Exception GetNotAllowedContentTypeExceptionOnMove(Node node, GenericContent target)
        {
            using (new SystemAccount())
            {
                var ancestor = target;
                while (ancestor.NodeType.Name == "Folder" || ancestor.NodeType.Name == "Page")
                {
                    var p = ancestor.Parent as GenericContent;
                    if (p == null)
                        break;
                    ancestor = p;
                }

                var contentTypeName = node.NodeType.Name;

                return new InvalidOperationException(
                    $"Cannot move the content '{node.Path}' to '{target.Path}' because target's ancestor " +
                    $"does not allow the type '{contentTypeName}'. Ancestor: {ancestor.Path} ({ancestor.NodeType.Name}). " +
                    $"Allowed types: {string.Join(", ", target.GetAllowedChildTypeNames())}");
            }
        }
        /// <summary>
        /// Allow a child type by the given parameters.
        /// The original list will be extended by the given <see cref="Schema.ContentType"/>>.
        /// </summary>
        /// <param name="contentTypeName">The name of the allowed <see cref="Schema.ContentType"/>.</param>
        /// <param name="setOnAncestorIfInherits">If set to true and the current Content is a Folder or Page (meaning the allowed type list is inherited),
        /// the provided content type will be added to the parent's list.
        /// Optional parameter. Default: false.</param>
        /// <param name="throwOnError">Specifies whether an error should be thrown when the operation is unsuccessful. Optional, default: true.</param>
        /// <param name="save">Optional parameter that is true if the Content will be saved automatically after setting the new collection.
        /// Default: false</param>
        public void AllowChildType(string contentTypeName, bool setOnAncestorIfInherits = false, bool throwOnError = true, bool save = false)
        {
            AllowChildTypes(new[] { contentTypeName }, setOnAncestorIfInherits, throwOnError, save);
        }
        /// <summary>
        /// Allow a child type by the given parameters.
        /// The original list will be extended by the given <see cref="Schema.ContentType"/>>.
        /// </summary>
        /// <param name="contentType">The allowed <see cref="Schema.ContentType"/>.</param>
        /// <param name="setOnAncestorIfInherits">If set to true and the current Content is a Folder or Page (meaning the allowed type list is inherited),
        /// the provided content type will be added to the parent's list.
        /// Optional parameter. Default: false.</param>
        /// <param name="throwOnError">Specifies whether an error should be thrown when the operation is unsuccessful. Optional, default: true.</param>
        /// <param name="save">Optional parameter that is true if the Content will be saved automatically after setting the new collection.
        /// Default: false</param>
        public void AllowChildType(ContentType contentType, bool setOnAncestorIfInherits = false, bool throwOnError = true, bool save = false)
        {
            AllowChildTypes(new[] { contentType }, setOnAncestorIfInherits, throwOnError, save);
        }
        /// <summary>
        /// Allow child types by the given parameters.
        /// The original list will be extended by the given collection.
        /// </summary>
        /// <param name="contentTypeNames">The name collection of the allowed <see cref="Schema.ContentType"/>s.</param>
        /// <param name="setOnAncestorIfInherits">If set to true and the current Content is a Folder or Page (meaning the allowed type list is inherited),
        /// the provided content types will be added to the parent's list.
        /// Optional parameter. Default: false.</param>
        /// <param name="throwOnError">Specifies whether an error should be thrown when the operation is unsuccessful. Optional, default: true.</param>
        /// <param name="save">Optional parameter that is true if the Content will be saved automatically after setting the new collection.
        /// Default: false</param>
        public void AllowChildTypes(IEnumerable<string> contentTypeNames, bool setOnAncestorIfInherits = false, bool throwOnError = true, bool save = false)
        {
            AllowChildTypes(contentTypeNames
                .Select(ContentType.GetByName)
                .Where(x => x != null), setOnAncestorIfInherits, throwOnError, save);
        }
        /// <summary>
        /// Allow types of children by the given parameters.
        /// The original list will be extended by the given collection.
        /// </summary>
        /// <param name="contentTypes">The new collection of the allowed <see cref="Schema.ContentType"/>.</param>
        /// <param name="setOnAncestorIfInherits">If set to true and the current Content is a Folder or Page (meaning the allowed type list is inherited),
        /// the provided content types will be added to the parent's list.
        /// Optional parameter. Default: false.</param>
        /// <param name="throwOnError">Specifies whether an error should be thrown when the operation is unsuccessful. Optional, default: true.</param>
        /// <param name="save">Optional parameter that is true if the Content will be saved automatically after setting the new collection.
        /// Default: false</param>
        public void AllowChildTypes(IEnumerable<ContentType> contentTypes, bool setOnAncestorIfInherits = false, bool throwOnError = true, bool save = false)
        {
            if (contentTypes == null)
                throw new ArgumentNullException(nameof(contentTypes));

            SetAllowedChildTypesByType(
                parent => parent.AllowChildTypes(contentTypes, setOnAncestorIfInherits, throwOnError, true),
                () =>
                {
                    // get the full effective list and extend it with the new types
                    var effectiveList = GetAllowedChildTypes().Union(contentTypes).Distinct();

                    SetAllowedChildTypesInternal(effectiveList, save);
                }, 
                setOnAncestorIfInherits, 
                throwOnError);
        }
        /// <summary>
        /// Set the allowed child types on this content. If the provided list is the same as on the content type,
        /// the property will be cleared and values will be inherited from the content type from now on.
        /// </summary>
        /// <param name="contentTypes">The new collection of the allowed <see cref="Schema.ContentType"/>.</param>
        /// <param name="setOnAncestorIfInherits">If set to true and the current Content is a Folder or Page (meaning the allowed type list 
        /// is inherited), the provided content types will be added to the parent's list.
        /// Optional parameter. Default: false.</param>
        /// <param name="throwOnError">Specifies whether an error should be thrown when the operation is unsuccessful. Optional, default: true.</param>
        /// <param name="save">Optional parameter that is true if the Content should be saved automatically after setting the new collection.
        /// Default: false</param>
        public void SetAllowedChildTypes(IEnumerable<ContentType> contentTypes, bool setOnAncestorIfInherits = false, bool throwOnError = true, bool save = false)
        {
            if (contentTypes == null)
                throw new ArgumentNullException(nameof(contentTypes));

            SetAllowedChildTypesByType(
                parent => parent.SetAllowedChildTypes(contentTypes, setOnAncestorIfInherits, throwOnError, true),
                () => SetAllowedChildTypesInternal(contentTypes, save), 
                setOnAncestorIfInherits, 
                throwOnError);
        }

        private void SetAllowedChildTypesByType(Action<GenericContent> parentAction, Action setAction, 
            bool setOnAncestorIfInherits = false, bool throwOnError = true)
        {
            // This method provides the algorithm for handling special types (Folder, Page) 
            // that are treated differenlty in case of the allowed child types feature.

            switch (this.NodeType.Name)
            {
                case "Folder":
                case "Page":
                    if (setOnAncestorIfInherits)
                    {
                        GenericContent parent;
                        using (new SystemAccount())
                            parent = this.Parent as GenericContent;

                        if (parent != null)
                        {
                            // execute the action on the parent instead
                            parentAction(parent);
                        }
                        else
                        {
                            if (throwOnError)
                                throw GetCannotAllowContentTypeException();
                        }
                    }
                    else
                    {
                        if (throwOnError)
                            throw GetCannotAllowContentTypeException();
                    }
                    return;
                case "SystemFolder":
                    if (throwOnError)
                        throw GetCannotAllowContentTypeException();
                    return;
                default:
                    // execute the action on the content itself
                    setAction();
                    return;
            }
        }
        private void SetAllowedChildTypesInternal(IEnumerable<ContentType> contentTypes, bool save = false)
        {
            var newContentTypeList = contentTypes?.ToArray() ?? new ContentType[0];

            // compare the new list with the list defined on the content type
            var contentTypeExceptNewAny = ContentType.AllowedChildTypes.Except(newContentTypeList).Any();
            var newExceptContentTypeAny = newContentTypeList.Except(ContentType.AllowedChildTypes).Any();

            // If the two lists are identical, the local value should be empty: 
            // the values from the CTD will be inherited. Otherwise set the
            // provided list explicitely.
            AllowedChildTypes = !newExceptContentTypeAny && !contentTypeExceptNewAny
                ? new ContentType[0]
                : newContentTypeList;

            if (save)
                Save();
        }

        private Exception GetCannotAllowContentTypeException()
        {
            return new InvalidOperationException(
                $"Cannot allow ContentType on a {this.NodeType.Name}. Path: {this.Path}");
        }

        /// <summary>
        /// Extends the requested content's AllowedChildTypes collection with the provided Content types.
        /// <nodoc>The Content will be saved after the operation.
        /// This is an <see cref="ODataAction"/>.</nodoc>
        /// </summary>
        /// <snCategory>Content Types</snCategory>
        /// <param name="content"></param>
        /// <param name="contentTypes" example='["Task", "Event"]'>The extension.</param>
        /// <returns>Empty string.</returns>
        [ODataAction]
        [AllowedRoles(N.R.Everyone)]
        public static string AddAllowedChildTypes(Content content, string[] contentTypes)
        {
            if (!(content.ContentHandler is GenericContent gc))
                return string.Empty;

            gc.AllowChildTypes(contentTypes, false, true, true);

            return string.Empty;
        }
        /// <summary>
        /// Removes the specified Content types from the requested content's AllowedChildTypes collection.
        /// <nodoc>The Content will be saved after the operation.
        /// This is an <see cref="ODataAction"/>.</nodoc>
        /// </summary>
        /// <snCategory>Content Types</snCategory>
        /// <param name="content"></param>
        /// <param name="contentTypes">The items that will be removed.</param>
        /// <returns>Empty string.</returns>
        [ODataAction]
        [AllowedRoles(N.R.Everyone)]
        public static string RemoveAllowedChildTypes(Content content, string[] contentTypes)
        {
            if (!(content.ContentHandler is GenericContent gc))
                return string.Empty;

            var remainingNames = gc
                .GetAllowedChildTypeNames(false)
                .Except(contentTypes)
                .ToArray();

            IEnumerable<ContentType> remainingTypes = null;
            var ctdNames = content.ContentType.AllowedChildTypeNames.ToArray();
            if (0 < (ctdNames.Except(remainingNames).Count() + remainingNames.Except(ctdNames).Count()))
                remainingTypes = remainingNames.Select(ContentType.GetByName).ToArray();

            gc.AllowedChildTypes = remainingTypes;
            gc.Save();

            return string.Empty;
        }

        /// <summary>
        /// Tool method that returns information about inconsistent elements in this subtree.
        /// Checks whether every Content in this subtree meets the allowed child types rules.
        /// Every difference is recorded in a tab separated table.
        /// </summary>
        /// <returns>String containing a tab separated table of the inconsistent elements.</returns>
        public string CheckChildrenTypeConsistence()
        {
            var result = new StringBuilder();
            result.AppendLine("Path\tType\tChild name\tChild type\tReason");
            foreach (var node in NodeEnumerator.GetNodes(this.Path, ExecutionHint.ForceRelationalEngine))
            {
                var parentGc = node.Parent as GenericContent;
                if (parentGc == null)
                    continue;

                var checkResult = parentGc.CheckAllowedChildType(node);
                if(checkResult != TypeAllow.Allowed)
                {
                    result.AppendFormat("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\r\n", parentGc.Path, parentGc.NodeType.Name, node.Name, node.NodeType.Name, 
                        string.Join(", ", parentGc.GetAllowedChildTypeNames()), checkResult);
                    result.AppendLine();
                }
            }
            return result.ToString();
        }

        // =============================================================================================

        /// <summary>
        /// Returns the list of all <see cref="FieldSetting"/>s in this Content's allowed child types.
        /// Every <see cref="FieldSetting"/>'s root version of the inheritance chain is included.
        /// </summary>
        /// <returns>The collected list of <see cref="FieldSetting"/>s</returns>
        public virtual List<FieldSetting> GetAvailableFields()
        {
            return GetAvailableFields(true);
        }

        /// <summary>
        /// Returns the list of all <see cref="FieldSetting"/>s in this Content's allowed child types.
        /// </summary>
        /// <param name="rootFields">Boolean value that specifies whether the currently overridden <see cref="FieldSetting"/>
        /// or root version of the inheritance chain should be included.</param>
        /// <returns>The collected list of <see cref="FieldSetting"/>s</returns>
        public virtual List<FieldSetting> GetAvailableFields(bool rootFields)
        {
            var availableFields = new List<FieldSetting>();

            GetAvailableContentTypeFields(availableFields, rootFields);

            return availableFields;
        }

        /// <summary>
        /// Collects all <see cref="FieldSetting"/>s int this Content's allowed child types.
        /// Every <see cref="FieldSetting"/>'s root version of the inheritance chain is included.
        /// </summary>
        /// <param name="availableFields">Output collection of <see cref="FieldSetting"/>s. Does not contain duplicates.</param>
        protected void GetAvailableContentTypeFields(ICollection<FieldSetting> availableFields)
        {
            GetAvailableContentTypeFields(availableFields, true);
        }

        /// <summary>
        /// Collects all <see cref="FieldSetting"/>s in this Content's allowed child types.
        /// </summary>
        /// <param name="availableFields">Output collection of <see cref="FieldSetting"/>s. Does not contain duplicates.</param>
        /// <param name="rootFields">Boolean value that specifies whether the currently overridden <see cref="FieldSetting"/>
        /// or root version of the inheritance chain should be included.</param>
        protected void GetAvailableContentTypeFields(ICollection<FieldSetting> availableFields, bool rootFields)
        {
            var contentTypes = this.GetAllowedChildTypes().ToArray();

            // if there are no available content types, it means all types are allowed
            if (contentTypes.Length == 0)
                contentTypes = ContentType.GetContentTypes();

            foreach (var contentType in contentTypes)
            {
                GetFields(contentType, availableFields, rootFields);
            }
        }

        /// <summary>
        /// Collects all <see cref="FieldSetting"/>s of the given <see cref="Schema.ContentType"/>.
        /// </summary>
        /// <param name="contentType">The <see cref="Schema.ContentType"/> thats <see cref="FieldSetting"/>s will be collected.</param>
        /// <param name="availableFields">Output collection of <see cref="FieldSetting"/>s. Does not contain duplicates.</param>
        /// <param name="rootFields">Boolean value that specifies whether the currently overridden <see cref="FieldSetting"/>
        /// or root version of the inheritance chain should be included.</param>
        protected static void GetFields(ContentType contentType, ICollection<FieldSetting> availableFields, bool rootFields)
        {
            foreach (var fieldSetting in contentType.FieldSettings)
            {
                var fsRoot = rootFields ? FieldSetting.GetRoot(fieldSetting) : fieldSetting;

                if (!availableFields.Contains(fsRoot))
                    availableFields.Add(fsRoot);
            }
        }

        /// <summary>
        /// Moves this Content under the given target <see cref="Node"/>.
        /// If the target is <see cref="GenericContent"/>, the AllowedChildTypes collections need to be compatible. 
        /// It means that the current Content's AllowedChildTypes cannot contain any element that is not allowed on the
        /// target <see cref="Node"/> otherwise an InvalidOperationException will be thrown.
        /// </summary>
        /// <param name="target">The target <see cref="Node"/> that will be the parent of this Content.</param>
        public override void MoveTo(Node target)
        {
            if (target is GenericContent targetGc)
                foreach (var nt in this.GetChildTypesToAllow())
                    if (!targetGc.IsAllowedChildType(nt.Name))
                        throw new InvalidOperationException(
                            $"Cannot move {this.Path} ({this.NodeType.Name}) to {target.Path} ({target.NodeType.Name}) " +
                            $"because '{nt.Name}' type is not allowed in the new position.");

            // Invalidate pinned workspace reference because it may change
            // after the move operation (e.g. when restoring from the trash).
            _workspace = null;

            base.MoveTo(target);
        }

        /// <summary>
        /// Copies this Content under the given target <see cref="Node"/>.
        /// </summary>
        /// <param name="target">The parent <see cref="Node"/> of the new instance.</param>
        /// <param name="newName">String value of the new name or null if it should not be changed.</param>
        /// <returns>The new instance.</returns>
        public override Node MakeCopy(Node target, string newName)
        {
            var copy = base.MakeCopy(target, newName);
            var version = copy.Version;
            if (version.Status != VersionStatus.Locked)
                return copy;
            copy.Version = new VersionNumber(version.Major, version.Minor, version.IsMajor ? VersionStatus.Approved : VersionStatus.Draft);
            return copy;
        }
        /// <summary>
        /// Copies the dynamic property values to the given target.
        /// </summary>
        protected override void CopyDynamicProperties(Node target)
        {
            var content = (GenericContent)target;

            var sourceList = (ContentList)LoadContentList();
            Dictionary<string, string> crossBinding = null;
            if (sourceList != null)
            {
                var targetList = (ContentList)target.LoadContentList();
                if (targetList != null)
                    crossBinding = GetContentListCrossBinding(sourceList, targetList);
            }

            foreach (var propType in this.PropertyTypes)
            {
                if (EXCLUDED_COPY_PROPERTIES.Contains(propType.Name)) continue;

                string targetName;
                if (propType.IsContentListProperty)
                {
                    if (crossBinding == null)
                        continue;
                    crossBinding.TryGetValue(propType.Name, out targetName);
                    if (targetName == null || target.PropertyTypes[targetName] == null)
                        continue;
                }
                else
                {
                    targetName = propType.Name;
                }

                var propVal = this.GetProperty(propType.Name);
                if (!(propVal is BinaryData binProp))
                    content.SetProperty(targetName, propVal);
                else
                    content.GetBinary(targetName).CopyFromWithoutDbRead(binProp);
            }
        }
        internal Dictionary<string, string> GetContentListCrossBinding(ContentList source, ContentList target)
        {
            var result = new Dictionary<string, string>();
            var targetContentListBindings = target.ContentListBindings;
            foreach (var item in source.ContentListBindings)
            {
                if (item.Value != null && item.Value.Count == 1)
                {
                    targetContentListBindings.TryGetValue(item.Key, out var targetPropertyNames);
                    if (targetPropertyNames != null && targetPropertyNames.Count == 1)
                        result.Add(item.Value[0], targetPropertyNames[0]);
                }
            }
            return result;
        }


        /// <summary>
        /// Gets a boolean value that specifies whether the Content can be moved to the Trash.
        /// </summary>
        public virtual bool IsTrashable
        {
            get
            {
                GenericContent parentContent;
                using (new SystemAccount())
                    parentContent = Parent as GenericContent;

                if (parentContent == null)
                    return !this.TrashDisabled;
                else
                    return !(parentContent.TrashDisabled || this.TrashDisabled);
            }
        }

        /// <summary>
        /// Gets the total count of contents in the subtree under this Content.
        /// </summary>
        public override int NodesInTree => ContentQuery.Query(SafeQueries.InTreeCountOnly,
            new QuerySettings { EnableAutofilters = FilterStatus.Disabled, EnableLifespanFilter = FilterStatus.Disabled },
            this.Path).Count;

        /// <summary>
        /// Moves this Content and the whole subtree to the Trash if possible, otherwise deletes it physically.
        /// </summary>
        public override void Delete()
        {
            Delete(false);
        }

        /// <summary>
        /// Moves this Content and the whole subtree to the Trash if possible, otherwise deletes it physically.
        /// </summary>
        /// <param name="bypassTrash">Specifies whether the content should be deleted physically or only to the Trash.</param>
        public virtual void Delete(bool bypassTrash)
        {
            using (var op = SnTrace.ContentOperation.StartOperation("GC.Delete: VId:{0}, Path:{1}", this.VersionId, this.Path))
            {
                // let the TrashBin handle the delete operation:
                // only move the node to the trash or delete it permanently
                if (bypassTrash)
                    TrashBin.ForceDelete(this);
                else
                    TrashBin.DeleteNode(this);
                op.Successful = true;
            }
        }

        /// <summary>
        /// Gets the nearest <see cref="ContentList"/> from the parent chain or null.
        /// </summary>
        public GenericContent MostRelevantContext => ContentList.GetContentListForNode(this) ?? this;

        /// <summary>
        /// Gets the nearest <see cref="SystemFolder"/> from the parent chain or null.
        /// </summary>
        public GenericContent MostRelevantSystemContext => SystemFolder.GetSystemContext(this) ?? this;

        /// <inheritdoc/>
        public override string ToString()
        {
            return this.Name;
        }

        /// <summary>
        /// Returns the <see cref="Schema.ContentType"/> of this instance.
        /// </summary>
        public ContentType GetContentType()
        {
            return ContentType;
        }

        /// <summary>
        /// Gets or sets the icon name for this Content. Inherited classes may customize this value.
        /// By default the value of the Icon property of the instance's <see cref="Schema.ContentType"/> is returned.
        /// The setter throws an SnNotSupportedException in this class.
        /// </summary>
        public virtual string Icon
        {
            get => GetContentType().Icon;
            set => throw new SnNotSupportedException("Please implement Icon setter in your derived class");
        }

        /// <summary>
        /// Returns the <see cref="NodeHead"/> of the application Content specified by the given action name.
        /// In the default implementation this always returns the Browse application if specified, otherwise null.
        /// </summary>
        /// <param name="actionName">The name of the action (e.g. "Browse").</param>
        /// <returns>The <see cref="NodeHead"/> or null if it does not exist.</returns>
        public virtual NodeHead GetApplication(string actionName)
        {
            if (actionName == "Browse")
            {
                var app = this.BrowseApplication;
                if (app == null)
                    return null;

                return NodeHead.Get(app.Id);
            }

            return null;
        }

        /// <summary>
        /// Gets or sets the "Browse" application of this Content explicitly.
        /// Persisted as <see cref="RepositoryDataType.Reference"/>.
        /// </summary>
        [RepositoryProperty("BrowseApplication", RepositoryDataType.Reference)]
        public Node BrowseApplication
        {
            get => base.GetReference<Node>("BrowseApplication");
            set => this.SetReference("BrowseApplication", value);
        }

        // ==================================================== Versioning & Approving ====================================================

        private void VersionSetup()
        {
            if (this.Id == 0)
                this.Version = SavingAction.ComputeNewVersion(this.HasApproving, this.VersioningMode);
        }

        /// <summary>
        /// Persist this Content's changes.
        /// In derived classes to modify or extend the general persistence mechanism of a content, please
        /// override the <see cref="Save(NodeSaveSettings)"/> method instead, to avoid duplicate Save calls.
        /// </summary>
        public override void Save()
        {
            if (!IsNew && IsVersionChanged())
            {
                SaveExplicitVersion();
            }
            else if (Locked)
            {
                Save(this.IsLatestVersion ? SavingMode.KeepVersion : SavingMode.KeepVersionAndLock);
            }
            else
            {
                Save(SavingMode.RaiseVersion);
            }
        }
        private bool _savingExplicitVersion;
        /// <summary>
        /// Persist this Content's changes by the given mode.
        /// In derived classes to modify or extend the general persistence mechanism of a content, please
        /// override the <see cref="Save(NodeSaveSettings)"/> method instead, to avoid duplicate Save calls.
        /// </summary>
        /// <param name="mode"><see cref="SavingMode"/> that controls versioning.</param>
        public virtual void Save(SavingMode mode)
        {
            using (var op = SnTrace.ContentOperation.StartOperation("GC.Save: Mode:{0}, VId:{1}, Path:{2}", mode, this.VersionId, this.Path))
            {
                var action = SavingAction.Create(this);
                if (_savingExplicitVersion)
                {
                    SnTrace.ContentOperation.Write("GC.SavingExplicitVersion");
                    action.SaveExplicitVersion();
                    _savingExplicitVersion = false;
                }
                else
                {
                    switch (mode)
                    {
                        case SavingMode.RaiseVersion:
                            action.CheckOutAndSaveAndCheckIn();
                            break;
                        case SavingMode.RaiseVersionAndLock:
                            action.CheckOutAndSave();
                            break;
                        case SavingMode.KeepVersion:
                            action.SaveSameVersion();
                            break;
                        case SavingMode.KeepVersionAndLock:
                            action.SaveAndLock();
                            break;
                        case SavingMode.StartMultistepSave:
                            action.StartMultistepSave();
                            break;
                        default:
                            throw new SnNotSupportedException("Unknown SavingMode: " + mode);
                    }
                }

                action.Execute();

                op.Successful = true;
            }
        }
        /// <summary>
        /// Persist this Content's changes by the given settings.
        /// </summary>
        /// <param name="settings"><see cref="NodeSaveSettings"/> that contains the algorithm of the persistence.</param>
        public override void Save(NodeSaveSettings settings)
        {
            base.Save(settings);

            // if related workflows should be kept alive, update them on a separate thread
            if (_keepWorkflowsAlive)
                System.Threading.Tasks.Task.Run(() => UpdateRelatedWorkflows());

            if(_content != null)
                foreach(var field in _content.Fields.Values)
                    field.Reset();
        }

        private bool _keepWorkflowsAlive;
        /// <summary>
        /// Tells the system that the next Save operation should not abort workflows that this Content is attached to.
        /// </summary>
        public void KeepWorkflowsAlive()
        {
            _keepWorkflowsAlive = true;
            DisableObserver(TypeResolver.GetType(NodeObserverNames.WORKFLOWNOTIFICATION, false));
        }

        private void UpdateRelatedWorkflows()
        {
            // Update workflows only if the Workflow component is installed - otherwise
            // the query below would lead to an Unknown field exception.
            if (!RepositoryVersionInfo.Instance.Components.Any(c =>
                string.Equals(c.ComponentId, "SenseNet.Workflow", StringComparison.InvariantCultureIgnoreCase)))
            {
                _keepWorkflowsAlive = false;
                return;
            }

            // Certain workflows (e.g. approving) are designed to be aborted when a Content changes, but in case
            // certain system operations (e.g. updating a page count on a document) this should not happen.
            // So after saving the Content we update the related workflows to contain the same timestamp
            // as the Content has. This will prevent the workflows from aborting next time they wake up.
            using (new SystemAccount())
            {
                var newTimeStamp = this.NodeTimestamp;

                // query all workflows in the system that have this content as their related content
                var nodes = SearchManager.ContentQueryIsAllowed
                    ? ContentQuery.Query(SafeQueries.WorkflowsByRelatedContent, null, this.Id).Nodes
                    : NodeQuery.QueryNodesByReferenceAndType("RelatedContent", this.Id,
                        ActiveSchema.NodeTypes["Workflow"], false).Nodes;

                foreach (var workflow in nodes.Cast<GenericContent>())
                {
                    try
                    {
                        // We have to use the GetProperty/SetProperty API here
                        // to go through the content handler instead of directly
                        // writing to the node data.
                        var relatedTimeStamp = (long)workflow.GetProperty("RelatedContentTimestamp");
                        if (relatedTimeStamp == newTimeStamp)
                            continue;

                        workflow.SetProperty("RelatedContentTimestamp", newTimeStamp);
                        workflow.Save();
                    }
                    catch (Exception ex)
                    {
                        SnLog.WriteException(ex);
                    }
                }
            }

            // reset the flag: the developer should switch it on intentionally every time this functionality is needed
            _keepWorkflowsAlive = false;
        }

        /// <summary>
        /// Check this Content out. Enables modifications for the currently logged in user exclusively.
        /// Enables other users to access it but only for reading.
        /// After this operation the version of the Content is always raised even if the versioning mode is "off".
        /// </summary>
        public virtual void CheckOut()
        {
            using (var op = SnTrace.ContentOperation.StartOperation("GC.CheckOut: VId:{0}, Path:{1}", this.VersionId, this.Path))
            {
                var prevVersion = this.Version;
                var action = SavingAction.Create(this);
                action.CheckOut();
                action.Execute();

                // Workaround: the OnModified event is not fired in case the
                // content is locked, so we need to copy preview images here.
                if (DocumentPreviewProvider.Current.IsContentSupported(this))
                    // ReSharper disable once ArrangeStaticMemberQualifier
                    DocumentPreviewProvider.Current.StartCopyingPreviewImages(Node.LoadNode(this.Id, prevVersion), this);

                op.Successful = true;
            }
        }
        /// <summary>
        /// Commits the modifications of the checked out Content and releases the lock.
        /// </summary>
        public virtual void CheckIn()
        {
            using (var op = SnTrace.ContentOperation.StartOperation("GC.CheckIn: VId:{0}, Path:{1}", this.VersionId, this.Path))
            {
                var action = SavingAction.Create(this);
                action.CheckIn();
                action.Execute();

                op.Successful = true;
            }
        }
        /// <summary>
        /// Reverts the Content to the state before the user checked it out and reloads it.
        /// </summary>
        public virtual void UndoCheckOut()
        {
            UndoCheckOut(true);
        }

        /// <summary>
        /// Reverts the Content to the state before the user checked it out.
        /// If the "'forceRefresh" parameter is true, the Content will be reloaded.
        /// </summary>
        /// <param name="forceRefresh">Optional boolean value that specifies
        /// whether the Content will be reloaded or not. Default: true.</param>
        public void UndoCheckOut(bool forceRefresh)
        {
            using (var op = SnTrace.ContentOperation.StartOperation("GC.UndoCheckOut: forceRefresh:{0}, VId:{1}, Path:{2}", forceRefresh, this.VersionId, this.Path))
            {
                // this flag will be used by the preview observer to check whether it should start generating preview images
                this.NodeOperation = Storage.NodeOperation.UndoCheckOut;

                var action = SavingAction.Create(this);
                action.UndoCheckOut(forceRefresh);
                action.Execute();

                op.Successful = true;
            }
        }

        /// <summary>
        /// Publishes the Content. Depending on the versioning workflow, the version
        /// will be the next public version with Approved state or remains the same but the 
        /// versioning state will be changed to Pending.
        /// </summary>
        public virtual void Publish()
        {
            using (var op = SnTrace.ContentOperation.StartOperation("GC.Publish: VId:{0}, Path:{1}", this.VersionId, this.Path))
            {
                var action = SavingAction.Create(this);
                action.Publish();
                action.Execute();

                op.Successful = true;
            }
        }
        /// <summary>
        /// Approves the Content. After this action the Content's version number
        /// (depending on the mode) will be raised to the next public version.
        /// </summary>
        public virtual void Approve()
        {
            using (var op = SnTrace.ContentOperation.StartOperation("GC.Approve: VId:{0}, Path:{1}", this.VersionId, this.Path))
            {
                // When we approve a Content we clear the reject reason because it would be confusing on an approved 
                // Content. In case of minor versioning the reason text will still be available on previous versions.
                this["RejectReason"] = string.Empty;

                var action = SavingAction.Create(this);
                action.Approve();
                action.Execute();

                op.Successful = true;
            }
        }
        /// <summary>
        /// Rejects the approvable Content. After this action the Content's version number
        /// remains the same but the versioning state of the Content will be changed to Rejected.
        /// </summary>
        public virtual void Reject()
        {
            using (var op = SnTrace.ContentOperation.StartOperation("GC.Reject: VId:{0}, Path:{1}", this.VersionId, this.Path))
            {
                var action = SavingAction.Create(this);
                action.Reject();
                action.Execute();

                op.Successful = true;
            }
        }
        internal void SaveExplicitVersion()
        {
            var update = false;
            if (!IsNew)
            {
                var head = NodeHead.Get(this.Id);
                if (head != null)
                {
                    if (this.SavingState != ContentSavingState.Finalized)
                        throw new InvalidContentActionException("Only 'Finalized' content can be saved with explicit new version.");

                    var lastDraft = head.GetLastMinorVersion();
                    var st = lastDraft.VersionNumber.Status;
                    if (st != VersionStatus.Approved && st != VersionStatus.Draft)
                        throw new InvalidContentActionException("Only 'Approved' or 'Draft' content version can be saved with explicit new version.");

                    if (this.Version < lastDraft.VersionNumber)
                        throw new InvalidContentActionException("Only the latest or greater version is allowed to save. Latest version: " + lastDraft.VersionNumber);

                    update = this.Version == lastDraft.VersionNumber;
                }
            }
            _savingExplicitVersion = true;

            Save(update ? SavingMode.KeepVersion : SavingMode.RaiseVersion);
        }

        /// <summary>
        /// Ends the multistep saving process and makes the Content available for modification.
        /// </summary>
        public override void FinalizeContent()
        {
            using (var op = SnTrace.ContentOperation.StartOperation("GC.FinalizeContent: SavingState:{0}, VId:{1}, Path:{2}", this.SavingState, this.VersionId, this.Path))
            {
                if (this.Locked && (this.SavingState == ContentSavingState.Creating || this.SavingState == ContentSavingState.Modifying))
                {
                    var action = SavingAction.Create(this);
                    action.CheckIn();
                    action.Execute();
                }
                else
                {
                    base.FinalizeContent();
                }

                op.Successful = true;
            }
        }

        /// <summary>
        /// Gets the boolean value that describes whether the Content versioning
        /// workflow and the current user's permissions enable the Approve function.
        /// </summary>
        public bool Approvable
        {
            get
            {
                // field setting is a special content that is not represented in the security component
                if (this is FieldSettingContent || !this.Security.HasPermission(PermissionType.Open))
                    return false;
                return SavingAction.HasApprove(this);
            }
        }
        /// <summary>
        /// Gets the boolean value that describes whether the Content versioning
        /// workflow and the current user's permissions enable the Publish function.
        /// </summary>
        public bool Publishable
        {
            get
            {
                // field setting is a special content that is not represented in the security component
                if (this is FieldSettingContent || !this.Security.HasPermission(PermissionType.Open))
                    return false;
                return SavingAction.HasPublish(this);
            }
        }
        /// <summary>
        /// Gets the permitted old versions. See <see cref="Node.LoadVersions"/>.
        /// </summary>
        public IEnumerable<Node> Versions => LoadVersions();

        // ==================================================== Children obsolete

        /// <summary>
        /// Returns with children of this Content. THIS METHOD IS OBSOLETE.
        /// Use Children property of any inherited class that implements the <see cref="IFolder"/> interface.
        /// Use ChildrenDefinition property for tuning the settings of the children collection.
        /// </summary>
        [Obsolete("Use declarative concept instead: ChildrenDefinition")]
        public virtual QueryResult GetChildren(QuerySettings settings)
        {
            return GetChildren(string.Empty, settings);
        }
        /// <summary>
        /// Returns with children of this Content. THIS METHOD IS OBSOLETE.
        /// Use Children property of any inherited class that implements the <see cref="IFolder"/> interface.
        /// Use ChildrenDefinition property for tuning the settings of the children collection.
        /// </summary>
        [Obsolete("Use declarative concept instead: ChildrenDefinition")]
        public virtual QueryResult GetChildren(string text, QuerySettings settings)
        {
            return GetChildren(text, settings, false);
        }
        /// <summary>
        /// Returns with children of this Content. THIS METHOD IS OBSOLETE.
        /// Use Children property of any inherited class that implements the <see cref="IFolder"/> interface.
        /// Use ChildrenDefinition property for tuning the settings of the children collection.
        /// </summary>
        [Obsolete("Use declarative concept instead: ChildrenDefinition")]
        public virtual QueryResult GetChildren(string text, QuerySettings settings, bool getAllChildren)
        {
            if (SearchManager.ContentQueryIsAllowed)
            {
                var query = ContentQuery.CreateQuery(getAllChildren ? SafeQueries.InTree : SafeQueries.InFolder, settings, this.Path);
                if (!string.IsNullOrEmpty(text))
                    query.AddClause(text);
                return query.Execute();
            }
            else
            {
                var nqr = NodeQuery.QueryChildren(this.Path);
                return new QueryResult(nqr.Identifiers, nqr.Count);
            }
        }

        // ==================================================== Children

        /// <summary>
        /// Protected member that gives access to the raw value of the ChildrenDefinition property.
        /// </summary>
        protected ChildrenDefinition _childrenDefinition;
        /// <summary>
        /// Gets or sets the children definition of this Content. This can be a static list of
        /// child items or a Content Query with settings that define a query for child items.
        /// </summary>
        public virtual ChildrenDefinition ChildrenDefinition
        {
            get => _childrenDefinition ?? (_childrenDefinition = ChildrenDefinition.Default);
            set => _childrenDefinition = value;
        }

        internal ISnQueryable<Content> GetQueryableChildren()
        {
            return new ContentSet<Content>(this.ChildrenDefinition.Clone(), this.Path);
        }

        // ==================================================== IIndexable Members

        /// <summary>
        /// Returns a collection of <see cref="Field"/>s that will be indexed for this Content.
        /// </summary>
        /// <remarks>The fields are accessed through the wrapper <see cref="ContentRepository.Content"/> of this instance.</remarks>
        public virtual IEnumerable<IIndexableField> GetIndexableFields()
        {
            var content = Content;
            var fields = content.Fields.Values;
            var indexableFields = fields.Where(f => f.IsInIndex).Cast<IIndexableField>().ToArray();
            return indexableFields;
        }
    }
}
