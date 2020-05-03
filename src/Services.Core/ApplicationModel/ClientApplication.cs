using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Schema;

// ReSharper disable once CheckNamespace
namespace SenseNet.ApplicationModel
{
    [ContentHandler]
    public class ClientApplication : Application
    {
        public ClientApplication(Node parent) : this(parent, null) { }
        public ClientApplication(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        protected ClientApplication(NodeToken nt) : base(nt) { }
        
        [RepositoryProperty(nameof(Parameters), RepositoryDataType.Text)]
        public virtual string Parameters
        {
            get => GetProperty<string>(nameof(Parameters));
            set => this[nameof(Parameters)] = value;
        }

        public override string ActionTypeName
        {
            get => typeof(ClientAction).Name;
            set { /* do not store the value */ }
        }

        public override object GetProperty(string name)
        {
            switch (name)
            {
                case nameof(Parameters):
                    return Parameters;
                default:
                    return base.GetProperty(name);
            }
        }
        public override void SetProperty(string name, object value)
        {
            switch (name)
            {
                case nameof(Parameters):
                    Parameters = (string)value;
                    break;
                default:
                    base.SetProperty(name, value);
                    break;
            }
        }
    }
}
