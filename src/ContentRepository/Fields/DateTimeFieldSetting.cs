using System;
using System.Collections.Generic;
using SenseNet.ContentRepository.Schema;
using System.Xml.XPath;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SenseNet.Search.Indexing;

namespace SenseNet.ContentRepository.Fields
{
    public enum DateTimeMode
    {
        None, 
        Date, 
        DateAndTime
    }
    public enum DateTimePrecision
    {
        Millisecond, Second, Minute, Hour, Day
    }

    public class DateTimeFieldSetting : FieldSetting
    {
        private const string MaxValueName = "MaxValue";
        private const string MinValueName = "MinValue";

        public const string DateTimeModeName = "DateTimeMode";
        public const string PrecisionName = "Precision";
        public static readonly DateTimePrecision DefaultPrecision = DateTimePrecision.Minute;
        
        private string _minValue;

        /// <summary>
        /// Minimum value for the date field defined explicitly or by a dynamic template.
        /// </summary>
        public string MinValue
        {
            get => !string.IsNullOrEmpty(_minValue)
                ? _minValue
                : ((DateTimeFieldSetting) ParentFieldSetting)?.MinValue;
            set
            {
                if (!_mutable)
                    throw new InvalidOperationException("Setting MinValue is not allowed within readonly instance.");
                _minValue = value;
            }
        }

        /// <summary>
        /// Evaluates the minimum value and converts it to a date value.
        /// </summary>
        public DateTime? GetMinValue()
        {
            var minValueText = MinValue;
            if (string.IsNullOrEmpty(minValueText))
                return null;

            // evaluate templates like @@Today@@
            minValueText = TemplateManager.Replace(typeof(DateTimeTemplateReplacer), minValueText);

            if (DateTime.TryParse(minValueText, out var minValue))
                return RepositoryTools.ConvertToUtcDateTime(minValue);

            return null;
        }

        private string _maxValue;

        /// <summary>
        /// Maximum value for the date field defined explicitly or by a dynamic template.
        /// </summary>
        public string MaxValue
        {
            get => !string.IsNullOrEmpty(_maxValue)
                ? _maxValue
                : ((DateTimeFieldSetting)ParentFieldSetting)?.MaxValue;
            set
            {
                if (!_mutable)
                    throw new InvalidOperationException("Setting MaxValue is not allowed within readonly instance.");
                _maxValue = value;
            }
        }

        /// <summary>
        /// Evaluates the maximum value and converts it to a date value.
        /// </summary>
        public DateTime? GetMaxValue()
        {
            var maxValueText = MaxValue;
            if (string.IsNullOrEmpty(maxValueText))
                return null;

            // evaluate templates like @@Today@@
            maxValueText = TemplateManager.Replace(typeof(DateTimeTemplateReplacer), maxValueText);

            if (DateTime.TryParse(maxValueText, out var maxValue))
                return RepositoryTools.ConvertToUtcDateTime(maxValue);

            return null;
        }

        private DateTimeMode? _dateTimeMode;
        [JsonConverter(typeof(StringEnumConverter))]
        public DateTimeMode? DateTimeMode
        {
            get
            {
                if (_dateTimeMode.HasValue)
                    return _dateTimeMode;

                return this.ParentFieldSetting == null ? null : 
                    ((DateTimeFieldSetting)this.ParentFieldSetting).DateTimeMode;
            }
            set
            {
                if (!_mutable)
                    throw new InvalidOperationException("Setting DateTimeMode is not allowed within readonly instance.");
                _dateTimeMode = value;
            }
        }

        private DateTimePrecision? _precision;
        [JsonConverter(typeof(StringEnumConverter))]
        public DateTimePrecision? Precision
        {
            get
            {
                if (_precision.HasValue)
                    return _precision;
                return this.ParentFieldSetting == null ? null :
                    ((DateTimeFieldSetting)this.ParentFieldSetting).Precision;
            }
            set
            {
                if (!_mutable)
                    throw new InvalidOperationException("Setting Precision is not allowed within readonly instance.");
                _precision = value;
            }
        }

        protected override void ParseConfiguration(XPathNavigator configurationElement, IXmlNamespaceResolver xmlNamespaceResolver, ContentType contentType)
        {
            base.ParseConfiguration(configurationElement, xmlNamespaceResolver, contentType);

            foreach (XPathNavigator node in configurationElement.SelectChildren(XPathNodeType.Element))
            {
                switch (node.LocalName)
                {
                    case MinValueName:
                        if (!string.IsNullOrEmpty(node.InnerXml))
                            _minValue = node.InnerXml;
                        break;
                    case MaxValueName:
                        if (!string.IsNullOrEmpty(node.InnerXml))
                            _maxValue = node.InnerXml;
                        break;
                    case DateTimeModeName:
                        ParseEnumValue(node.InnerXml, ref _dateTimeMode);
                        break;
                    case PrecisionName:
                        ParseEnumValue(node.InnerXml, ref _precision);
                        break;
                }
            }
        }
        protected override void ParseConfiguration(Dictionary<string, object> info)
        {
            base.ParseConfiguration(info);
            _minValue = GetConfigurationStringValue(info, MinValueName, null);
            _maxValue = GetConfigurationStringValue(info, MaxValueName, null);
            _dateTimeMode = GetConfigurationNullableValue<DateTimeMode>(info, DateTimeModeName, null);
            _precision = GetConfigurationNullableValue<DateTimePrecision>(info, PrecisionName, null);
        }
        protected override Dictionary<string, object> WriteConfiguration()
        {
            var result = base.WriteConfiguration();
            result.Add(MinValueName, _minValue);
            result.Add(MaxValueName, _maxValue);
            result.Add(DateTimeModeName, _dateTimeMode);
            result.Add(PrecisionName, _precision);
            return result;
        }

