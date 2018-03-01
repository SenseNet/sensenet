using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository.Schema;
using System.Xml.XPath;
using System.Xml;
using SenseNet.ContentRepository.Storage.Search;
using SenseNet.Search;
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
        public const string DateTimeModeName = "DateTimeMode";
        public const string PrecisionName = "Precision";
        public static readonly DateTimePrecision DefaultPrecision = DateTimePrecision.Minute;

        private DateTimeMode? _dateTimeMode;
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
            _dateTimeMode = GetConfigurationNullableValue<DateTimeMode>(info, DateTimeModeName, null);
            _precision = GetConfigurationNullableValue<DateTimePrecision>(info, PrecisionName, null);
        }
        protected override Dictionary<string, object> WriteConfiguration()
        {
            var result = base.WriteConfiguration();
            result.Add(DateTimeModeName, _dateTimeMode);
            result.Add(PrecisionName, _precision);
            return result;
        }

        protected override void WriteConfiguration(XmlWriter writer)
        {
            if (this._dateTimeMode.HasValue)
                WriteElement(writer, this._dateTimeMode.Value.ToString(), DateTimeModeName);
            if(this._precision.HasValue)
                WriteElement(writer, this._precision.Value.ToString(), PrecisionName);
        }

        protected override void CopyPropertiesFrom(FieldSetting source)
        {
            base.CopyPropertiesFrom(source);

            var etSource = (DateTimeFieldSetting)source;

            DateTimeMode = etSource.DateTimeMode;
            Precision = etSource.Precision;
        }

        public override IDictionary<string, FieldMetadata> GetFieldMetadata()
        {
            var fmd = base.GetFieldMetadata();

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
            return new SenseNet.Search.Indexing.DateTimeIndexHandler();
        }
    }
}
