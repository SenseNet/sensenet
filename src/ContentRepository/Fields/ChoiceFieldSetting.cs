using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Xml;
using System.Xml.XPath;
using SenseNet.ContentRepository.i18n;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using System.IO;
using SenseNet.ContentRepository.Storage.Search;
using SenseNet.Search;
using SenseNet.Tools;

namespace SenseNet.ContentRepository.Fields
{
    public enum DisplayChoice
    {
        DropDown, RadioButtons, CheckBoxes
    }

	public class ChoiceFieldSetting : ShortTextFieldSetting
	{
		public const string AllowMultipleName = "AllowMultiple";
		public const string AllowExtraValueName = "AllowExtraValue";
		public const string OptionsName = "Options";
        public const string DisplayChoicesName = "DisplayChoices";
        public const string InvalidExtraValue = "InvalidExtraValue";

        public static readonly string CtdResourceClassName = "Ctd";

		protected bool? _allowExtraValue;
		protected bool? _allowMultiple;
		protected List<ChoiceOption> _options;

        private DisplayChoice? _displayChoice;

		public bool? AllowExtraValue
		{
			get
			{
				if (_allowExtraValue.HasValue)
					return _allowExtraValue;

				return this.ParentFieldSetting == null ? null : 
                    ((ChoiceFieldSetting)this.ParentFieldSetting).AllowExtraValue;
			}
            set
            {
                if (!_mutable)
                    throw new InvalidOperationException("Setting AllowExtraValue is not allowed within readonly instance.");
                _allowExtraValue = value;
            }
		}
		public bool? AllowMultiple
		{
			get
			{
				if (_allowMultiple.HasValue)
					return _allowMultiple;

				return this.ParentFieldSetting == null ? null : 
                    ((ChoiceFieldSetting)this.ParentFieldSetting).AllowMultiple;
			}
            set
            {
                if (!_mutable)
                    throw new InvalidOperationException("Setting AllowMultiple is not allowed within readonly instance.");
                _allowMultiple = value;
            }
		}
		public List<ChoiceOption> Options
		{
			get
			{
				if (_options != null)
					return _options;
				if (this.ParentFieldSetting == null)
					return null;
				return ((ChoiceFieldSetting)this.ParentFieldSetting).Options;
			}
            set
            {
                if (!_mutable)
                    throw new InvalidOperationException("Setting Options is not allowed within readonly instance.");
                _options = value;
            }
		}
        public DisplayChoice? DisplayChoice
        {
            get
            {
                if (_displayChoice.HasValue)
                    return _displayChoice;
                if (this.ParentFieldSetting == null)
                    return !_allowMultiple.HasValue || !_allowMultiple.Value ? 
                        Fields.DisplayChoice.DropDown :
                        Fields.DisplayChoice.CheckBoxes;

                var dcValue = ((ChoiceFieldSetting)this.ParentFieldSetting).DisplayChoice;

                if (_allowMultiple.HasValue && !_allowMultiple.Value && dcValue == Fields.DisplayChoice.CheckBoxes)
                    dcValue = Fields.DisplayChoice.DropDown;

                return dcValue;
            }
            set
            {
                if (!_mutable)
                    throw new InvalidOperationException("Setting DisplayChoice is not allowed within readonly instance.");
                _displayChoice = value;
            }
        }
        public string EnumTypeName { get; set; }

		public ChoiceFieldSetting()
		{
			_options = new List<ChoiceOption>();
		}

