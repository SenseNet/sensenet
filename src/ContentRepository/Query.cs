using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;

namespace SenseNet.ContentRepository
{
    public enum QueryType { NonDefined, Public, Private }

    [ContentHandler]
    public class QueryContent : GenericContent
    {
        // ======================================================================== Constructors

        public QueryContent(Node parent) : this(parent, null) { }
        public QueryContent(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        protected QueryContent(NodeToken token) : base(token) { }

        // ======================================================================== Properties

        private const string QUERYPROPERTY = "Query";
        [RepositoryProperty(QUERYPROPERTY, RepositoryDataType.Text)]
        public string QueryText
        {
            get { return base.GetProperty<string>(QUERYPROPERTY); }
            set { this[QUERYPROPERTY] = value; }
        }

        private const string QUERYTYPEPROPERTY = "QueryType";
        public string QueryType
        {
            get
            {
                return Enum.GetName(typeof(QueryType), this.Path.StartsWith(Repository.UserProfilePath) 
                    ? ContentRepository.QueryType.Private 
                    : ContentRepository.QueryType.Public);
            }
        }

        // ======================================================================== Overrides

        public override object GetProperty(string name)
        {
            switch (name)
            {
                case QUERYPROPERTY:
                    return this.QueryText;
                case QUERYTYPEPROPERTY:
                    return this.QueryType;
                default:
                    return base.GetProperty(name);
            }
        }
        public override void SetProperty(string name, object value)
        {
            switch (name)
            {
                case QUERYPROPERTY:
                    this.QueryText = (string)value;
                    break;
                case QUERYTYPEPROPERTY:
                    break;
                default:
                    base.SetProperty(name, value);
                    break;
            }
        }
    }
}
