using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;

namespace SenseNet.ApplicationModel;

[ContentHandler]
public class Operation : ClientApplication
{
    public Operation(Node parent) : base(parent) { }
    public Operation(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
    protected Operation(NodeToken nt) : base(nt) { }

    // ReSharper disable once InconsistentNaming
    [RepositoryProperty(nameof(UIDescriptor), RepositoryDataType.Text)]
    public virtual string UIDescriptor
    {
        get => base.GetProperty<string>(nameof(UIDescriptor));
        set => base.SetProperty(nameof(UIDescriptor), value);
    }

    public override string ActionTypeName
    {
        get => nameof(UiAction);
        set { /* do not store the value */ }
    }

    public override object GetProperty(string name)
    {
        switch (name)
        {
            case nameof(UIDescriptor):
                return UIDescriptor;
            default:
                return base.GetProperty(name);
        }
    }

    public override void SetProperty(string name, object value)
    {
        switch (name)
        {
            case nameof(UIDescriptor): UIDescriptor = (string) value; break;
            default: base.SetProperty(name, value); break;
        }
    }
}