		protected override void ParseConfiguration(XPathNavigator configurationElement, IXmlNamespaceResolver xmlNamespaceResolver, ContentType contentType)
		{
            base.ParseConfiguration(configurationElement, xmlNamespaceResolver, contentType);

			// <AllowExtraValue>true</AllowExtraValue>
			// <AllowMultiple>true</AllowMultiple>
			// <Options>
			//    <Option value="1" enabled="true|false" selected="">text1</Options>
			//    <Option value="2" enabled="true|false" selected="">text2</Options>
			// </Options>
			foreach (XPathNavigator node in configurationElement.SelectChildren(XPathNodeType.Element))
			{
				switch (node.LocalName)
				{
					case AllowMultipleName:
						bool allowMultiple;
						if (Boolean.TryParse(node.InnerXml, out allowMultiple))
							_allowMultiple = allowMultiple;
						break;
					case AllowExtraValueName:
						bool allowExtraValue;
						if (Boolean.TryParse(node.InnerXml, out allowExtraValue))
							_allowExtraValue = allowExtraValue;
						break;
					case OptionsName:
				        ParseOptionsPrivate(node);
						break;
                    case DisplayChoicesName:
				        var dcValue = node.InnerXml;
                        if (string.IsNullOrEmpty(dcValue))
                            break;
                        _displayChoice = (DisplayChoice)Enum.Parse(typeof(DisplayChoice), dcValue);
				        break;
				}
			}
		}
        protected override void ParseConfiguration(Dictionary<string, object> info)
        {
            base.ParseConfiguration(info);
            _allowMultiple = GetConfigurationNullableValue<bool>(info, AllowMultipleName, null);
            _allowExtraValue = GetConfigurationNullableValue<bool>(info, AllowExtraValueName, null);
            _displayChoice = GetConfigurationNullableValue<DisplayChoice>(info, DisplayChoicesName, null);
        }
        protected override Dictionary<string, object> WriteConfiguration()
        {
            var result = base.WriteConfiguration();
            result.Add(AllowMultipleName, _allowMultiple);
            result.Add(AllowExtraValueName, _allowExtraValue);
            result.Add(DisplayChoicesName, _displayChoice);
            return result;
        }
        protected override void SetDefaults()
		{
            base.SetDefaults();
			_allowMultiple = false;
			_allowExtraValue = false;
			_options = null;
		}
		public override FieldValidationResult ValidateData(object value, Field field)
		{
			List<string> selectedValues = ChoiceField.ConvertToStringList(value);

			bool allowMultiple = this.AllowMultiple ?? false;
			bool allowExtraValue = this.AllowExtraValue ?? false;

            if ((this.Compulsory ?? false) && selectedValues.Count == 0)
			{
				return new FieldValidationResult(CompulsoryName);
			}

			if (!allowMultiple && selectedValues.Count > 1)
			{
				return new FieldValidationResult(AllowMultipleName);
			}

            string extraValue = null;
			List<string> optionValues = GetOptionValues(Options);
			foreach (string selectedValue in selectedValues)
			{
				if (!optionValues.Contains(selectedValue))
				{
                    extraValue = selectedValue;
                    break;
				}
			}

            if(extraValue == null)
			    return FieldValidationResult.Successful;

            if(!extraValue.StartsWith(ChoiceField.EXTRAVALUEPREFIX))
                return new FieldValidationResult(InvalidExtraValue);

            return base.ValidateData(extraValue, field);
		}
		public static List<string> GetOptionValues(List<ChoiceOption> optionList)
		{
			List<string> result = new List<string>();
			foreach (ChoiceOption option in optionList)
				result.Add(option.Value);
			return result;
		}

        protected override void WriteConfiguration(XmlWriter writer)
        {
            base.WriteConfiguration(writer);

            WriteElement(writer, this._allowMultiple, AllowMultipleName);
            WriteElement(writer, this._allowExtraValue, AllowExtraValueName);

            if (!string.IsNullOrEmpty(this.EnumTypeName))
                WriteOptions(this.EnumTypeName, writer);
            else
                WriteOptions(_options, writer);

            if (_displayChoice.HasValue)
                WriteElement(writer, _displayChoice.Value.ToString(), DisplayChoicesName);
        }

        public static void WriteOptions(string enumTypeName, XmlWriter writer)
        {
            if (string.IsNullOrEmpty(enumTypeName))
                return;

            writer.WriteStartElement(OptionsName);

            writer.WriteStartElement("Enum");
            writer.WriteAttributeString("type", enumTypeName);
            writer.WriteEndElement();

            writer.WriteEndElement();
        }

        public static void WriteOptions(List<ChoiceOption> options, XmlWriter writer)
        {
            if (options == null || options.Count == 0) 
                return;

            writer.WriteStartElement(OptionsName);

            foreach (var option in options)
            {
                option.WriteXml(writer);
            }

            writer.WriteEndElement();
        }

        protected override void CopyPropertiesFrom(FieldSetting source)
        {
            base.CopyPropertiesFrom(source);
            
            var choiceFieldSettingSource = (ChoiceFieldSetting) source;

            AllowExtraValue = choiceFieldSettingSource.AllowExtraValue;
            AllowMultiple = choiceFieldSettingSource.AllowMultiple;
            DisplayChoice = choiceFieldSettingSource.DisplayChoice;
            Options = choiceFieldSettingSource.Options != null && choiceFieldSettingSource.Options.Count > 0 
                ? new List<ChoiceOption>(choiceFieldSettingSource.Options)
                : new List<ChoiceOption>();
        }

