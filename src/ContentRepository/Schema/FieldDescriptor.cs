using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.XPath;
using System.Xml;
using SenseNet.Search;
using SenseNet.Search.Indexing;

namespace SenseNet.ContentRepository.Schema
{
    internal class FieldDescriptor
    {
        public ContentType Owner { get; private set; }
        internal string FieldName { get; private set; }
        internal string FieldTypeShortName { get; private set; }
        internal string FieldTypeName { get; private set; }
        internal string DisplayName { get; private set; }
        internal string Description { get; private set; }
        internal string Icon { get; private set; }
        internal List<string> Bindings { get; private set; }
        internal bool IsRerouted { get; private set; }
        internal string IndexingMode { get; private set; }
        internal string IndexStoringMode { get; private set; }
        internal string IndexingTermVector { get; private set; }
        internal IndexFieldAnalyzer Analyzer { get; private set; }
        internal string IndexHandlerTypeName { get; private set; }
        internal string FieldSettingTypeName { get; private set; }
        public XPathNavigator ConfigurationElement { get; private set; }
        public XPathNavigator AppInfo { get; private set; }
        public IXmlNamespaceResolver XmlNamespaceResolver { get; private set; }
        public RepositoryDataType[] DataTypes { get; private set; }
        public bool IsContentListField { get; private set; }

        private FieldDescriptor() { }

        internal static FieldDescriptor Parse(XPathNavigator fieldElement, IXmlNamespaceResolver nsres, ContentType contentType)
        {
            FieldDescriptor fdesc = new FieldDescriptor();
            fdesc.Owner = contentType;
            var fieldName = fieldElement.GetAttribute("name", String.Empty);
            fdesc.FieldName = fieldName;
            fdesc.FieldTypeShortName = fieldElement.GetAttribute("type", String.Empty);
            fdesc.FieldTypeName = fieldElement.GetAttribute("handler", String.Empty);
            fdesc.IsContentListField = fdesc.FieldName[0] == '#';
            if (String.IsNullOrEmpty(fdesc.FieldTypeShortName))
            {
                fdesc.FieldTypeShortName = FieldManager.GetShortName(fdesc.FieldTypeName);

                if (string.IsNullOrEmpty(fdesc.FieldTypeShortName))
                    throw new ContentRegistrationException($"Unknown field handler: {fdesc.FieldTypeName}", null,
                        contentType.Name, fieldName);
            }

            if (fdesc.FieldTypeName.Length == 0)
            {
                if (fdesc.FieldTypeShortName.Length == 0)
                    throw new ContentRegistrationException("Field element's 'handler' attribute is required if 'type' attribute is not given.", contentType.Name, fdesc.FieldName);

                try
                {
                    fdesc.FieldTypeName = FieldManager.GetFieldHandlerName(fdesc.FieldTypeShortName);
                }
                catch (NotSupportedException ex)
                {
                    throw new ContentRegistrationException($"Unknown field type: {fdesc.FieldTypeShortName}", ex,
                        contentType.Name, fieldName);
                }
            }

            fdesc.Bindings = new List<string>();

            foreach (XPathNavigator subElement in fieldElement.SelectChildren(XPathNodeType.Element))
            {
                switch (subElement.LocalName)
                {
                    case "DisplayName":
                        fdesc.DisplayName = subElement.Value;
                        break;
                    case "Description":
                        fdesc.Description = subElement.Value;
                        break;
                    case "Icon":
                        fdesc.Icon = subElement.Value;
                        break;
                    case "Bind":
                        var propertyName = subElement.GetAttribute("property", String.Empty);
                        fdesc.Bindings.Add(propertyName);
                        fdesc.IsRerouted |= propertyName != fdesc.FieldName;
                        break;
                    case "Indexing":
                        foreach (XPathNavigator indexingSubElement in subElement.SelectChildren(XPathNodeType.Element))
                        {
                            switch (indexingSubElement.LocalName)
                            {
                                case "Mode": fdesc.IndexingMode = indexingSubElement.Value; break;
                                case "Store": fdesc.IndexStoringMode = indexingSubElement.Value; break;
                                case "TermVector": fdesc.IndexingTermVector = indexingSubElement.Value; break;
                                case "Analyzer": fdesc.Analyzer = ParseAnalyzer(indexingSubElement.Value, contentType.Name, fieldName); break;
                                case "IndexHandler": fdesc.IndexHandlerTypeName = indexingSubElement.Value; break;
                            }
                        }
                        break;
                    case "Configuration":
                        fdesc.ConfigurationElement = subElement;
                        fdesc.FieldSettingTypeName = subElement.GetAttribute("handler", String.Empty);
                        break;
                    case "AppInfo":
                        fdesc.AppInfo = subElement;
                        break;
                    default:
                        throw new NotSupportedException(String.Concat("Unknown element in Field: ", subElement.LocalName));
                }
            }

            // Default binding;
            RepositoryDataType[] dataTypes = FieldManager.GetDataTypes(fdesc.FieldTypeShortName);
            fdesc.DataTypes = dataTypes;
            if (fdesc.IsContentListField)
            {
                foreach (var d in dataTypes)
                    fdesc.Bindings.Add(null);
            }
            else
            {
                if (dataTypes.Length > 1 && fdesc.Bindings.Count != dataTypes.Length)
                    throw new ContentRegistrationException("Missing explicit 'Binding' elements", contentType.Name, fdesc.FieldName);
                if (dataTypes.Length == 1 && fdesc.Bindings.Count == 0)
                    fdesc.Bindings.Add(fdesc.FieldName);
            }

            fdesc.XmlNamespaceResolver = nsres;

            return fdesc;
        }
        private static IndexFieldAnalyzer ParseAnalyzer(string analyzerName, string contentTypeName, string fieldName)
        {
            if(Enum.TryParse(analyzerName, true, out IndexFieldAnalyzer result))
                return result;

            var values = Enum.GetValues(typeof(IndexFieldAnalyzer)).Cast<IndexFieldAnalyzer>().Select(a => a.ToString()).ToArray();
            var validValues = string.Join("', '", values);
            throw new ContentRegistrationException(
                $"Invalid analyzer in {fieldName} field of content type {contentTypeName}: {analyzerName}. " +
                $"Valid values are: '{validValues}', default value: 'Default'");
        }
    }
}