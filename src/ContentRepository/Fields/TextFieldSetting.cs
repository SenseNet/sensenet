using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository.Schema;
using System.Xml;
using System.Xml.XPath;

namespace SenseNet.ContentRepository.Fields
{
    public class TextFieldSetting : FieldSetting
    {
        public const string MaxLengthName = "MaxLength";
        public const string MinLengthName = "MinLength";

        private int? _minLength;
        private int? _maxLength;

        public int? MinLength
        {
            get
            {
                return _minLength ?? (this.ParentFieldSetting == null ? null :
                    ((TextFieldSetting)this.ParentFieldSetting).MinLength);
            }
            set
            {
                if (!_mutable)
                    throw new InvalidOperationException("Setting MinLength is not allowed within readonly instance.");
                _minLength = value;
            }
        }
        public int? MaxLength
        {
            get
            {
                if (_maxLength != null)
                    return _maxLength;
                if (this.ParentFieldSetting == null)
                    return null;
                return ((TextFieldSetting)this.ParentFieldSetting).MaxLength;
            }
            set
            {
                if (!_mutable)
                    throw new InvalidOperationException("Setting MaxLength is not allowed within readonly instance.");
                _maxLength = value;
            }
        }

        protected override void ParseConfiguration(XPathNavigator configurationElement, IXmlNamespaceResolver xmlNamespaceResolver, ContentType contentType)
        {
            // <MinLength>0..sok</MinLength>
            // <MaxLength>0..sok<MaxLength>
            foreach (XPathNavigator element in configurationElement.SelectChildren(XPathNodeType.Element))
            {
                switch (element.LocalName)
                {
                    case MaxLengthName:
                        int maxLength;
                        if (int.TryParse(element.InnerXml, out maxLength))
                            _maxLength = maxLength;
                        break;
                    case MinLengthName:
                        int minLength;
                        if (int.TryParse(element.InnerXml, out minLength))
                            _minLength = minLength;
                        break;
                }
            }
        }
        protected override void ParseConfiguration(Dictionary<string, object> info)
        {
            base.ParseConfiguration(info);
            _maxLength = GetConfigurationNullableValue<int>(info, MaxLengthName, null);
            _minLength = GetConfigurationNullableValue<int>(info, MinLengthName, null);
        }
        protected override Dictionary<string, object> WriteConfiguration()
        {
            var result = base.WriteConfiguration();
            result.Add(MaxLengthName, _maxLength);
            result.Add(MinLengthName, _minLength);
            return result;
        }
        protected override void SetDefaults()
        {
            _maxLength = null;
            _minLength = null;
        }

        public override FieldValidationResult ValidateData(object value, Field field)
        {
            var stringValue = (string)value;

            // compulsory
            if (String.IsNullOrEmpty(stringValue) && (this.Compulsory ?? false))
                return new FieldValidationResult(CompulsoryName);

            stringValue = stringValue ?? "";

            // minLength
            int minLength = this.MinLength.HasValue ? this.MinLength.Value : 0;
            if (stringValue.Length < minLength)
            {
                var result = new FieldValidationResult(MinLengthName);
                result.AddParameter(MinLengthName, minLength);
                return result;
            }
            // maxLength
            int maxLength = this.MaxLength.HasValue ? this.MaxLength.Value : Int32.MaxValue;
            if (stringValue.Length > maxLength)
            {
                var result = new FieldValidationResult(MaxLengthName);
                result.AddParameter(MaxLengthName, maxLength);
                return result;
            }

            return FieldValidationResult.Successful;
        }

        protected override void CopyPropertiesFrom(FieldSetting source)
        {
            base.CopyPropertiesFrom(source);

            var textFieldSettingSource = (TextFieldSetting)source;

            MaxLength = textFieldSettingSource.MaxLength;
            MinLength = textFieldSettingSource.MinLength;
        }
        protected override void WriteConfiguration(XmlWriter writer)
        {
            WriteElement(writer, this._minLength, MinLengthName);
            WriteElement(writer, this._maxLength, MaxLengthName);
        }

        public override IDictionary<string, FieldMetadata> GetFieldMetadata()
        {
            var fmd = base.GetFieldMetadata();
            var minFs = new IntegerFieldSetting
            {
                Name = MinLengthName,
                DisplayName = GetTitleString(MinLengthName),
                Description = GetDescString(MinLengthName),
                ShortName = "Integer",
                FieldClassName = typeof(IntegerField).FullName,
                MinValue = 0
            };
            var maxFs = new IntegerFieldSetting
            {
                Name = MaxLengthName,
                DisplayName = GetTitleString(MaxLengthName),
                Description = GetDescString(MaxLengthName),
                ShortName = "Integer",
                FieldClassName = typeof(IntegerField).FullName,
                MinValue = 0
            };

            fmd.Add(MinLengthName, new FieldMetadata
            {
                FieldName = MinLengthName,
                CanRead = true,
                CanWrite = true,
                FieldSetting = minFs
            });
            fmd.Add(MaxLengthName, new FieldMetadata
            {
                FieldName = MaxLengthName,
                CanRead = true,
                CanWrite = true,
                FieldSetting = maxFs
            });

            return fmd;
        }
    }
}