        public override IDictionary<string, FieldMetadata> GetFieldMetadata()
        {
            var fmd = base.GetFieldMetadata();

            FieldSetting fieldSetting;

            fieldSetting = fmd[MinLengthName].FieldSetting;
            fieldSetting.VisibleBrowse = FieldVisibility.Hide;
            fieldSetting.VisibleEdit = FieldVisibility.Hide;
            fieldSetting.VisibleNew = FieldVisibility.Hide;

            fieldSetting = fmd[MaxLengthName].FieldSetting;
            fieldSetting.VisibleBrowse = FieldVisibility.Hide;
            fieldSetting.VisibleEdit = FieldVisibility.Hide;
            fieldSetting.VisibleNew = FieldVisibility.Hide;

            fieldSetting = fmd[RegexName].FieldSetting;
            fieldSetting.VisibleBrowse = FieldVisibility.Hide;
            fieldSetting.VisibleEdit = FieldVisibility.Hide;
            fieldSetting.VisibleNew = FieldVisibility.Hide;

            fmd.Add(AllowExtraValueName, new FieldMetadata
            {
                FieldName = AllowExtraValueName,
                CanRead = true,
                CanWrite = true,
                FieldSetting = new NullFieldSetting
                {
                    Name = AllowExtraValueName,
                    DisplayName = GetTitleString(AllowExtraValueName),
                    Description = GetDescString(AllowExtraValueName),
                    FieldClassName = typeof(BooleanField).FullName
                }
            });

            fmd.Add(AllowMultipleName, new FieldMetadata
            {
                FieldName = AllowMultipleName,
                CanRead = true,
                CanWrite = true,
                FieldSetting = new NullFieldSetting
                            {
                                        Name = AllowMultipleName,
                                        DisplayName = GetTitleString("Choice_" + AllowMultipleName),
                                        Description = GetDescString("Choice_" + AllowMultipleName),
                                        FieldClassName = typeof(BooleanField).FullName
                            }
            });

            fmd.Add(OptionsName, new FieldMetadata
            {
                FieldName = OptionsName,
                CanRead = true,
                CanWrite = true,
                FieldSetting = new LongTextFieldSetting
                                   {
                                       Name = OptionsName,
                                       DisplayName = GetTitleString(OptionsName),
                                       Description = GetDescString(OptionsName),
                                       FieldClassName = typeof(LongTextField).FullName,
                                       ControlHint = "sn:ChoiceOptionEditor",
                                       Rows = 10
                                   }
            });

            fmd.Add(DisplayChoicesName, new FieldMetadata
            {
                FieldName = DisplayChoicesName,
                CanRead = true,
                CanWrite = true,
                FieldSetting = new ChoiceFieldSetting
                {
                    Name = DisplayChoicesName,
                    DisplayName = GetTitleString(DisplayChoicesName),
                    Description = GetDescString(DisplayChoicesName),
                    EnumTypeName = typeof(DisplayChoice).FullName,
                    DisplayChoice = Fields.DisplayChoice.DropDown,
                    AllowMultiple = false,
                    AllowExtraValue = false,
                    DefaultValue = ((int)Fields.DisplayChoice.DropDown).ToString(),
                    FieldClassName = typeof(ChoiceField).FullName,
                }
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
                    case AllowMultipleName:
                        val = AllowMultiple.HasValue ? (AllowMultiple.Value) : false;
                        found = true;
                        break;
                    case AllowExtraValueName:
                        val = AllowExtraValue.HasValue ? (AllowExtraValue.Value) : false;
                        found = true;
                        break;
                    case OptionsName:
                        if (_options != null && _options.Count > 0)
                        {
                            var sw = new StringWriter();
                            var ws = new XmlWriterSettings
                                         {
                                             OmitXmlDeclaration = true,
                                             ConformanceLevel = ConformanceLevel.Fragment
                                         };

                            using (var writer = XmlWriter.Create(sw, ws))
                            {
                                writer.WriteStartElement(OptionsName);

                                foreach (var option in this._options)
                                {
                                    option.WriteXml(writer);
                                }

                                writer.WriteEndElement();
                            }

                            val = sw.ToString();
                        }
                        found = true;
                        break;
                    case DisplayChoicesName:
                        found = true;
                        if (_displayChoice.HasValue)
                            val =(int)_displayChoice.Value;
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
                    case AllowMultipleName:
                        if (value != null)
                            AllowMultiple = (bool)value;
                        found = true;
                        break;
                    case AllowExtraValueName:
                        if (value != null)
                            AllowExtraValue = (bool)value;
                        found = true;
                        break;
                    case OptionsName:
                        var optString = value as string;
                        if (!string.IsNullOrEmpty(optString))
                        {
                            var opt = new XmlDocument();
                            opt.LoadXml(optString);

                            if (opt.DocumentElement != null)
                                ParseOptionsPrivate(opt.DocumentElement.CreateNavigator());
                        }

                        if (_options == null || _options.Count == 0)
                            throw new InvalidContentException(SR.GetString(SR.Exceptions.Fields.Error_Choice_OneOption));

                        found = true;
                        break;
                    case DisplayChoicesName:
                        found = true;
                        if (value != null)
                            _displayChoice = (DisplayChoice)Convert.ToInt32(value);
                        break;
                }
            }

            return found;
        }