        protected override void WriteConfiguration(XmlWriter writer)
        {
            WriteElement(writer, _minValue, MinValueName);
            WriteElement(writer, _maxValue, MaxValueName);
            if (this._dateTimeMode.HasValue)
                WriteElement(writer, this._dateTimeMode.Value.ToString(), DateTimeModeName);
            if(this._precision.HasValue)
                WriteElement(writer, this._precision.Value.ToString(), PrecisionName);
        }

        public override FieldValidationResult ValidateData(object value, Field field)
        {
            if ((value == null) && (this.Compulsory ?? false))
                return new FieldValidationResult(CompulsoryName);

            if (value != null)
            {
                var dateTimeValue = (DateTime)value;
                var min = this.GetMinValue() ?? DateTime.MinValue;
                var max = this.GetMaxValue() ?? DateTime.MaxValue;

                if (dateTimeValue < min)
                {
                    var result = new FieldValidationResult(MinValueName);
                    result.AddParameter(MinValueName, min);
                    return result;
                }

                if (dateTimeValue > max)
                {
                    var result = new FieldValidationResult(MaxValueName);
                    result.AddParameter(MaxValueName, max);
                    return result;
                }

            }
            return FieldValidationResult.Successful;
        }

        protected override void CopyPropertiesFrom(FieldSetting source)
        {
            base.CopyPropertiesFrom(source);

            var etSource = (DateTimeFieldSetting)source;

            MinValue = etSource.MinValue;
            MaxValue = etSource.MaxValue;
            DateTimeMode = etSource.DateTimeMode;
            Precision = etSource.Precision;
        }

        public override IDictionary<string, FieldMetadata> GetFieldMetadata()
        {
            var fmd = base.GetFieldMetadata();

            var minFs = new ShortTextFieldSetting
            {
                Name = MinValueName,
                DisplayName = GetTitleString(MinValueName),
                Description = GetDescString(MinValueName),
                FieldClassName = typeof(ShortTextField).FullName
            };
            var maxFs = new ShortTextFieldSetting
            {
                Name = MaxValueName,
                DisplayName = GetTitleString(MaxValueName),
                Description = GetDescString(MaxValueName),
                FieldClassName = typeof(ShortTextField).FullName
            };

            fmd.Add(MinValueName, new FieldMetadata
            {
                FieldName = MinValueName,
                CanRead = true,
                CanWrite = true,
                FieldSetting = minFs
            });

            fmd.Add(MaxValueName, new FieldMetadata
            {
                FieldName = MaxValueName,
                CanRead = true,
                CanWrite = true,
                FieldSetting = maxFs
            });

            fmd.Add(DateTimeModeName, new FieldMetadata
            {
                FieldName = DateTimeModeName,
                CanRead = true,
                CanWrite = true,
                FieldSetting = new ChoiceFieldSetting
                {
                    Name = DateTimeModeName,
                    DisplayName = GetTitleString(DateTimeModeName),
                    Description = GetDescString(DateTimeModeName),
                    EnumTypeName = typeof(DateTimeMode).FullName,
                    DisplayChoice = DisplayChoice.RadioButtons,
                    AllowMultiple = false,
                    AllowExtraValue = false,
                    DefaultValue = ((int)Fields.DateTimeMode.DateAndTime).ToString(),
                    FieldClassName = typeof(ChoiceField).FullName,
                }
            });

            fmd.Add(PrecisionName, new FieldMetadata
            {
                FieldName = PrecisionName,
                CanRead = true,
                CanWrite = true,
                FieldSetting = new ChoiceFieldSetting
                {
                    Name = PrecisionName,
                    DisplayName = GetTitleString(PrecisionName),
                    Description = GetDescString(PrecisionName),
                    EnumTypeName = typeof(DateTimePrecision).FullName,
                    DisplayChoice = DisplayChoice.RadioButtons,
                    AllowMultiple = false,
                    AllowExtraValue = false,
                    DefaultValue = ((int)DefaultPrecision).ToString(),
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
                    case DateTimeModeName:
                        found = true;
                        if (_dateTimeMode.HasValue)
                            val = (int)_dateTimeMode.Value;
                        break;
                    case PrecisionName:
                        found = true;
                        if (_precision.HasValue)
                            val = (int)_precision.Value;
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
                    case DateTimeModeName:
                        found = true;
                        if (value != null)
                            _dateTimeMode = (DateTimeMode)Convert.ToInt32(value);
                        break;
                    case PrecisionName:
                        found = true;
                        if (value != null)
                            _precision = (DateTimePrecision)Convert.ToInt32(value);
                        break;
                }
            }

            return found;
        }

        protected override IFieldIndexHandler CreateDefaultIndexFieldHandler()
        {
            return new DateTimeIndexHandler();
        }
    }
}
