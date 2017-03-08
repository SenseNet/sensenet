using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace SenseNet.ContentRepository.Schema
{
    public static class DynamicContentTools
    {
        public static IDictionary<string, FieldMetadata> GetFieldMetadata(Type type)
        {
            return GetFieldMetadata(type, null);
        }
        public static IDictionary<string, FieldMetadata> GetFieldMetadata(Type type, Func<FieldMetadata, bool> isHiddenCallback)
        {
            var data = new Dictionary<string, FieldMetadata>();
            foreach (var propertyInfo in type.GetProperties(BindingFlags.FlattenHierarchy|BindingFlags.Instance|BindingFlags.Public))
            {
                if (!propertyInfo.CanRead)
                    continue;
                var fieldType = FieldManager.GetSuggestedFieldType(propertyInfo.PropertyType);

                var fieldMeta = new FieldMetadata
                {
                    PropertyInfo = propertyInfo,
                    FieldName = propertyInfo.Name,
                    PropertyType = propertyInfo.PropertyType,
                    FieldType = fieldType,
                    CanRead = propertyInfo.CanRead,
                    CanWrite = propertyInfo.CanWrite
                };
                if (fieldType != null && (isHiddenCallback == null || isHiddenCallback(fieldMeta)))
                    data.Add(propertyInfo.Name, fieldMeta);
            }
            return data;
        }
        public static string GenerateCtd(ISupportsDynamicFields handler, ContentType baseContentType)
        {
            var ctdFormat = @"<ContentType name=""{0}"" parentType=""{1}"" handler=""{2}"" xmlns=""{4}"">
    <Fields>
{3}
    </Fields>
</ContentType>";
            var fieldFormat = @"        <Field name=""{0}"" handler=""{1}"">
            <DisplayName>{2}</DisplayName>
            <Description>{3}</Description>
            <Bind property=""{4}"" />
        </Field>
";
            var enumFieldFormat = @"        <Field name=""{0}"" handler=""{1}"">
            <DisplayName>{2}</DisplayName>
            <Description>{3}</Description>
            <Bind property=""{4}"" />
            <Configuration>
                <AllowMultiple>false</AllowMultiple>
                <AllowExtraValue>false</AllowExtraValue>
                <Options>
                    <Enum type='{5}' />
                </Options>
            </Configuration>
        </Field>
";

            var handlerType = handler.GetType();
            var nameAttr = baseContentType.Name;
            var parentAttr = baseContentType.Name;
            var handlerAttr = handlerType.FullName;

            var fields = new StringBuilder();
            var dynamicFieldMetadata = handler.GetDynamicFieldMetadata();
            foreach (var propertyName in dynamicFieldMetadata.Keys)
            {
                var dynamicField = dynamicFieldMetadata[propertyName];

                if (dynamicField.FieldSetting == null)
                {
                    var proptype = dynamicField.PropertyType;
                    var fieldType = dynamicField.FieldType;
                    if (typeof (Enum).IsAssignableFrom(proptype))
                    {
                        fields.AppendFormat(enumFieldFormat, dynamicField.FieldName, fieldType, dynamicField.DisplayName,
                            dynamicField.Description, propertyName, proptype.FullName);
                    }
                    else
                    {
                        fields.AppendFormat(fieldFormat, dynamicField.FieldName, fieldType,
                                            dynamicField.DisplayName, dynamicField.Description, propertyName);
                    }
                }
                else
                {
                    fields.Append(dynamicField.FieldSetting.ToXml());
                }
            }

            return String.Format(ctdFormat, nameAttr, parentAttr, handlerAttr, 
                fields, ContentType.ContentDefinitionXmlNamespace);
        }
        public static Type GetSuggestedFieldType(Type propertyType)
        {
            return FieldManager.GetSuggestedFieldType(propertyType);
        }
    }
}
