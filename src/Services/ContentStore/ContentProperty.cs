using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Storage;
using System.Diagnostics;

[Serializable]
[DebuggerDisplay("{Name}: {Value}")]
public class ContentProperty
{
    public ContentProperty() { }

    public ContentProperty(DataType _type, string _name, object _value)
    {
        DataType[] flattypes = new DataType[] {DataType.Currency, DataType.DateTime, 
                    DataType.Int, DataType.String, DataType.Text };

        Name = _name;

        if (flattypes.Contains(_type))
            Value = _value;
        else
            switch (_type)
            {
                case DataType.Binary:
                    Value = ((BinaryData)_value).FileName.FullFileName;
                    break;
                case DataType.Reference:
                    Value = "";
                    break;
            }
    }

    private string _name;

    public string Name
    {
        get { return _name; }
        set { _name = value; }
    }

    private object _value;
    public object Value
    {
        get { return _value; }
        set { _value = value; }
    }
}