        private void ParseOptionsPrivate(XPathNavigator node)
        {
            var enumTypeName = string.Empty;
            _options = ParseOptions(node, out enumTypeName);
            this.EnumTypeName = enumTypeName;
            if (_options.Any(x => x.Value.StartsWith(ChoiceField.EXTRAVALUEPREFIX, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Option cannot start with '" + ChoiceField.EXTRAVALUEPREFIX + "'");
        }
        protected virtual List<ChoiceOption> ParseOptions(XPathNavigator node, out string enumTypeName)
        {
            var options = new List<ChoiceOption>();
            enumTypeName = string.Empty;
            try
            {
                ParseOptions(node, options, this, out enumTypeName);
            }
            catch (ContentRegistrationException)
            {
                throw new ContentRegistrationException(String.Concat(
                            "Cannot parse an enum as an option list. Enum type name: '", enumTypeName,
                            "'. ChoiceField name: '", this.Name, "'. ContentType name: '", Owner.Name, "'."));
            }
            return options;
        }

	    public static void ParseOptions(XPathNavigator node, List<ChoiceOption> options, ChoiceFieldSetting fieldSetting, out string enumTypeName)
        {
            options.Clear();
	        enumTypeName = string.Empty;

            foreach (XPathNavigator optionElement in node.SelectChildren(XPathNodeType.Element))
            {
                if (optionElement.Name == "Enum")
                {
                    enumTypeName = optionElement.GetAttribute("type", "");
                    var enumType = TypeResolver.GetType(enumTypeName);
                    if (enumType == null)
                        throw new ContentRegistrationException("Enum");

                    var resClassName = optionElement.GetAttribute("resourceClass", "");
                    if (string.IsNullOrEmpty(resClassName))
                        resClassName = CtdResourceClassName;

                    var names = Enum.GetNames(enumType);
                    var values = Enum.GetValues(enumType).Cast<int>().ToArray();
                    for (var i = 0; i < names.Length; i++)
                    {
                        var resKey = fieldSetting == null ? string.Empty : fieldSetting.GetResourceKey(names[i]);
                        var text = string.IsNullOrEmpty(resKey) ? names[i] : SenseNetResourceManager.GetResourceKey(resClassName, resKey);
                        var c = new ChoiceOption(values[i].ToString(), text);
                        options.Add(c);
                    }
                }
                else
                {
                    // decode the xml string in order to handle &, < and > characters correctly
                    var text = HttpUtility.HtmlDecode(optionElement.InnerXml);
                    var key = optionElement.GetAttribute("value", "");

                    if (text.Length == 0 && key.Length == 0)
                        key = options.Count.ToString();
                    if (text.Length == 0)
                        text = key;
                    if (key.Length == 0)
                        key = text;

                    bool enabled;
                    if (!Boolean.TryParse(optionElement.GetAttribute("enabled", ""), out enabled))
                        enabled = true;

                    var selected = false;
                    Boolean.TryParse(optionElement.GetAttribute("selected", ""), out selected);

                    options.Add(new ChoiceOption(key, text, enabled, selected));
                }
            }
        }

        protected override IFieldIndexHandler CreateDefaultIndexFieldHandler()
        {
            return new SenseNet.Search.Indexing.ChoiceIndexHandler();
        }

        // ======================================================================= Helper methods

        private string GetResourceKey(string enumName)
        {
            // FieldSetting content types are defined on-the-fly and have
            // all the fields, so we cannot find the real owner. We have to
            // use the "FieldSettingContent" base class name here.
            var typeName = this.Owner.IsInstaceOfOrDerivedFrom("FieldSettingContent") 
                ? "FieldSettingContent"
                : this.Owner.Name;

            return string.Format("Enum-{0}-{1}-{2}", typeName, this.Name, enumName);
        }
    }
}