using System;
using System.Collections.Generic;
using System.Text;
using SenseNet.ContentRepository.Schema;
using System.Xml.XPath;
using System.Xml;
using System.Web;

namespace SenseNet.ContentRepository.Fields
{
    public class ShortTextFieldSetting : TextFieldSetting
    {
        public const string RegexName = "Regex";

        private string _regex;
        public string Regex
        {
            get
            {
                if (_regex != null)
                    return _regex;
                if (this.ParentFieldSetting == null)
                    return null;
                return ((ShortTextFieldSetting)this.ParentFieldSetting).Regex;
            }
            set
            {
                if (!_mutable)
                    throw new InvalidOperationException("Setting Regex is not allowed within readonly instance.");
                _regex = value;
            }
        }

        protected override void ParseConfiguration(XPathNavigator configurationElement, IXmlNamespaceResolver xmlNamespaceResolver, ContentType contentType)
        {
            base.ParseConfiguration(configurationElement, xmlNamespaceResolver, contentType);

            // <Regex>^[a-zA-Z0-9]*$</Regex>
            foreach (XPathNavigator element in configurationElement.SelectChildren(XPathNodeType.Element))
            {
                switch (element.LocalName)
                {
                    case RegexName:
                        _regex = element.Value;
                        break;
                }
            }
        }
        protected override void ParseConfiguration(Dictionary<string, object> info)
        {
            base.ParseConfiguration(info);
            _regex = GetConfigurationStringValue(info, RegexName, null);
        }
        protected override Dictionary<string, object> WriteConfiguration()
        {
            var result = base.WriteConfiguration();
            result.Add(RegexName, _regex);
            return result;
        }
        protected override void SetDefaults()
        {
            _regex = null;
        }

        public override FieldValidationResult ValidateData(object value, Field field)
        {
            var result = base.ValidateData(value, field);
            if (result != FieldValidationResult.Successful)
                return result;

            string stringValue = (string)value ?? "";

            // regex
            if (!string.IsNullOrEmpty(this.Regex))
            {
                var r = new System.Text.RegularExpressions.Regex(this.Regex);
                var matches = r.Matches(stringValue);
                if (matches.Count == 0 || matches[0].Length != stringValue.Length)
                {
                    return new FieldValidationResult(RegexName);
                }
            }
            return FieldValidationResult.Successful;
        }

        protected override void WriteConfiguration(XmlWriter writer)
        {
            base.WriteConfiguration(writer);

            WriteElement(writer, this._regex, RegexName);
        }

        protected override void CopyPropertiesFrom(FieldSetting source)
        {
            base.CopyPropertiesFrom(source);

            var shortTextFieldSettingSource = (ShortTextFieldSetting)source;

            Regex = shortTextFieldSettingSource.Regex;

        }

        public override IDictionary<string, FieldMetadata> GetFieldMetadata()
        {
            var fmd = base.GetFieldMetadata();

            fmd.Add(RegexName, new FieldMetadata
            {
                FieldName = RegexName,
                CanRead = true,
                CanWrite = true,
                FieldSetting = new ShortTextFieldSetting()
                {
                    Name = RegexName,
                    DisplayName = GetTitleString(RegexName),
                    Description = GetDescString(RegexName),
                    FieldClassName = typeof(ShortTextField).FullName
                }
            });

            fmd[OutputMethodName].FieldSetting.VisibleNew = FieldVisibility.Advanced;
            fmd[OutputMethodName].FieldSetting.VisibleEdit = FieldVisibility.Advanced;

            return fmd;
        }
    }
}