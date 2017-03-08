using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using SenseNet.ContentRepository.Storage;

namespace SenseNet.ContentRepository.Schema
{
    internal class PropertyTypeRegistration
    {
        public string Name { get; private set; }
        public RepositoryDataType DataType { get; set; }
        public bool IsDeclared { get; private set; }
        public NodeTypeRegistration Parent { get; set; }

        public PropertyTypeRegistration(string propName, RepositoryDataType dataType)
        {
            // Use when a property is generic: ContentHandler does not contain this property but ContentTypeDefinition contains it.
            Name = propName;
            DataType = dataType;
            IsDeclared = true;
        }
        public PropertyTypeRegistration(PropertyInfo propInfo, RepositoryPropertyAttribute propTypeAttr)
        {
            // Use when a PropertyType attribute is on a code Property
            Name = propTypeAttr.PropertyName != null ? propTypeAttr.PropertyName : DefaultName(propInfo);
            DataType = (propTypeAttr.DataType != RepositoryDataType.NotDefined) ? propTypeAttr.DataType : DataTypeFromSystemType(propInfo.PropertyType);
            IsDeclared = true;
        }

        private static string DefaultName(PropertyInfo propInfo)
        {
            return String.Concat(propInfo.DeclaringType.FullName, ".", propInfo.Name);
        }
        private static RepositoryDataType DataTypeFromSystemType(Type type)
        {
            // short types
            if (type == typeof(Boolean))
                return RepositoryDataType.Int;
            if (type == typeof(Byte))
                return RepositoryDataType.Int;
            if (type == typeof(SByte))
                return RepositoryDataType.Int;

            // integer types
            if (type == typeof(Int16))
                return RepositoryDataType.Int;
            if (type == typeof(Int32))
                return RepositoryDataType.Int;
            if (type == typeof(Int64))
                return RepositoryDataType.Currency;
            if (type == typeof(UInt16))
                return RepositoryDataType.Int;
            if (type == typeof(UInt32))
                return RepositoryDataType.Int;
            if (type == typeof(UInt64))
                return RepositoryDataType.Currency;

            // datetime
            if (type == typeof(DateTime))
                return RepositoryDataType.DateTime;

            // float
            if (type == typeof(Single))
                return RepositoryDataType.Currency;
            if (type == typeof(Double))
                return RepositoryDataType.Currency;
            if (type == typeof(Decimal))
                return RepositoryDataType.Currency;

            // binary
            if (type == typeof(BinaryData))
                return RepositoryDataType.Binary;

            // others
            if (type == typeof(string))
                return RepositoryDataType.String;

            throw new ContentRegistrationException(String.Concat(SR.Exceptions.Registration.Msg_NotARepositoryDataType, ": ", type.FullName));
        }

        public override string ToString()
        {
            if (Parent == null)
                return String.Concat("PropertyTypeRegistration: '[unknown class]::", Name, "'");
            else
                return String.Concat("PropertyTypeRegistration: '", Parent.Name, "::", Name, "'");
        }
    }
}