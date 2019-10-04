using Compatibility.SenseNet.ApplicationModel;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Schema;

namespace Compatibility.SenseNet.Portal.ApplicationModel
{
    [ContentHandler]
    public class GenericODataApplication : Application
    {
        public GenericODataApplication(Node parent) : this(parent, null) { }
        public GenericODataApplication(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        protected GenericODataApplication(NodeToken nt) : base(nt) { }

        public const string CLASSNAME = "ClassName";
        [RepositoryProperty(CLASSNAME, RepositoryDataType.String)]
        public virtual string ClassName
        {
            get { return base.GetProperty<string>(CLASSNAME); }
            set { this[CLASSNAME] = value; }
        }

        public const string METHODNAME = "MethodName";
        [RepositoryProperty(METHODNAME, RepositoryDataType.String)]
        public virtual string MethodName
        {
            get { return base.GetProperty<string>(METHODNAME); }
            set { this[METHODNAME] = value; }
        }

        public const string PARAMETERS = "Parameters";
        [RepositoryProperty(PARAMETERS, RepositoryDataType.Text)]
        public virtual string Parameters
        {
            get { return base.GetProperty<string>(PARAMETERS); }
            set { this[PARAMETERS] = value; }
        }

        public override string ActionTypeName
        {
            get { return typeof(GenericODataOperation).Name; }
            set { /* do not store the value */ }
        }

        public override object GetProperty(string name)
        {
            switch (name)
            {
                case CLASSNAME:
                    return this.ClassName;
                case METHODNAME:
                    return this.MethodName;
                case PARAMETERS:
                    return this.Parameters;
                default:
                    return base.GetProperty(name);
            }
        }
        public override void SetProperty(string name, object value)
        {
            switch (name)
            {
                case CLASSNAME:
                    this.ClassName = (string)value;
                    break;
                case METHODNAME:
                    this.MethodName = (string)value;
                    break;
                case PARAMETERS:
                    this.Parameters = (string)value;
                    break;
                default:
                    base.SetProperty(name, value);
                    break;
            }
        }
    }
}
