using System;
using System.Collections.Generic;
using System.Linq;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Caching.Dependency;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Storage.Search;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.ContentRepository.Versioning;
using SenseNet.Diagnostics;
using System.Text;
using SenseNet.Search;
using SenseNet.Search.Indexing;
using System.Diagnostics;
using SenseNet.ContentRepository.Linq;
using SenseNet.Preview;
using SenseNet.ContentRepository.Storage.Events;
using SenseNet.Tools;

namespace SenseNet.ContentRepository
{
    /// <summary>
    /// Defines constants for Content-saving algorithm selection.
    /// </summary>
    public enum SavingMode
    {
        /// <summary>After saving, the Content will have a new higher version.
        /// The value of the new version is depends from the current VersioningMode.</summary>
        RaiseVersion,
        /// <summary>After saving, the Content will have a new higher version and will be locked for the current user.
        /// The value of the new version is depends from the current VersioningMode.</summary>
        RaiseVersionAndLock,
        /// <summary>After saving, the Content's version will not be changed.</summary>
        KeepVersion,
        /// <summary>After saving, the Content's version will not be changed but will be locked for the current user.</summary>
        KeepVersionAndLock,
        /// <summary>After saving, the Content will be in multistep saving state.</summary>
        StartMultistepSave
    }

    /// <summary>
    /// Defines constants for the checkin comment policy of the current Content during execution of the CheckiIn action.
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
    /// Defines constants for the Path interpretation mode in the programmed Content queries.
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
        public static ChildrenDefinition Default { get { return new ChildrenDefinition { PathUsage = PathUsageMode.InFolderAnd }; } }

        /// <summary>
        /// Gets or sets the value of the owner Content's path interpretation mode.
        /// </summary>
        public PathUsageMode PathUsage { get; set; }
        /// <summary>
        /// Gets or sets the CQL text if the children are produced by a Content query.
        /// Not affected if the BaseCollection property is not null.
        /// </summary>
        public string ContentQuery { get; set; }

        /// <summary>
        /// Gets or sets a predefined children collection.
        /// Eliminates the ContentQuery property if the value is not null.
        /// </summary>
        public IEnumerable<Node> BaseCollection { get; set; }

        /// <summary>
        /// Gets or sets the extension value of the query result maximization for the Content Query property.
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
        /// Gets or sets the value for the ContentQuery property that is true if the Count property of the query result contains the total count of hits.
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
        /// Gets or sets the <see cref="SenseNet.Search.QueryExecutionMode"/> extension value for the Content Query property.
        /// </summary>
        public QueryExecutionMode QueryExecutionMode { get; set; }

