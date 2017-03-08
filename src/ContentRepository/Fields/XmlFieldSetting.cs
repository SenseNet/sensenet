using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository.Schema;
using System.Xml.XPath;
using System.IO;
using System.Xml;

namespace SenseNet.ContentRepository.Fields
{
    public class XmlFieldSetting : FieldSetting
    {
        public const string ExpectedXmlNamespaceName = "ExpectedXmlNamespace";
        public const string NotWellformedXmlName = "NotWellformedXml";

        private string _expectedXmlNamespace;

        public string ExpectedXmlNamespace
        {
            get
            {
                if (_expectedXmlNamespace != null)
                    return _expectedXmlNamespace;

                var parentAsXmlFieldSetting = this.ParentFieldSetting as XmlFieldSetting;
                return parentAsXmlFieldSetting == null ? null : parentAsXmlFieldSetting.ExpectedXmlNamespace;
            }
            set
            {
                if (!_mutable)
                    throw new InvalidOperationException("Setting ExpectedXmlNamespace is not allowed within readonly instance.");
                _expectedXmlNamespace = value;
            }
        }

        protected override void ParseConfiguration(XPathNavigator configurationElement, IXmlNamespaceResolver xmlNamespaceResolver, ContentType contentType)
        {
            // <ExpectedXmlNamespace>htp://example.com/namespace</ExpectedXmlNamespace>
            foreach (XPathNavigator element in configurationElement.SelectChildren(XPathNodeType.Element))
            {
                if (element.LocalName != ExpectedXmlNamespaceName)
                    continue;
                _expectedXmlNamespace = element.InnerXml;
                return;
            }
        }
        protected override void ParseConfiguration(Dictionary<string, object> info)
        {
            base.ParseConfiguration(info);
            _expectedXmlNamespace = GetConfigurationStringValue(info, ExpectedXmlNamespaceName, null);
        }
        protected override Dictionary<string, object> WriteConfiguration()
        {
            var result = base.WriteConfiguration();
            result.Add(ExpectedXmlNamespaceName, _expectedXmlNamespace);
            return result;
        }
        protected override void SetDefaults()
        {
            _expectedXmlNamespace = null;
        }

        public override FieldValidationResult ValidateData(object value, Field field)
        {
            if(value == null)
                return FieldValidationResult.Successful;

            var stringValue = (string)value;

            try
            {
                var x = new XPathDocument(new StringReader(stringValue));
                var nav = x.CreateNavigator().SelectSingleNode("/*[1]");

                if (ExpectedXmlNamespace != null)
                    if (nav.NamespaceURI != ExpectedXmlNamespace)
                        return new FieldValidationResult(ExpectedXmlNamespaceName);
            }
            catch (Exception)
            {
                return new FieldValidationResult(NotWellformedXmlName);
            }

            return FieldValidationResult.Successful;
        }

        public override IDictionary<string, FieldMetadata> GetFieldMetadata()
        {
            var fmd = base.GetFieldMetadata();

            fmd.Add("ExpectedXmlNamespace", new FieldMetadata
            {
                FieldName = "ExpectedXmlNamespace",
                PropertyType = typeof(string),
                FieldType = DynamicContentTools.GetSuggestedFieldType(typeof(string)),
                DisplayName = GetTitleString("ExpectedXmlNamespace"),
                Description = GetDescString("ExpectedXmlNamespace"),
                CanRead = true,
                CanWrite = true
            });

            return fmd;
        }

        protected override void CopyPropertiesFrom(FieldSetting source)
        {
            base.CopyPropertiesFrom(source);

            var fsSource = (XmlFieldSetting)source;
            ExpectedXmlNamespace = fsSource.ExpectedXmlNamespace;
        }

        protected override void WriteConfiguration(XmlWriter writer)
        {
            WriteElement(writer, _expectedXmlNamespace, ExpectedXmlNamespaceName);
        }
    }
}
