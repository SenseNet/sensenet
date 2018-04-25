using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SenseNet.ContentRepository.Fields;
using SenseNet.ContentRepository.Schema.Metadata;
using SenseNet.ContentRepository.Storage.Search;

namespace SenseNet.Portal.OData.Typescript
{
    internal abstract class TypescriptModuleWriter : TypescriptSchemaVisitor
    {
        protected const string STRING = "string";
        protected const string NUMBER = "number";
        protected static readonly string MediaResourceObject = $"{TypescriptGenerationContext.ComplexTypesModuleName}.MediaResourceObject";

        protected static string[] FieldSettingPropertyBlackList =
        {
            "Aspect", "ShortName", "FieldClassName", "DisplayNameStoredValue", "DescriptionStoredValue", "Bindings",
            "IsRerouted", "Owner", "ParentFieldSetting", "FullName", "BindingName", "IndexingInfo", "OutputMethod",
            "Visible", "LocalizationEnabled", "FieldDataType"
        };

        protected static Dictionary<string, string> SimplifiedProperties = new Dictionary<string, string>
        {
            {"Rating", STRING},
            {"NodeType", STRING},
            {"Version", STRING},
            {"AllowedChildTypes", STRING},
            {"TextExtractors", STRING},
            {"UrlList", STRING},
            {"Image", MediaResourceObject},
            {"Binary", MediaResourceObject},
        };

        protected readonly TextWriter _writer;
        protected TextWriter Writer => _writer;

        protected int _indentCount = 0;
        private string _indent = "    ";

        protected TypescriptModuleWriter(TypescriptGenerationContext context, TextWriter writer) : base(context)
        {
            _writer = writer;
        }

        protected string GetPropertyValue(object @object, PropertyInfo propertyInfo)
        {
            var value = propertyInfo.GetValue(@object);
            var name = propertyInfo.Name;

            if (value == null)
                return null;

            if (value is bool)
                return value.ToString().ToLowerInvariant();

            var stringValue = value as string;
            if (stringValue != null)
                return "'" + stringValue.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\r\n", "\\").Replace("\r", "\\").Replace("\n", "\\") + "'";

            var stringEnumerable = value as IEnumerable<string>;
            if (stringEnumerable != null)
                return "[" + string.Join(",", stringEnumerable.Select(s => "'" + s + "'").ToArray()) + "]";

            var choiceOptionEnumerable = value as IEnumerable<ChoiceOption>;
            if (choiceOptionEnumerable != null)
                return GetChoiceOptions(choiceOptionEnumerable);

            var type = value.GetType();
            var prefix = type.IsEnum ? $"FieldSettings.{type.Name}." : string.Empty;

            return string.Format(CultureInfo.InvariantCulture, "{0}{1}", prefix, value);
        }

        protected string GetChoiceOptions(IEnumerable<ChoiceOption> options)
        {
            var indent = "";
            for (int i = 0; i < _indentCount + 3; i++)
                indent += _indent;

            var sb = new StringBuilder("[");
            var line = 0;
            foreach (var option in options)
            {
                if (0 == line++)
                    sb.AppendLine();
                else
                    sb.AppendLine(",");
                sb.Append($"{indent}{{Value: '{option.Value}', Text: '{option.Text}', Enabled: {option.Enabled.ToString().ToLowerInvariant()}, Selected: {option.Selected.ToString().ToLowerInvariant()} }}");
            }
            sb.AppendLine();
            indent = indent.Substring(_indent.Length);
            sb.Append(indent).Append("]");

            return sb.ToString();
        }

        protected string GetPropertyTypeName(Property property)
        {
            var propertyType = property.Type;

            var simpleType = propertyType as SimpleType;
            if (simpleType != null)
                return GetPropertyTypeName(simpleType);

            var referenceType = propertyType as ReferenceType;
            if (referenceType != null)
                return GetPropertyTypeName(referenceType);

            var complexType = propertyType as ComplexType;
            if (complexType != null)
                return GetPropertyTypeName(complexType);

            var enumeration = propertyType as Enumeration;
            if (enumeration != null)
                return GetPropertyTypeName(enumeration);

            throw new NotSupportedException($"An instance of the {propertyType.GetType().FullName} is not supported here.");
        }
        protected string GetPropertyTypeName(SimpleType simpleType)
        {
            return GetPropertyTypeName(simpleType.UnderlyingType);
        }
        protected virtual string GetPropertyTypeName(Type type)
        {
            if (typeof(IEnumerable<string>).IsAssignableFrom(type))
                return "string[]";

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                type = type.GetGenericArguments()[0];
            var typeName = type.Name;

            if (type == typeof(List<ChoiceOption>))
                return TypescriptGenerationContext.ComplexTypesModuleName + ".ChoiceOption[]";

            switch (typeName)
            {
                default:
                    return $"string /* original: {typeName} */";
                case "String": return STRING;
                case "Guid": return STRING;
                case "Boolean": return "boolean";
                case "DateTime": return STRING;
                case "Currency": return NUMBER;
                case "Byte": return NUMBER;
                case "SByte": return NUMBER;
                case "Int16": return NUMBER;
                case "Int32": return NUMBER;
                case "Int64": return NUMBER;
                case "Double": return NUMBER;
                case "Single": return NUMBER;
                case "Decimal": return NUMBER;
            }
        }
        protected string GetPropertyTypeName(ReferenceType referenceType)
        {
            ReferenceFieldSetting settings = referenceType.FieldSetting as ReferenceFieldSetting;
            var allAllowedTypeNames = new Schema(TypescriptFormatter.DisabledContentTypeNames).Classes.Select(c => c.Name);
            settings.AllowedTypes = settings?.AllowedTypes?.Where(allowedType => allAllowedTypeNames.Contains(allowedType)).ToList();
            var allowMultiple = settings?.AllowMultiple != null && settings.AllowMultiple.Value;
            var allowedTypes = "GenericContent";
            if (settings?.AllowedTypes?.Count > 0)
            {
                allowedTypes = string.Join(" | ", settings?.AllowedTypes);
            }
            
            if (allowMultiple)
            {
                return $"ContentListReferenceField<{allowedTypes}>";
            }
            return $"ContentReferenceField<{allowedTypes}>";
        }
        protected string GetPropertyTypeName(ComplexType complexType)
        {
            string simplifiedTypeName;
            if (SimplifiedProperties.TryGetValue(complexType.Name, out simplifiedTypeName))
                return simplifiedTypeName;

            return $"{TypescriptGenerationContext.ComplexTypesModuleName}.{complexType.Name}";
        }
        protected string GetPropertyTypeName(Enumeration enumeration)
        {
            string enumName = enumeration.FieldSetting.Name;
            string enumKey;
            if (!Context.EmittedEnumerationNames.TryGetValue(enumName, out enumKey) || enumKey != enumeration.Key)
                enumName = Context.GetEnumerationFullName(enumeration);
            return $"{TypescriptGenerationContext.EnumTypesModuleName}.{enumName}";
        }

        protected void WriteStart(string text)
        {
            WriteIndent();
            _writer.Write(text);
        }
        protected void WriteLine(string line = null)
        {
            if (line != null)
                WriteIndent();
            _writer.WriteLine(line);
        }
        protected void WriteIndent()
        {
            for (int i = 0; i < _indentCount; i++)
                _writer.Write(_indent);
        }
    }
}
