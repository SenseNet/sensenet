using System.Collections.Generic;
using SenseNet.ContentRepository.Storage;
using  SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Search;
using SenseNet.ContentRepository.Storage.Search;
using System;

namespace SenseNet.ContentRepository
{
    /// <summary>
    /// Defines a Content handler class that can support a children Content collection produced by a Content Query.
    /// </summary>
    [ContentHandler]
    public class SmartFolder : Folder
    {
        // ===================================================================================== Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="SmartFolder"/> class.
        /// </summary>
        /// <param name="parent">The parent.</param>
        public SmartFolder(Node parent) : this(parent, null) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="SmartFolder"/> class.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="nodeTypeName">Name of the node type.</param>
        public SmartFolder(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="SmartFolder"/> class in the loading procedure.
        /// Do not use this constructor directly from your code.
        /// </summary>
        protected SmartFolder(NodeToken nt) : base(nt) { }

        // ===================================================================================== Properties

        /// <summary>
        /// Gets or sets the CQL query that defines the children collection. Persisted as <see cref="RepositoryDataType.Text"/>.
        /// </summary>
        [RepositoryProperty("Query", RepositoryDataType.Text)]
        public string Query
        {
            get { return this.GetProperty<string>("Query"); }
            set { this["Query"] = value; }
        }

        /// <summary>
        /// Gets or sets a <see cref="FilterStatus"/> value that specifies whether the value of the Query will be extended with
        /// auto-filters or not. Persisted as <see cref="RepositoryDataType.String"/>.
        /// </summary>
        [RepositoryProperty("EnableAutofilters", RepositoryDataType.String)]
        public virtual FilterStatus EnableAutofilters
        {
            get
            {
                var enumVal = base.GetProperty<string>("EnableAutofilters");
                if (string.IsNullOrEmpty(enumVal))
                    return FilterStatus.Default;

                return (FilterStatus)Enum.Parse(typeof(FilterStatus), enumVal); 
            }
            set
            {
                this["EnableAutofilters"] = Enum.GetName(typeof(FilterStatus), value);
            }
        }

        /// <summary>
        /// Gets or sets a <see cref="FilterStatus"/> value that specifies whether the value of the Query will be extended with
        /// lifespan-filters or not. Persisted as <see cref="RepositoryDataType.String"/>.
        /// </summary>
        [RepositoryProperty("EnableLifespanFilter", RepositoryDataType.String)]
        public virtual FilterStatus EnableLifespanFilter
        {
            get
            {
                var enumVal = base.GetProperty<string>("EnableLifespanFilter");
                if (string.IsNullOrEmpty(enumVal))
                    return FilterStatus.Default;

                return (FilterStatus)Enum.Parse(typeof(FilterStatus), enumVal); 
            }
            set
            {
                this["EnableLifespanFilter"] = Enum.GetName(typeof(FilterStatus), value);
            }
        }

        /// <summary>
        /// Gets or sets the abstraction of the children Coontent collection.
        /// In default it is based on the values of Query, EnableAutofilters and EnableLifespanFilter properties.
        /// This property is not persisted so the effect of overriding the default behavior is temporary.
        /// </summary>
        public override ChildrenDefinition ChildrenDefinition
        {
            get
            {
                if (_childrenDefinition == null)
                {
                    _childrenDefinition = new ChildrenDefinition
                    {
                        ContentQuery = this.Query,
                        PathUsage = PathUsageMode.InFolderOr,
                        EnableAutofilters = this.EnableAutofilters,
                        EnableLifespanFilter = this.EnableLifespanFilter
                    };
                }
                return _childrenDefinition;
            }
            set
            {
                base.ChildrenDefinition = value;
            }
        }

        // =====================================================================================

        /// <inheritdoc />
        public override object GetProperty(string name)
        {
            switch (name)
            {
                case "Query":
                    return this.Query;
                case "EnableAutofilters":
                    return this.EnableAutofilters;
                case "EnableLifespanFilter":
                    return this.EnableLifespanFilter;
                default:
                    return base.GetProperty(name);
            }
        }
        /// <inheritdoc />
        public override void SetProperty(string name, object value)
        {
            switch (name)
            {
                case "Query":
                    this.Query = (string)value;
                    break;
                case "EnableAutofilters":
                    this.EnableAutofilters = (FilterStatus)value;
                    break;
                case "EnableLifespanFilter":
                    this.EnableLifespanFilter = (FilterStatus)value;
                    break;
                default:
                    base.SetProperty(name, value);
                    break;
            }
        }

        // ===================================================================================== Public interface

        /// <summary>
        /// Returns an one-use and not-saved <see cref="SmartFolder"/>.
        /// Designed for handling virtual containers in various business processes.
        /// </summary>
        /// <returns></returns>
        public static SmartFolder GetRuntimeQueryFolder()
        {
            // elevation is needed because the /Root/System folder may be inaccessible for some users
            using (new SystemAccount())
            {
                return new SmartFolder(Repository.SystemFolder) { Name = "RuntimeQuery" };
            }
        }
    }
}
