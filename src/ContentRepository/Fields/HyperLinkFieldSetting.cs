using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage.Search;

namespace SenseNet.ContentRepository.Fields
{
    public enum UrlFormat
    {
        Hyperlink, Picture
    }

    public class HyperLinkFieldSetting : FieldSetting
    {
        public const string UrlFormatName = "UrlFormat";
        private UrlFormat? _format;

        public UrlFormat? UrlFormat
        {
            get
            {
                if (_format.HasValue)
                    return _format;

                return this.ParentFieldSetting == null ? null :
                    ((HyperLinkFieldSetting)this.ParentFieldSetting).UrlFormat;
            }
            set
            {
                if (!_mutable)
                    throw new InvalidOperationException("Setting UrlFormat is not allowed within readonly instance.");
                _format = value;
            }
        }

        protected override void ParseConfiguration(XPathNavigator configurationElement, IXmlNamespaceResolver xmlNamespaceResolver, ContentType contentType)
        {
            base.ParseConfiguration(configurationElement, xmlNamespaceResolver, contentType);

            foreach (XPathNavigator node in configurationElement.SelectChildren(XPathNodeType.Element))
            {
                switch (node.LocalName)
                {
                    case UrlFormatName:
                        ParseEnumValue(node.InnerXml, ref _format);
                        break;
                }
            }
        }
        protected override void ParseConfiguration(Dictionary<string, object> info)
        {
            base.ParseConfiguration(info);
            _format = GetConfigurationNullableValue<UrlFormat>(info, UrlFormatName, null);
        }
        protected override Dictionary<string, object> WriteConfiguration()
        {
            var result = base.WriteConfiguration();
            result.Add(UrlFormatName, _format);
            return result;
        }

        protected override void WriteConfiguration(XmlWriter writer)
        {
            if (this._format.HasValue)
                WriteElement(writer, this._format.Value.ToString(), UrlFormatName);
        }

        protected override void CopyPropertiesFrom(FieldSetting source)
        {
            base.CopyPropertiesFrom(source);

            var etSource = (HyperLinkFieldSetting)source;

            UrlFormat = etSource.UrlFormat;
        }

        public override IDictionary<string, FieldMetadata> GetFieldMetadata()
        {
            var fmd = base.GetFieldMetadata();

            if (fmd.ContainsKey(DefaultValueName))
            {
                fmd[DefaultValueName] = new FieldMetadata
                                            {
                                                FieldName = DefaultValueName,
                                                CanRead = true,
                                                CanWrite = true,
                                                FieldSetting = new HyperLinkFieldSetting
                                                                   {
                                                                       Name = DefaultValueName,
                                                                       DisplayName = GetTitleString(DefaultValueName),
                                                                       Description = GetDescString(DefaultValueName),
                                                                       FieldClassName = typeof (HyperLinkField).FullName,
                                                                       ControlHint = "sn:HyperLink",
                                                                       DefaultValue = "<a href=\"http://\" target=\"_blank\"></a>"
                                                                   }

                                            };
            }

            fmd.Add(UrlFormatName, new FieldMetadata
            {
                FieldName = UrlFormatName,
                PropertyType = typeof(UrlFormat),
                FieldType = DynamicContentTools.GetSuggestedFieldType(typeof(UrlFormat)),
                DisplayName = GetTitleString(UrlFormatName),
                Description = GetDescString(UrlFormatName),
                CanRead = true,
                CanWrite = true
            });

            return fmd;
        }

        public override object GetProperty(string name, out bool found)
        {
            var val = base.GetProperty(name, out found);

            if (!found)
            {
                switch (name)
                {
                    case UrlFormatName:
                        found = true;
                        if (_format.HasValue)
                            val = _format.Value;
                        break;
                }
            }

            return found ? val : null;
        }

        public override bool SetProperty(string name, object value)
        {
            var found = base.SetProperty(name, value);

            if (!found)
            {
                switch (name)
                {
                    case UrlFormatName:
                        found = true;
                        if (value != null)
                            _format = (UrlFormat)value;
                        break;
                }
            }

            return found;
        }

        protected override IFieldIndexHandler CreateDefaultIndexFieldHandler()
        {
            return new SenseNet.Search.Indexing.HyperLinkIndexHandler();
        }
    }
}
