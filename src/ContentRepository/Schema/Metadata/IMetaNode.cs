using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SenseNet.ContentRepository.Fields;
using SenseNet.ContentRepository.Storage.Security;

namespace SenseNet.ContentRepository.Schema.Metadata
{
    public interface IMetaNode { }

    public class Schema : IMetaNode
    {
        public IEnumerable<Class> Classes { get; private set; }

        public Schema(string[] disabledContentTypeNames = null)
        {
            Classes = new MetaClassEnumerable(disabledContentTypeNames);
        }

        public Schema Rewrite(IEnumerable<Class> classes)
        {
            return new Schema {Classes = classes};
        }
    }
    public class Class : IMetaNode
    {
        public ContentType ContentType { get; }
        public string Name => ContentType.Name;
        public string BaseClassName => ContentType.ParentTypeName;
        public IEnumerable<Property> Properties { get; }

        internal bool Enabled { get; set; }

        public Class(ContentType contentType)
        {
            ContentType = contentType;
            Properties = new MetaPropertyEnumerable(contentType.FieldSettings, this);
        }
        private Class(ContentType contentType, IEnumerable<Property> properties)
        {
            ContentType = contentType;
            Properties = properties;
        }

        public Class Rewrite(IEnumerable<Property> properties)
        {
            return new Class(ContentType, properties);
        }

        public bool IsInstaceOfOrDerivedFrom(string contentTypeName)
        {
            return SenseNet.ContentRepository.Storage.ActiveSchema.NodeTypes[Name].IsInstaceOfOrDerivedFrom(contentTypeName);
        }
    }
    public class Property : IMetaNode
    {
        public FieldSetting FieldSetting { get; private set; }
        public string Name => FieldSetting.Name;
        public bool IsLocal { get; }
        public PropertyType Type { get; }

        public Property(FieldSetting fieldSetting, Class @class)
        {
            FieldSetting = fieldSetting;
            Type = PropertyType.Create(fieldSetting, @class);
            IsLocal = fieldSetting.Owner.Name == @class.Name && fieldSetting.ParentFieldSetting == null;
        }
        private Property(FieldSetting fieldSetting, Class @class, PropertyType propertyType) : this(fieldSetting, @class)
        {
            Type = propertyType;
        }

        public Property Rewrite(PropertyType propertyType)
        {
            return new Property(FieldSetting, Type.Class, propertyType);
        }
    }
    public abstract class PropertyType : IMetaNode
    {
        // fieldSetting.Configuration + indexing

        protected FieldSetting _fieldSetting;
        protected Class _class;

        public string Name => _fieldSetting.ShortName;
        public string PropertyName => _fieldSetting.Name;
        public bool Required { get; }
        public bool ReadOnly => _fieldSetting.ReadOnly;
        public Type UnderlyingType { get; protected set; }
        public Class Class => _class;
        public FieldSetting FieldSetting => _fieldSetting;

        public PropertyType(FieldSetting fieldSetting, Class @class)
        {
            _fieldSetting = fieldSetting;
            _class = @class;
            Required = fieldSetting.Compulsory ?? false;
            UnderlyingType = _fieldSetting.FieldDataType;
        }

        public static PropertyType Create(FieldSetting fieldSetting, Class @class)
        {
            var referenceFieldSetting = fieldSetting as ReferenceFieldSetting;
            if (referenceFieldSetting != null)
                return new ReferenceType(referenceFieldSetting, @class);

            var choiceFieldSetting = fieldSetting as ChoiceFieldSetting;
            if (choiceFieldSetting != null)
                return new Enumeration(choiceFieldSetting, @class);

            if (IsPrimitivePropertyType(fieldSetting.FieldDataType))
                return new SimpleType(fieldSetting, @class);

            return new ComplexType(fieldSetting, @class);
        }
        internal static bool IsPrimitivePropertyType(Type type)
        {
            if (type == typeof(string)) return true;
            if (type == typeof(byte)) return true;
            if (type == typeof(sbyte)) return true;
            if (type == typeof(short)) return true;
            if (type == typeof(int)) return true;
            if (type == typeof(long)) return true;
            if (type == typeof(double)) return true;
            if (type == typeof(float)) return true;
            if (type == typeof(bool)) return true;
            if (type == typeof(decimal)) return true;
            if (type == typeof(DateTime)) return true;
            if (type == typeof(Guid)) return true;
            return false;
        }
    }
    public class SimpleType : PropertyType
    {
        public SimpleType(FieldSetting fieldSetting, Class @class) : base(fieldSetting, @class) { }
        public SimpleType(FieldSetting fieldSetting, Class @class, Type propertyType) : base(fieldSetting, @class)
        {
            UnderlyingType = propertyType;
        }
    }
    public class ReferenceType : PropertyType
    {
        public ReferenceType(FieldSetting fieldSetting, Class @class) : base(fieldSetting, @class) { }
    }
    public class ComplexType : PropertyType
    {
        public IEnumerable<PropertyInfo> Properties;

        public ComplexType(FieldSetting fieldSetting, Class @class) : base(fieldSetting, @class)
        {
            var dataType = fieldSetting.FieldDataType;
            Properties = dataType == null ? new PropertyInfo[0] : dataType.GetProperties();
        }
    }
    public class Enumeration : PropertyType
    {
        private ChoiceFieldSetting _choiceFieldSetting;
        public EnumOption[] Options;
        public string Key { get; }
        public Enumeration(ChoiceFieldSetting choiceFieldSetting, Class @class) : base(choiceFieldSetting, @class)
        {
            _choiceFieldSetting = choiceFieldSetting;
            Options = new MetaEnumOptionEnumerable(_choiceFieldSetting, @class).ToArray();
            Key = $"{choiceFieldSetting.Name}:{string.Join(",", Options.Select(o => o.Name).ToArray())}";
        }
        private Enumeration(ChoiceFieldSetting choiceFieldSetting, Class @class, EnumOption[] options) : base(choiceFieldSetting, @class)
        {
            _choiceFieldSetting = choiceFieldSetting;
            Options = options;
        }

        internal Enumeration Rewrite(EnumOption[] options)
        {
            return new Enumeration(_choiceFieldSetting, _class, options);
        }
    }
    public class EnumOption : IMetaNode
    {
        private Class _class;
        private ChoiceFieldSetting _choiceFielSetting;
        public string Name;
        public string OriginalName;
        public string Value;

        public EnumOption(string name, string value, string originalName, ChoiceOption option, ChoiceFieldSetting choiceFielSetting, Class @class)
        {
            _class = @class;
            _choiceFielSetting = choiceFielSetting;
            Name = name;
            OriginalName = originalName;
            Value = value;
        }
    }
}
