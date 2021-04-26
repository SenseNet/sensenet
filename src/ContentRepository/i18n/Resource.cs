using System.Linq;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Events;

namespace SenseNet.ContentRepository.i18n
{
    /// <summary>
    /// Stores the necessary resources. 
    /// </summary>
    [ContentHandler]
    public class Resource : File
    {
        public Resource(Node parent) : this(parent, null) { }
        public Resource(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        protected Resource(NodeToken nt) : base(nt) { }

        public override object GetProperty(string name)
        {
            switch (name)
            {
                default:
                    return base.GetProperty(name);
            }
        }
        public override void SetProperty(string name, object value)
        {
            switch (name)
            {
                default:
                    base.SetProperty(name, value);
                    break;
            }
        }

        protected override void OnCreated(object sender, NodeEventArgs e)
        {
            base.OnCreated(sender, e);

            SenseNetResourceManager.Reset();
        }

        protected override void OnModified(object sender, NodeEventArgs e)
        {
            base.OnModified(sender, e);

            if (e.ChangedData.Any(cd => cd.Name == "Binary"))
                SenseNetResourceManager.Reset();
        }
    }
}