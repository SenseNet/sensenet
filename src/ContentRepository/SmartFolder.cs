using System.Collections.Generic;
using SenseNet.ContentRepository.Storage;
using  SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Search;
using SenseNet.ContentRepository.Storage.Search;
using System;

namespace SenseNet.ContentRepository
{
    [ContentHandler]
    public class SmartFolder : Folder
    {
        // ===================================================================================== Construction

        public SmartFolder(Node parent) : this(parent, null) { }
        public SmartFolder(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        protected SmartFolder(NodeToken nt) : base(nt) { }

        // ===================================================================================== Properties

        [RepositoryProperty("Query", RepositoryDataType.Text)]
        public string Query
        {
            get { return this.GetProperty<string>("Query"); }
            set { this["Query"] = value; }
        }

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