        /// <summary>
        /// Gets or sets the value that is true if the value of the PathUsage property is InTreeAnd or InTreeOr.
        /// </summary>
        public bool AllChildren
        {
            get { return PathUsage == PathUsageMode.InTreeAnd || PathUsage == PathUsageMode.InTreeOr; }
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
            return ContentTypeManager.Current.ContentTypes.Count;
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
            return ContentTypeManager.Current.ContentTypes.Values.GetEnumerator();
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
            return ContentTypeManager.Current.ContentTypes.Count;
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
            return ContentTypeManager.Current.ContentTypes.Keys.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    /// <summary>
    /// Defines a class that can handle all types of Content (except <see cref="Schema.ContentType"/>) in the sensenet Content repository.
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
        /// Initializes a new instance of the <see cref="GenericContent"/> class in the loading procedure.
        /// Do not use this constructor directly from your code.
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
        public Content Content
        {
            get { return _content ?? (_content = Content.Create(this)); }
        }

        /// <summary>
        /// Initializes the default field values if the instance is a new one.
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

                if (Parent == null)
                    IsSystem = false;
                else
                    IsSystem = Parent.IsSystem; 
            }
        }

        /// <summary>
        /// Gets the <see cref="SenseNet.ContentRepository.Schema.ContentType"/> of this instance.
        /// </summary>
        public ContentType ContentType
        {
            get { return ContentType.GetByName(this.NodeType.Name); }
        }

        /// <summary>
        /// Gets whether this class is <see cref="SenseNet.ContentRepository.Schema.ContentType"/>.
        /// The value is false in this case.
        /// </summary>
        public override bool IsContentType { get { return false; } }

        /// <summary>
        /// Gets or sets the user friendly name of this instance.
        /// The value in each version is the same.
        /// </summary>
        public override string DisplayName
        {
            get
            {
                var result = base.DisplayName;
                if (String.IsNullOrEmpty(result) && this.Id != 0)
                    result = this.Name;
                return result;
            }
            set
            {
                base.DisplayName = value;
            }
        }

        /// <summary>
        /// Gets or sets the description of this instance. Persisted as <see cref="RepositoryDataType.Text"/>.
        /// </summary>
        [RepositoryProperty("Description", RepositoryDataType.Text)]
        public virtual string Description
        {
            get
            {
                return base.GetProperty<string>("Description");
            }
            set
            {
                this["Description"] = value;
            }
        }

        /// <summary>
        /// Gets or sets whether this instance is hidden or not. Persisted as <see cref="RepositoryDataType.Int"/>.
        /// </summary>
        [RepositoryProperty("Hidden", RepositoryDataType.Int)]
        public virtual bool Hidden
        {
            get { return base.GetProperty<int>("Hidden") != 0; }
            set { this["Hidden"] = value ? 1 : 0; }
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
                    var parent = this.Parent as GenericContent;
                    return parent == null ? VersioningType.None : (VersioningType)parent.InheritableVersioningMode;
                }
            }
            set
            {
                this[VERSIONINGMODE] = value;
            }
        }

        /// <summary>
        /// Gets or sets the versioning mode of the new child Content.
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
                    var parent = this.Parent as GenericContent;
                    return parent == null ? InheritableVersioningType.None : parent.InheritableVersioningMode;
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
                    var parent = this.Parent as GenericContent;
                    return parent == null ? ApprovingType.False : parent.InheritableApprovingMode;
                }
            }
            set
            {
                if (HasProperty("ApprovingMode"))
                    this["ApprovingMode"] = value;
            }
        }

        /// <summary>
        /// Gets or sets the approving mode of of the new child Content.
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
                    var parent = this.Parent as GenericContent;
                    return parent == null ? ApprovingType.False : parent.InheritableApprovingMode;
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
        /// Gets or sets the collection of the allowed <see cref="Schema.ContentType"/>.
        /// The type inheritance is ignored in this case, all affected exact types have to be in the collection.
        /// The <see cref="Schema.ContentType"/> of a new child Content need to be in the collection.
        /// Persisted as <see cref="RepositoryDataType.Text"/>.
        /// </summary>
        [RepositoryProperty(ALLOWEDCHILDTYPES, RepositoryDataType.Text)]
        public virtual IEnumerable<ContentType> AllowedChildTypes
        {
            get
            {
                var value = this.GetProperty<string>(ALLOWEDCHILDTYPES);
                if (String.IsNullOrEmpty(value))
                    return ContentType.EmptyAllowedChildTypes;
                var names =     value.Split(ContentType.XmlListSeparators, StringSplitOptions.RemoveEmptyEntries);
                var result = new List<ContentType>(names.Length);
                ContentType ct;
                for (int i = 0; i < names.Length; i++)
                    if ((ct = ContentType.GetByName(names[i])) != null)
                        result.Add(ct);
                return result;
            }
            set
            {
                var names = value == null ? null : String.Join(" ", value.Select(x => x.Name));
                this[ALLOWEDCHILDTYPES] = names;
            }
        }

        /// <summary>
        /// Gets the effective collection of the allowed <see cref="Schema.ContentType"/>.
        /// Contains all allowed types except that id permitted for the current user.
        /// The type inheritance is ignored in this case, all affected exact types have to be in the collection.
        /// The <see cref="Schema.ContentType"/> of a new child Content need to be in the collection.
        /// </summary>
        public virtual IEnumerable<ContentType> EffectiveAllowedChildTypes
        {
            get
            {
                return GetAllowedChildTypes();
            }
        }

        /// <summary>
        /// Gets the <see cref="SenseNet.ContentRepository.CheckInCommentsMode"/> policy of this class.
        /// Returns with <see cref="SenseNet.ContentRepository.CheckInCommentsMode.None"/> in this case.
        /// Override this property in the inherited Content handlers to customize this behavior.
        /// </summary>
        public virtual CheckInCommentsMode CheckInCommentsMode
        {
            get
            {
                // overwritten in File type!
                return CheckInCommentsMode.None;
            }
        }

        /// <summary>
        /// Gets the <see cref="User"/> who locked exclusively this Content.
        /// This property is similar to the LockedBy property.
        /// </summary>
        public User CheckedOutTo
        {
            get
            {
                return this.LockedBy as User;
            }
        }

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
        public bool HasApproving
        {
            get
            {
                return this.ApprovingMode == ApprovingType.True ? true : false;
            }
        }

        private GenericContent _workspace;
        /// <summary>
        /// Gets the nearest Content in the parent chain that's ContentType is "Workspace" or inherited from it.
        /// Returns null if there is no any Workspace in the parent chain.
        /// </summary>
        public GenericContent Workspace
        {
            get
            {
                if (_workspace == null)
                {
                    _workspace = Node.GetAncestorOfNodeType(this, "Workspace") as GenericContent;
                }

                return _workspace;
            }
        }

        /// <summary>
        /// Gets the name of the Workspace if exists or null.
        /// </summary>
        public string WorkspaceName
        {
            get { return this.Workspace == null ? string.Empty : this.Workspace.Name; }
        }

        /// <summary>
        /// Gets the title of the Workspace if exists or null.
        /// </summary>
        public string WorkspaceTitle
        {
            get { return this.Workspace == null ? string.Empty : this.Workspace.DisplayName; }
        }

        /// <summary>
        /// Gets the path of the Workspace if exists or null.
        /// </summary>
        public string WorkspacePath
        {
            get { return this.Workspace == null ? string.Empty : this.Workspace.Path; }
        }

        /// <summary>
        /// Gets or sets a value that is true if this Content can be moved to the Trash or not.
        /// Persisted as <see cref="RepositoryDataType.Int"/>.
        /// </summary>
        [RepositoryProperty("TrashDisabled", RepositoryDataType.Int)]
        public bool TrashDisabled
        {
            get
            {
                //TODO: re-think trash enabled/disabled logic
                return base.GetProperty<int>("TrashDisabled") != 0;
            }
            set { this["TrashDisabled"] = value ? 1 : 0; }
        }

        /// <summary>
        /// Gets or sets whether this instance under lifespan control or not.
        /// Persisted as <see cref="RepositoryDataType.Int"/>.
        /// </summary>
        [RepositoryProperty("EnableLifespan", RepositoryDataType.Int)]
        public bool EnableLifespan
        {
            get { return base.GetProperty<int>("EnableLifespan") != 0; }
            set { this["EnableLifespan"] = value ? 1 : 0; }
        }

        /// <summary>
        /// Gets or sets the DateTime of the start of  this instance's lifespan.
        /// Persisted as <see cref="RepositoryDataType.DateTime"/>.
        /// </summary>
        [RepositoryProperty("ValidFrom", RepositoryDataType.DateTime)]
        public DateTime ValidFrom
        {
            get { return base.GetProperty<DateTime>("ValidFrom"); }
            set { this["ValidFrom"] = value; }
        }

        /// <summary>
        /// Gets or sets the DateTime of the end of  this instance's lifespan.
        /// Persisted as <see cref="RepositoryDataType.DateTime"/>.
        /// </summary>
        [RepositoryProperty("ValidTill", RepositoryDataType.DateTime)]
        public DateTime ValidTill
        {
            get { return base.GetProperty<DateTime>("ValidTill"); }
            set { this["ValidTill"] = value; }
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
            get { return base.GetReferences(ASPECTS).Cast<Aspect>(); }
            set { this.SetReferences(ASPECTS, value); }
        }

        /// <summary>
        /// Defines a constant value for the name of the AspectData property.
        /// </summary>
        public const string ASPECTDATA = "AspectData";
        /// <summary>
        /// Gets or sets the all field values of all associated <see cref="Aspect"/>s.
        /// Persisted as <see cref="RepositoryDataType.Text"/>.
        /// </summary>
        [RepositoryProperty(ASPECTDATA, RepositoryDataType.Text)]
        public string AspectData
        {
            get { return base.GetProperty<string>(ASPECTDATA); }
            set { this[ASPECTDATA] = value; }
        }

        /// <summary>
        /// Gets true if the inherited class implements the <see cref="IFolder"/> interface.
        /// </summary>
        public bool IsFolder
        {
            get { return this is IFolder; }
        }

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
        /// Gets true if this instance is enabled to indexing.
        /// This is shortcut of similar property of the current ContentType.
        /// </summary>
        protected override bool IsIndexingEnabled
        {
            get
            {
                // this is configured on the Content type (AllowIndexing xml node in the header of the CTD)
                return this.ContentType.IndexingEnabled;
            }
        }

        /// <summary>
        /// Returns a property value by name. Well known and dynamic properties also can accessed.
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
                default:
                    return base[name];
            }
        }
        /// <summary>
        /// Assigns the given value to the named property. Well known and dynamic properties also can accessed.
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
                    this.ValidFrom = value == null ? DateTime.MinValue : (DateTime)value;
                    break;
                case "ValidTill":
                    this.ValidTill = value == null ? DateTime.MinValue : (DateTime)value;
                    break;
                case "Workspace":
                case "WorkspaceName":
                case "WorkspacePath":
                case "WorkspaceTitle":
                case "Approvable":
                case "Publishable":
                case "Versions":
                case "CheckedOutTo":
                    // do nothing, these props are readonly
                    break;
                default:
                    base[name] = value;
                    break;
            }
        }

        /// <summary>
        /// Returns the collection of the referrers from the following properties:
        /// CreatedById, ModifiedById, VersionCreatedById, VersionModifiedById, LockedById.
        /// The collection does not contain any referrers in the dynamic reference properties.
        /// This Content is also excluded from the collection.
        /// </summary>
        /// <param name="top">Maximum count of the collection</param>
        /// <param name="totalCount">Output value of the total count of the referrers regardless the value of the "top" parameter.</param>
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
        /// Contains all allowed type names except that are not permitted for the current user.
        /// The type inheritance is ignored in this case, all affected exact types have to be in the collection.
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
                var parent = SystemAccount.Execute(() => { return Parent as GenericContent; });
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
        /// Returns the effective collection of the allowed <see cref="Schema.ContentType"/>.
        /// Contains all allowed types except that are not permitted for the current user.
        /// The type inheritance is ignored in this case, all affected exact types have to be in the collection.
        /// The <see cref="Schema.ContentType"/> of a new child Content need to be in the collection.
        /// </summary>
        public IEnumerable<ContentType> GetAllowedChildTypes()
        {
            // in case of folders and pages inherit settings from parent
            if (this.NodeType.Name == "Folder" || this.NodeType.Name == "Page")
            {
                GenericContent parent = null;
                using (new SystemAccount())
                    parent = Parent as GenericContent;
                if (parent == null)
                    return ContentType.EmptyAllowedChildTypes;
                return parent.GetAllowedChildTypes();
            }

            // collect types set on local instance
            var types = new List<ContentType>();
            foreach (var ct in this.AllowedChildTypes)
                if (ct.Security.HasPermission(PermissionType.See))
                    types.Add(ct);

            // Indicates that the local list has items. The length of list is not enough because the permission filters
            // the list and if the filter skips all elements, the user gets the list that declared on the Content type.
            var hasLocalItems = types.Count > 0;

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
        /// Returns true if the current user can create a Content by the given <see cref="Schema.ContentType"/> name.
        /// </summary>
        public bool IsAllowedChildType(string contentTypeName)
        {
            if (this is SystemFolder || this is TrashBag)
                return true;
            var list = GetAllowedChildTypeNames();
            return list.Contains(contentTypeName);
        }
        /// <summary>
        /// Returns true if the current user can create a Content by the given <see cref="Schema.ContentType"/>.
        /// </summary>
        public bool IsAllowedChildType(ContentType contentType)
        {
            return IsAllowedChildType(contentType.Name);
        }

        /// <summary>
        /// Defines constans for the verification of the child type.
        /// </summary>
        public enum TypeAllow //UNDONE: Make obsolete. Be private.
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
                default:
                    break;
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
            var nodePath = String.Concat(node.ParentPath, "/", node.Name);

            return new InvalidOperationException(String.Format("Cannot save the content '{0}' because its ancestor does not allow the type '{1}'. Ancestor: {2} ({3}). Allowed types: {4}"
                , nodePath, contentTypeName, ancestor.Path, ancestor.NodeType.Name, String.Join(", ", parent.GetAllowedChildTypeNames())));
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

                return new InvalidOperationException(String.Format("Cannot move the content '{0}' to '{1}' because target's ancestor does not allow the type '{2}'. Ancestor: {3} ({4}). Allowed types: {5}"
                    , node.Path, target.Path, contentTypeName, ancestor.Path, ancestor.NodeType.Name, String.Join(", ", target.GetAllowedChildTypeNames())));
            }
        }
        /// <summary>
        /// Allow type of children by the given parameters.
        /// The original list will be expanded to the given <see cref="SenseNet.ContentRepository.Schema.ContentType"/>>.
        /// </summary>
        /// <param name="contentTypeName">The name of the allowed <see cref="Schema.ContentType"/>.</param>
        /// <param name="setOnAncestorIfInherits">Optional parameter.
        /// If true, inherited types of the "contentType" also added to allowed list.
        /// Default: false.</param>
        /// <param name="throwOnError">Specifies whether an error will be thrown when the operation is unsuccessful. Optional, default: true.</param>
        /// <param name="save">Optional parameter that is true if the Content will be saved automatically after setting the new collection.
        /// Default: false</param>
        public void AllowChildType(string contentTypeName, bool setOnAncestorIfInherits = false, bool throwOnError = true, bool save = false)
        {
            AllowChildTypes(new[] { contentTypeName }, setOnAncestorIfInherits, throwOnError, save);
        }
        /// <summary>
        /// Allow type of children by the given parameters.
        /// The original list will be expanded to the given <see cref="SenseNet.ContentRepository.Schema.ContentType"/>>.
        /// </summary>
        /// <param name="contentType">The allowed <see cref="Schema.ContentType"/>.</param>
        /// <param name="setOnAncestorIfInherits">Optional parameter.
        /// If true, inherited types of the "contentType" also added to allowed list.
        /// Default: false.</param>
        /// <param name="throwOnError">Specifies whether an error will be thrown when the operation is unsuccessful. Optional, default: true.</param>
        /// <param name="save">Optional parameter that is true if the Content will be saved automatically after setting the new collection.
        /// Default: false</param>
        public void AllowChildType(ContentType contentType, bool setOnAncestorIfInherits = false, bool throwOnError = true, bool save = false)
        {
            AllowChildTypes(new[] { contentType }, setOnAncestorIfInherits, throwOnError, save);
        }
        /// <summary>
        /// Allow types of children by the given parameters.
        /// The original list will be expanded to the given collection.
        /// </summary>
        /// <param name="contentTypeNames">The name collection of the allowed <see cref="Schema.ContentType"/>s.</param>
        /// <param name="setOnAncestorIfInherits">Optional parameter.
        /// If true, all inherited types of the items in "contentType" collection also added to allowed list.
        /// Default: false.</param>
        /// <param name="throwOnError">Specifies whether an error will be thrown when the operation is unsuccessful. Optional, default: true.</param>
        /// <param name="save">Optional parameter that is true if the Content will be saved automatically after setting the new collection.
        /// Default: false</param>
        public void AllowChildTypes(IEnumerable<string> contentTypeNames, bool setOnAncestorIfInherits = false, bool throwOnError = true, bool save = false)
        {
            AllowChildTypes(contentTypeNames.Select(n => ContentType.GetByName(n)).Where(x => x != null), setOnAncestorIfInherits, throwOnError, save);
        }
        /// <summary>
        /// Allow types of children by the given parameters.
        /// The original list will be expanded to the given collection.
        /// </summary>
        /// <param name="contentTypes">The new collection of the allowed <see cref="Schema.ContentType"/>.</param>
        /// <param name="setOnAncestorIfInherits">Optional parameter.
        /// If true, all inherited types of the items in "contentType" collection also added to allowed list.
        /// Default: false.</param>
        /// <param name="throwOnError">Specifies whether an error will be thrown when the operation is unsuccessful. Optional, default: true.</param>
        /// <param name="save">Optional parameter that is true if the Content will be saved automatically after setting the new collection.
        /// Default: false</param>
        public void AllowChildTypes(IEnumerable<ContentType> contentTypes, bool setOnAncestorIfInherits = false, bool throwOnError = true, bool save = false)
        {
            if (contentTypes == null)
                throw new ArgumentNullException("contentTypes");
            switch (this.NodeType.Name)
            {
                case "Folder":
                case "Page":
                    if (setOnAncestorIfInherits)
                    {
                        GenericContent parent = null;
                        using (new SystemAccount())
                            parent = this.Parent as GenericContent;

                        if (parent != null)
                        {
                            parent.AllowChildTypes(contentTypes, setOnAncestorIfInherits, throwOnError, true);
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
                    SetAllowedChildTypes(contentTypes, throwOnError, save); 
                    return;
            }
        }
        private void SetAllowedChildTypes(IEnumerable<ContentType> contentTypes, bool throwOnError = true, bool save = false)
        {
            var origTypes = this.AllowedChildTypes.ToArray();
            if (origTypes.Length == 0)
                origTypes = this.ContentType.AllowedChildTypes.ToArray();

            var newTypes = contentTypes?.ToArray() ?? new ContentType[0];

            var addList = newTypes.Except(origTypes).ToArray();
            var removeList = origTypes.Except(newTypes).ToArray();
            if (addList.Length + removeList.Length == 0)
                return;

            var list = origTypes.Union(newTypes).Distinct().Except(removeList);
            this.AllowedChildTypes = list.ToArray();

            if (save)
                this.Save();
        }
        private Exception GetCannotAllowContentTypeException()
        {
            return new InvalidOperationException(String.Format("Cannot allow ContentType on a {0}. Path: {1}", this.NodeType.Name, this.Path));
        }

        /// <summary>
        /// Extends the given Content's AllowedChildTypes collection with the specified Content types.
        /// The Content will be saved after the operation.
        /// This is an <see cref="ODataAction"/>. 
        /// </summary>
        /// <param name="content">The <see cref="SenseNet.ContentRepository.Content"/> that will be modified.</param>
        /// <param name="contentTypes">The extension.</param>
        /// <returns>Empty string.</returns>
        [ODataAction]
        public static string AddAllowedChildTypes(Content content, string[] contentTypes)
        {
            var gc = content.ContentHandler as GenericContent;
            if (gc == null)
                return String.Empty;

            foreach (var contentTypeName in contentTypes)
                gc.AllowChildType(contentTypeName);
            gc.Save();

            return String.Empty;
        }
        /// <summary>
        /// Removes the specified Content types from the given Content's AllowedChildTypes collection.
        /// The Content will be saved after the operation.
        /// This is an <see cref="ODataAction"/>. 
        /// </summary>
        /// <param name="content">The <see cref="SenseNet.ContentRepository.Content"/> that will be modified.</param>
        /// <param name="contentTypes">The items that will be removed.</param>
        /// <returns>Empty string.</returns>
        [ODataAction]
        public static string RemoveAllowedChildTypes(Content content, string[] contentTypes)
        {
            var gc = content.ContentHandler as GenericContent;
            if (gc == null)
                return String.Empty;

            var remainingNames = gc
                .GetAllowedChildTypeNames(false)
                .Except(contentTypes)
                .ToArray();

            IEnumerable<ContentType> remainingTypes = null;
            var ctdNames = content.ContentType.AllowedChildTypeNames;
            if (0 < (ctdNames.Except(remainingNames).Count() + remainingNames.Except(ctdNames).Count()))
                remainingTypes = remainingNames.Select(x => ContentType.GetByName(x)).ToArray();

            gc.AllowedChildTypes = remainingTypes;
            gc.Save();

            return String.Empty;
        }

        //UNDONE:! XMLDOC:
        public string CheckChildrenTypeConsistence()
        {
            var result = new StringBuilder();
            result.AppendLine("Path\tType\tChild name\tChild type\tReason");
            foreach (var node in NodeEnumerator.GetNodes(this.Path, ExecutionHint.ForceRelationalEngine))
            {
                var parentGC = node.Parent as GenericContent;
                if (parentGC == null)
                    continue;

                var checkResult = parentGC.CheckAllowedChildType(node);
                if(checkResult != TypeAllow.Allowed)
                {
                    result.AppendFormat("{0}\t{1}\t{2}\t{3}\t{4}\r\n", parentGC.Path, parentGC.NodeType.Name, node.Name, node.NodeType.Name, 
                        String.Join(", ", parentGC.GetAllowedChildTypeNames()), checkResult);
                    result.AppendLine();
                }
            }
            return result.ToString();
        }

        // =============================================================================================

        //UNDONE:! XMLDOC:
        public virtual List<FieldSetting> GetAvailableFields()
        {
            return GetAvailableFields(true);
        }

        //UNDONE:! XMLDOC:
        public virtual List<FieldSetting> GetAvailableFields(bool rootFields)
        {
            var availableFields = new List<FieldSetting>();

            GetAvailableContentTypeFields(availableFields, rootFields);

            return availableFields;
        }

        //UNDONE:! XMLDOC:
        protected void GetAvailableContentTypeFields(ICollection<FieldSetting> availableFields)
        {
            GetAvailableContentTypeFields(availableFields, true);
        }

        //UNDONE:! XMLDOC:
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

        //UNDONE:! XMLDOC:
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
        /// It means that Content's AllowedChildTypes cannot contain any element that is not allowed on the
        /// target <see cref="Node"/> otherwise an InvalidOperationException will be thrown.
        /// </summary>
        /// <param name="target">The target <see cref="Node"/> that will be the parent of this Content.</param>
        public override void MoveTo(Node target)
        {
            var targetGc = target as GenericContent;
            if (targetGc != null)
                foreach (var nt in this.GetChildTypesToAllow())
                    if (!targetGc.IsAllowedChildType(nt.Name))
                        throw new InvalidOperationException(String.Format("Cannot move {0} ({1}) to {2} ({3}) because '{4}' type is not allowed in the new position."
                            , this.Path, this.NodeType.Name, target.Path, target.NodeType.Name, nt.Name));

            // Invalidate pinned workspace reference because it may change
            // after the move operation (e.g. when restoring from the trash).
            _workspace = null;

            base.MoveTo(target);
        }

        /// <summary>
        /// Copies this Content under the given target <see cref="Node"/>.
        /// </summary>
        /// <param name="target">The parent <see cref="Node"/> of the new instance.</param>
        /// <param name="newName">String value of the new name or null if it is not changed.</param>
        /// <returns></returns>
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
                if (Node.EXCLUDED_COPY_PROPERTIES.Contains(propType.Name)) continue;

                string targetName = null;
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
                var binProp = propVal as BinaryData;
                if (binProp == null)
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
                    List<string> targetPropertyNames = null;
                    targetContentListBindings.TryGetValue(item.Key, out targetPropertyNames);
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
                GenericContent parentContent = null;
                using (new SystemAccount())
                    parentContent = Parent as GenericContent;

                if (parentContent == null)
                    return !this.TrashDisabled;
                else
                    return !(parentContent.TrashDisabled || this.TrashDisabled);
            }
        }

        /// <summary>
        /// Gets the total count of contents in the subtree whose root is this Content.
        /// </summary>
        public override int NodesInTree
        {
            get
            {
                //TODO: it would be better to use GetChildren here, but QuerySettings should be extended with CountOnly handling, before we can do that.
                return ContentQuery.Query(SafeQueries.InTreeCountOnly,
                    new QuerySettings { EnableAutofilters = FilterStatus.Disabled, EnableLifespanFilter = FilterStatus.Disabled },
                    this.Path).Count;
            }
        }

        /// <summary>
        /// Moves this Content to the Trash if possible, otherwise deletes it physically.
        /// </summary>
        public override void Delete()
        {
            Delete(false);
        }

        /// <summary>
        /// Moves this Content to the Trash if possible, otherwise deletes it physically.
        /// </summary>
        /// <param name="bypassTrash">Specifies whether the moving to Trash should be tried or not.</param>
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
        public GenericContent MostRelevantContext
        {
            get { return ContentList.GetContentListForNode(this) ?? this; }
        }

        /// <summary>
        /// Gets the nearest <see cref="SystemFolder"/> from the parent chain or null.
        /// </summary>
        public GenericContent MostRelevantSystemContext
        {
            get { return SystemFolder.GetSystemContext(this) ?? this; }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return this.Name;
        }

        /// <summary>
        /// Returns the <see cref="SenseNet.ContentRepository.Schema.ContentType"/> of this instance.
        /// </summary>
        public ContentType GetContentType()
        {
            return ContentType.GetByName(NodeType.Name); //UNDONE:? use ContentType property.
        }

        /// <summary>
        /// Gets or sets the icon name in the inherited classes.
        /// In this class returns the value if the Icon property of the instance's <see cref="Schema.ContentType"/>.
        /// The setter throws a SnNotSupportedException in this class.
        /// </summary>
        public virtual string Icon
        {
            get
            {
                return GetContentType().Icon;
            }
            set
            {
                throw new SnNotSupportedException("Please implement Icon setter in your derived class");
            }
        }

        /// <summary>
        /// Returns <see cref="NodeHead"/> of the application representer Content specified by the given action name.
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
        /// Gets or sets the "Browse" application's Content if it is assigned explicitly.
        /// Persisted as <see cref="RepositoryDataType.Reference"/>.
        /// </summary>
        [RepositoryProperty("BrowseApplication", RepositoryDataType.Reference)]
        public Node BrowseApplication
        {
            get
            {
                return base.GetReference<Node>("BrowseApplication");
            }
            set
            {
                this.SetReference("BrowseApplication", value);
            }
        }

        // ==================================================== Versioning & Approving ====================================================

        private void VersionSetup()
        {
            if (this.Id == 0)
                this.Version = SavingAction.ComputeNewVersion(this.HasApproving, this.VersioningMode);
        }

        /// <summary>
        /// Persist this Content's changes.
        /// </summary>
        public override void Save()
        {
            if (!IsNew && IsVersionChanged())
            {
                SaveExplicitVersion();
            }
            else if (Locked)
            {
                if (this.IsLatestVersion)
                    Save(SavingMode.KeepVersion);
                else
                    Save(SavingMode.KeepVersionAndLock);
            }
            else
            {
                Save(SavingMode.RaiseVersion);
            }
        }
        private bool _savingExplicitVersion;
        /// <summary>
        /// Persist this Content's changes by the given mode.
        /// </summary>
        /// <param name="mode"><see cref="SavingMode"/> that controls the versioning.</param>
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
        /// <param name="settings"><see cref="NodeSaveSettings"/> that contains algorithm of the persistence.</param>
        public override void Save(NodeSaveSettings settings)
        {
            base.Save(settings);

            // if related workflows should be kept alive, update them on a separate thread
            if (_keepWorkflowsAlive)
                System.Threading.Tasks.Task.Run(() => UpdateRelatedWorkflows());
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
            // Certain workflows (e.g. approving) are designed to be aborted when a Content changes, but in case
            // certain system operations (e.g. updating a page count on a document) this should not happen.
            // So after saving the Content we update the related workflows to contain the same timestamp
            // as the Content has. This will prevent the workflows from aborting next time they wake up.
            using (new SystemAccount())
            {
                var newTimeStamp = this.NodeTimestamp;

                // query all workflows in the system that have this content as their related content
                var nodes = RepositoryInstance.ContentQueryIsAllowed
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
        /// After this operation the version of Content always raised even if the versioning mode is "off".
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
                    DocumentPreviewProvider.Current.StartCopyingPreviewImages(Node.LoadNode(this.Id, prevVersion), this);

                op.Successful = true;
            }
        }
        /// <summary>
        /// Commits the modifications of the checked out Content.
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
        /// Reverts the Content to the state before you checked it out and reloads it.
        /// </summary>
        public virtual void UndoCheckOut()
        {
            UndoCheckOut(true);
        }

        /// <summary>
        /// Reverts the Content to the state before you checked it out.
        /// If the "'forceRefresh" parameter is true, the Content will be reloaded.
        /// </summary>
        /// <param name="forceRefresh">Optional boolean value that specifies
        /// whether the Content will be reloaded or not. Default: true.</param>
        public void UndoCheckOut(bool forceRefresh = true) //UNDONE:? remove " = true"
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
        /// will be the next public with Approved state or remains the same but the versioning state changed to Pending.
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
        /// remains the same but the versioning state of the Content is changed to Rejected.
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

            if (update)
                Save(SavingMode.KeepVersion);
            else
                Save(SavingMode.RaiseVersion);
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
        /// workflow and current user's permissions enable the Approve function.
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
        /// workflow and current user's permissions enable the Publish function.
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
        public IEnumerable<Node> Versions
        {
            get { return LoadVersions(); }
        }

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
            if (RepositoryInstance.ContentQueryIsAllowed)
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
        /// Protected member that allow accessing for the raw value of the ChildrenDefinition property.
        /// </summary>
        protected ChildrenDefinition _childrenDefinition;
        /// <summary>
        /// Gets or sets the children representation of this Content.
        /// </summary>
        public virtual ChildrenDefinition ChildrenDefinition
        {
            get
            {
                if (_childrenDefinition == null)
                    _childrenDefinition = ChildrenDefinition.Default;
                return _childrenDefinition;
            }
            set { _childrenDefinition = value; }
        }

        internal ISnQueryable<Content> GetQueryableChildren()
        {
            return new ContentSet<Content>(this.ChildrenDefinition.Clone(), this.Path);
        }

        // ==================================================== IIndexable Members

        /// <summary>
        /// Returns collection of the <see cref="Field"/> that will be indexed for this Content.
        /// </summary>
        /// <remarks>The fields are accessed through the wrapper <see cref="SenseNet.ContentRepository.Content"/> of this instance.</remarks>
        public virtual IEnumerable<IIndexableField> GetIndexableFields()
        {
            var content = Content.Create(this); //UNDONE:? use this.Content
            var fields = content.Fields.Values;
            var indexableFields = fields.Where(f => f.IsInIndex).Cast<IIndexableField>().ToArray();
            var names = fields.Select(f => f.Name).ToArray();  //UNDONE:? remove unused line
            var indexableNames = indexableFields.Select(f => f.Name).ToArray();  //UNDONE:? remove unused line
            return indexableFields;
        }
    }
}
