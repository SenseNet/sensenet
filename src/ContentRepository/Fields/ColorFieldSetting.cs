using System;
using System.Collections.Generic;
using SenseNet.ContentRepository.Schema;
using System.Xml.XPath;
using System.Xml;
using SkiaSharp;

namespace SenseNet.ContentRepository.Fields
{
    public class ColorFieldSetting : TextFieldSetting
    {
        public const string PaletteName = "Palette";

        private string _palette;
        public string Palette
        {
            get
            {
                if (_palette != null)
                    return _palette;
                if (this.ParentFieldSetting == null)
                    return null;
                return ((ColorFieldSetting)this.ParentFieldSetting).Palette;
            }
            set
            {
                if (!_mutable)
                    throw new InvalidOperationException("Setting Palette is not allowed within readonly instance.");
                _palette = value;
            }
        }

        protected override void ParseConfiguration(XPathNavigator configurationElement, IXmlNamespaceResolver xmlNamespaceResolver,
            ContentType contentType, List<string> parsedElementNames)
        {
            base.ParseConfiguration(configurationElement, xmlNamespaceResolver, contentType, parsedElementNames);

            foreach (XPathNavigator element in configurationElement.SelectChildren(XPathNodeType.Element))
            {
                switch (element.LocalName)
                {
                    case PaletteName:
                        _palette = element.Value;
                        parsedElementNames.Add(PaletteName);
                        break;
                }
            }
        }
        protected override void ParseConfiguration(Dictionary<string, object> info)
        {
            base.ParseConfiguration(info);
            _palette = GetConfigurationStringValue(info, PaletteName, null);
        }
        protected override Dictionary<string, object> WriteConfiguration()
        {
            var result = base.WriteConfiguration();
            result.Add(PaletteName, _palette);
            return result;
        }
        protected override void SetDefaults()
        {
            _palette = null;
        }
        
        protected override void WriteConfiguration(XmlWriter writer)
        {
            base.WriteConfiguration(writer);

            WriteElement(writer, this._palette, PaletteName);
        }

        protected override void CopyPropertiesFrom(FieldSetting source)
        {
            base.CopyPropertiesFrom(source);

            var colorFieldSettingSource = (ColorFieldSetting)source;

            Palette = colorFieldSettingSource.Palette;

        }

        public override IDictionary<string, FieldMetadata> GetFieldMetadata()
        {
            var fmd = base.GetFieldMetadata();

            fmd.Add(PaletteName, new FieldMetadata
            {
                FieldName = PaletteName,
                CanRead = true,
                CanWrite = true,
                FieldSetting = new ShortTextFieldSetting()
                                    {
                                        Name = PaletteName,
                                        DisplayName = GetTitleString(PaletteName),
                                        Description = GetDescString(PaletteName),
                                        FieldClassName = typeof(ShortTextField).FullName
                                    }
            });

            return fmd;
        }

        public override FieldValidationResult ValidateData(object value, Field field)
        {
            if (value == null || value is string)
                return base.ValidateData(null, field);

            if (value is SKColor colorValue)
                return base.ValidateData(ColorField.ColorToString(colorValue), field);

            return FieldValidationResult.Successful;
        }
    }
}