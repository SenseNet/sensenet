using SenseNet.Packaging.Steps;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml;

namespace SenseNet.Packaging
{
    internal class PackageSchemaGenerator
    {
        #region xml source

        private const string Xsd = @"<?xml version=""1.0"" encoding=""utf-8""?>
<xs:schema id=""PackageSchema""
    elementFormDefault=""unqualified""
    xmlns:xs=""http://www.w3.org/2001/XMLSchema"">
  <xs:element name=""Package"">
    <xs:complexType>
      <xs:all>
        <xs:element name=""Description"" type=""xs:string"" minOccurs=""0"" maxOccurs=""1""/>
        <xs:element name=""ComponentId"" type=""xs:string"" minOccurs=""1"" maxOccurs=""1""/>
        <xs:element name=""ReleaseDate"" type=""xs:string"" minOccurs=""1"" maxOccurs=""1""/>
        <xs:element name=""Version"" type=""VersionNumber"" minOccurs=""1"" maxOccurs=""1""/>
        <xs:element name=""SuccessMessage"" type=""xs:string"" minOccurs=""0"" maxOccurs=""1""/>
        <xs:element name=""WarningMessage"" type=""xs:string"" minOccurs=""0"" maxOccurs=""1""/>
        <xs:element name=""ErrorMessage"" type=""xs:string"" minOccurs=""0"" maxOccurs=""1""/>
        <xs:element name=""Dependencies"" type=""Dependencies"" minOccurs=""0"" maxOccurs=""1""/>
        <xs:element name=""Steps"" type=""MainBlock"" minOccurs=""0"" maxOccurs=""1""/>
      </xs:all>
      <xs:attribute name=""type"" type=""PackageType"" use=""required"" />
    </xs:complexType>
  </xs:element>
  <xs:simpleType name=""PackageType"">
    <xs:restriction base=""xs:string"">
      <xs:enumeration value=""Tool"" />
      <xs:enumeration value=""Patch"" />
      <xs:enumeration value=""Install"" />
    </xs:restriction>
  </xs:simpleType>
  <xs:complexType name=""Dependencies"">
    <xs:sequence>
      <xs:element name=""Dependency"" type=""Dependency"" minOccurs=""0"" maxOccurs=""unbounded"" />
    </xs:sequence>
  </xs:complexType>
  <xs:complexType name=""Dependency"">
    <xs:attribute name=""id"" type=""xs:string"" use=""required""/>
    <xs:attribute name=""version"" type=""VersionNumber"" use=""optional""/>
    <xs:attribute name=""minVersion"" type=""VersionNumber"" use=""optional""/>
    <xs:attribute name=""maxVersion"" type=""VersionNumber"" use=""optional""/>
    <xs:attribute name=""minVersionExclusive"" type=""VersionNumber"" use=""optional""/>
    <xs:attribute name=""maxVersionExclusive"" type=""VersionNumber"" use=""optional""/>
  </xs:complexType>
  <xs:simpleType name=""VersionNumber"">
    <xs:restriction base=""xs:string"">
      <xs:pattern value=""{0}""/>
    </xs:restriction>
  </xs:simpleType>
  <xs:complexType name=""AnyXml"">
    <xs:sequence>
      <xs:any minOccurs=""0"" maxOccurs=""unbounded"" processContents=""skip"" />
    </xs:sequence>
  </xs:complexType>
  <xs:complexType name=""MainBlock"">
    <xs:complexContent mixed=""false"">
      <xs:extension base=""StepBlock"">
        <xs:sequence>
          <xs:element name=""Phase"" minOccurs=""0"" maxOccurs=""unbounded"" type=""StepBlock""/>
        </xs:sequence>
      </xs:extension>
    </xs:complexContent>
  </xs:complexType>
  <xs:complexType name=""StepBlock"">
    <xs:choice minOccurs=""0"" maxOccurs=""unbounded"">
{1}
    </xs:choice>
  </xs:complexType>
  <xs:complexType name=""Empty"">
  </xs:complexType>
  <!-- Enum types -->
{2}  <!-- Step types -->
{3}
</xs:schema>
";

        private const string StepHeader = @"      <xs:element name=""{0}"" type=""{1}"" />";

        private const string StepTemplate = @"  <xs:complexType name=""{0}"">
{1}    <xs:complexContent mixed=""true"">
      <xs:extension base=""Empty"">
        <xs:choice minOccurs=""0"" maxOccurs=""unbounded"">
{2}        </xs:choice>
{3}      </xs:extension>
    </xs:complexContent>
  </xs:complexType>";

        private const string StepAnnotation = @"    <xs:annotation>
      <xs:documentation>{0}</xs:documentation>
    </xs:annotation>
";

        private const string PropertyElementTemplate = @"          <xs:element name=""{0}"" type=""{1}"">
{2}          </xs:element>";

        private const string ElementAnnotation = @"            <xs:annotation>
              <xs:documentation>{0}</xs:documentation>
            </xs:annotation>
";

        private const string PropertyAttribuTetemplate = @"        <xs:attribute name=""{0}"" type=""{1}"">
{2}        </xs:attribute>";

        private const string AttributeAnnotation = @"          <xs:annotation>
            <xs:documentation>{0}</xs:documentation>
          </xs:annotation>
";

        private const string EnumTemplate = @"  <xs:simpleType name=""{0}"">
    <xs:restriction base=""xs:string"">
{1}    </xs:restriction>
  </xs:simpleType>
";

