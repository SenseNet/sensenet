using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using SenseNet.ContentRepository.Schema;

namespace SenseNet.ContentRepository.Fields
{
    public enum TextType
    {
        LongText, RichText, AdvancedRichText
    }

    public class LongTextFieldSetting : TextFieldSetting
    {
        public const string TextTypeName = "TextType";
        public const string RowsName = "Rows";
        public const string AppendModificationsName = "AppendModifications";

        private int? _rows;
        private TextType? _textType;
        private bool? _appendModifications;

        public int? Rows
        {
            get
            {
                return _rows ?? (this.ParentFieldSetting == null ? null : 
                    ((LongTextFieldSetting)this.ParentFieldSetting).Rows);
            }
            set
            {
                if (!_mutable)
                    throw new InvalidOperationException("Setting Rows is not allowed within readonly instance.");
                _rows = value;
            }
        }
        public TextType? TextType
        {
            get
            {
                if (_textType.HasValue)
                    return _textType;

                return this.ParentFieldSetting == null ? null :
                    ((LongTextFieldSetting)this.ParentFieldSetting).TextType;
            }
            set
            {
                if (!_mutable)
                    throw new InvalidOperationException("Setting TextType is not allowed within readonly instance.");
                _textType = value;
            }
        }
        public bool? AppendModifications
        {
            get
            {
                if (_appendModifications.HasValue)
                    return _appendModifications;

                return this.ParentFieldSetting == null ? null :
                    ((LongTextFieldSetting)this.ParentFieldSetting).AppendModifications;
            }
            set
            {
                if (!_mutable)
                    throw new InvalidOperationException("Setting AppendModifications is not allowed within readonly instance.");
                _appendModifications = value;
            }
        }

        protected override void ParseConfiguration(XPathNavigator configurationElement, IXmlNamespaceResolver xmlNamespaceResolver, ContentType contentType)
        {
            base.ParseConfiguration(configurationElement, xmlNamespaceResolver, contentType);

            foreach (XPathNavigator node in configurationElement.SelectChildren(XPathNodeType.Element))
            {
                switch (node.LocalName)
                {
                    case TextTypeName:
                        ParseEnumValue(node.InnerXml, ref _textType);
                        break;
                    case RowsName:
                        int rows;
                        if (int.TryParse(node.InnerXml, out rows))
                            _rows = rows;
                        break;
                    case AppendModificationsName:
                        if (!string.IsNullOrEmpty(node.InnerXml))
                            _appendModifications = node.InnerXml.ToLower().CompareTo("true") == 0;
                        break;
                }
            }
        }
        protected override void ParseConfiguration(Dictionary<string, object> info)
        {
            base.ParseConfiguration(info);
            _textType = GetConfigurationNullableValue<TextType>(info, TextTypeName, null);
            _rows = GetConfigurationNullableValue<int>(info, RowsName, null);
            _appendModifications = GetConfigurationNullableValue<bool>(info, AppendModificationsName, null);
        }
        protected override Dictionary<string, object> WriteConfiguration()
        {
            var result = base.WriteConfiguration();
            result.Add(TextTypeName, _textType);
            result.Add(RowsName, _rows);
            result.Add(AppendModificationsName, _appendModifications);
            return result;
        }

        protected override void WriteConfiguration(XmlWriter writer)
        {
            base.WriteConfiguration(writer);

            if (this._textType.HasValue)
                WriteElement(writer, this._textType.Value.ToString(), TextTypeName);
            
            WriteElement(writer, this._rows, RowsName);
            WriteElement(writer, this._appendModifications, AppendModificationsName);
        }

        protected override void CopyPropertiesFrom(FieldSetting source)
        {
            base.CopyPropertiesFrom(source);

            var fsSource = (LongTextFieldSetting)source;

            TextType = fsSource.TextType;
            Rows = fsSource.Rows;
            AppendModifications = fsSource.AppendModifications;
        }

        public override IDictionary<string, FieldMetadata> GetFieldMetadata()
        {
            var fmd = base.GetFieldMetadata();

            fmd.Add(TextTypeName, new FieldMetadata
            {
                FieldName = TextTypeName,
                CanRead = true,
                CanWrite = true,
                FieldSetting = new ChoiceFieldSetting
                                   {
                                       Name = TextTypeName,
                                       DisplayName = GetTitleString(TextTypeName),
                                       Description = GetDescString(TextTypeName),
                                       EnumTypeName = typeof(TextType).FullName,
                                       DisplayChoice = DisplayChoice.RadioButtons,
                                       AllowMultiple = false,
                                       AllowExtraValue = false,
                                       DefaultValue = ((int)Fields.TextType.LongText).ToString(),
                                       FieldClassName = typeof(ChoiceField).FullName,
                                   }
            });

            fmd.Add(RowsName, new FieldMetadata
            {
                FieldName = RowsName,
                PropertyType = typeof(int),
                FieldType = DynamicContentTools.GetSuggestedFieldType(typeof(int?)),
                DisplayName = GetTitleString(RowsName),
                Description = GetDescString(RowsName),
                CanRead = true,
                CanWrite = true
            });

            fmd.Add(AppendModificationsName, new FieldMetadata
            {
                FieldName = AppendModificationsName,
                CanRead = true,
                CanWrite = true,
                FieldSetting = new YesNoFieldSetting
                {
                    Name = AppendModificationsName,
                    DisplayName = GetTitleString(AppendModificationsName),
                    Description = GetDescString(AppendModificationsName),
                    DisplayChoice = DisplayChoice.RadioButtons,
                    DefaultValue = YesNoFieldSetting.NoValue,
                    FieldClassName = typeof(YesNoField).FullName,
                    VisibleBrowse = FieldVisibility.Hide,
                    VisibleEdit = FieldVisibility.Hide,
                    VisibleNew = FieldVisibility.Hide
                }
            });

            fmd[OutputMethodName].FieldSetting.VisibleNew = FieldVisibility.Advanced;
            fmd[OutputMethodName].FieldSetting.VisibleEdit = FieldVisibility.Advanced;

            return fmd;
        }

        public override object GetProperty(string name, out bool found)
        {
            var val = base.GetProperty(name, out found);

            if (!found)
            {
                switch (name)
                {
                    case TextTypeName:
                        found = true;
                        if (_textType.HasValue)
                            val = (int)_textType.Value;
                        break;
                    case AppendModificationsName:
                        found = true;
                        if (_appendModifications.HasValue)
                            val = _appendModifications.Value ? YesNoFieldSetting.YesValue : YesNoFieldSetting.NoValue;
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
                var sv = value as string;

                switch (name)
                {
                    case TextTypeName:
                        found = true;
                        if (value != null)
                            _textType = (TextType)Convert.ToInt32(value);
                        break;
                    case AppendModificationsName:
                        found = true;
                        if (!string.IsNullOrEmpty(sv))
                            _appendModifications = sv.CompareTo(YesNoFieldSetting.YesValue) == 0;
                        break;
                }
            }

            return found;
        }

        protected override SenseNet.Search.Indexing.FieldIndexHandler CreateDefaultIndexFieldHandler()
        {
            return new SenseNet.Search.Indexing.LongTextIndexHandler();
        }
    }
}
