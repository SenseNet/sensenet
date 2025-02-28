using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;

// ReSharper disable once CheckNamespace
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

    [RepositoryProperty(nameof(ClassName), RepositoryDataType.String)]
    public virtual string ClassName
    {
        get => base.GetProperty<string>(nameof(ClassName));
        set => base.SetProperty(nameof(ClassName), value);
    }

    [RepositoryProperty(nameof(MethodName), RepositoryDataType.String)]
    public virtual string MethodName
    {
        get => base.GetProperty<string>(nameof(MethodName));
        set => base.SetProperty(nameof(MethodName), value);
    }

    [RepositoryProperty(nameof(ActionTypeName), RepositoryDataType.String)]
    public override string ActionTypeName
    {
        get
        {
            var result = base.GetProperty<string>(nameof(ActionTypeName));
            if (string.IsNullOrEmpty(result))
                result = nameof(UIAction);
            return result;
        }
        set => this[ACTIONTYPENAME] = value;
    }



    public override object GetProperty(string name)
    {
        switch (name)
        {
            case nameof(UIDescriptor): return UIDescriptor;
            case nameof(ActionTypeName): return ActionTypeName;
            case nameof(ClassName): return ClassName;
            case nameof(MethodName): return MethodName;
            default:
                return base.GetProperty(name);
        }
    }

    public override void SetProperty(string name, object value)
    {
        switch (name)
        {
            case nameof(UIDescriptor): UIDescriptor = (string) value; break;
            case nameof(ActionTypeName): ActionTypeName = (string)value; break;
            case nameof(ClassName): ClassName = (string)value; break;
            case nameof(MethodName): MethodName = (string)value; break;
            default: base.SetProperty(name, value); break;
        }
    }
}