        private const string EnumOptionTemplate = @"      <xs:enumeration value=""{0}"" />
";

        #endregion

        [DebuggerDisplay("{Name} : {FullName}")]
        private class StepDescriptor
        {
            public string Name;
            public string FullName;
            public IEnumerable<StepPropertyDescriptor> Properties;
            public string Documentation;
        }
        [DebuggerDisplay("{Name} : {DataType} (isDefault: {IsDefault})")]
        private class StepPropertyDescriptor
        {
            public bool IsDefault;
            public string Name;
            public Type DataType;
            public bool CanBeAttribute;
            public bool CanBeElement;
            public string Documentation;
        }

        internal static string GenerateSchema()
        {
            var steps = GetStepDescriptors();
            var schema = GenerateSchema(steps);
            return schema;
        }
        private static IEnumerable<StepDescriptor> GetStepDescriptors()
        {
            var memory = new List<string>();
            var steps = new List<StepDescriptor>();
            foreach (var item in Step.StepTypes)
            {
                var stepType = item.Value;
                if (memory.Contains(stepType.FullName))
                    continue;
                memory.Add(stepType.FullName);

                //var step = (Step)Activator.CreateInstance(stepType);

                string classDoc = null;
                var docAttr = (AnnotationAttribute)stepType.GetCustomAttributes(true).FirstOrDefault(x => x is AnnotationAttribute);
                if (docAttr != null)
                    classDoc = docAttr.Documentation;

                var properties = new List<StepPropertyDescriptor>();
                foreach (var property in stepType.GetProperties())
                {
                    if (property.Name == "StepId" || property.Name == "ElementName")
                        continue;

                    var isStepBlock = property.PropertyType == typeof(IEnumerable<XmlElement>);

                    var attrs = property.GetCustomAttributes(true);
                    var isDefault = attrs.Any(x => x is DefaultPropertyAttribute);

                    string propDoc = null;
                    docAttr = (AnnotationAttribute)attrs.FirstOrDefault(x => x is AnnotationAttribute);
                    if (docAttr != null)
                        propDoc = docAttr.Documentation;

                    var isXmlFragment = attrs.Any(x => x is XmlFragmentAttribute);

                    properties.Add(new StepPropertyDescriptor
                    {
                        Name = property.Name,
                        DataType = isXmlFragment ? typeof(XmlFragmentAttribute) : property.PropertyType,
                        IsDefault = isDefault,
                        CanBeAttribute = !isStepBlock && !isXmlFragment,
                        CanBeElement = true,
                        Documentation = propDoc
                    });
                }
                steps.Add(new StepDescriptor { Name = item.Key, FullName = stepType.FullName, Properties = properties, Documentation = classDoc });
            }
            return steps;
        }

        private static string GenerateSchema(IEnumerable<StepDescriptor> steps)
        {
            var stepHeaders = string.Join(Environment.NewLine, steps.Select(s => String.Format(StepHeader, s.Name, !s.Properties.Any() ? "Empty" : s.Name)));
            var stepTypes = string.Join(Environment.NewLine, steps.Select(s => String.Format(StepTemplate
                , s.Name
                , String.IsNullOrEmpty(s.Documentation) ? "" : String.Format(StepAnnotation, s.Documentation)
                , GetStepElements(s)
                , GetStepAttributes(s)
                )));
            var enumTypes = string.Join(string.Empty, EnumTypes.Select(t => String.Format(EnumTemplate
                , t.Name
                , string.Join(string.Empty, Enum.GetNames(t).Select(o => String.Format(EnumOptionTemplate, o)))
                )));
            var schema = String.Format(PackageSchemaGenerator.Xsd, @"\d+(\.\d+){0,3}", stepHeaders, enumTypes, stepTypes);
            return schema;
        }
        private static string GetStepElements(StepDescriptor step)
        {
            var s = String.Join(Environment.NewLine, step.Properties
                .Where(p => p.CanBeElement)
                .Select(p => String.Format(PropertyElementTemplate
                    , p.Name
                    , GetDataType(p.DataType)
                    , String.IsNullOrEmpty(p.Documentation) ? "" : String.Format(ElementAnnotation, p.Documentation)
                )));
            return s;
        }
        private static string GetStepAttributes(StepDescriptor step)
        {
            var s = String.Join(Environment.NewLine, step.Properties
                .Where(p => p.CanBeAttribute)
                .Select(p => String.Format(PropertyAttribuTetemplate
                    , PackageManager.ToCamelCase(p.Name)
                    , GetDataType(p.DataType)
                    , String.IsNullOrEmpty(p.Documentation) ? "" : String.Format(AttributeAnnotation, p.Documentation)
                )));
            return s;
        }
        private static readonly List<Type> EnumTypes = new List<Type>();
        private static string GetDataType(Type type)
        {
            if (type == typeof(Int32))
                return "xs:integer";
            if (type == typeof(bool))
                return "xs:boolean";
            if (type == typeof(IEnumerable<XmlElement>))
                return "StepBlock";
            if (type == typeof(XmlFragmentAttribute))
                return "AnyXml";
            if (type.IsEnum)
            {
                if (EnumTypes.All(t => t != type))
                    EnumTypes.Add(type);
                return type.Name;
            }
            return "xs:string";
        }
    }
}
