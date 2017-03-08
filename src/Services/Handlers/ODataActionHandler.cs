using System.Web;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Schema;
using SenseNet.ApplicationModel;

namespace SenseNet.Portal.Handlers
{
    [ContentHandler]
    public class ODataActionHandler : Application, IHttpHandler
    {
        public ODataActionHandler(Node parent) : this(parent, null) { }
        public ODataActionHandler(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        protected ODataActionHandler(NodeToken nt) : base(nt) { }

        public bool IsReusable { get; } = false;

        public void ProcessRequest(HttpContext context)
        {
            var action = (IHttpHandler)this.CreateAction(this.Content, null, null);
            action.ProcessRequest(context);
        }

        public const string TYPENAME = "TypeName";
        [RepositoryProperty(TYPENAME, RepositoryDataType.String)]
        public virtual string TypeName
        {
            get { return base.GetProperty<string>(TYPENAME); }
            set { this[TYPENAME] = value; }
        }

        public override object GetProperty(string name)
        {
            switch (name)
            {
                case TYPENAME:
                    return this.TypeName;
                default:
                    return base.GetProperty(name);
            }
        }
        public override void SetProperty(string name, object value)
        {
            switch (name)
            {
                case TYPENAME:
                    this.TypeName = (string)value;
                    break;
                default:
                    base.SetProperty(name, value);
                    break;
            }
        }

    }
}
