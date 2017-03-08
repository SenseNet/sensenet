using System;
using System.Collections.Generic;
using System.Text;
using  SenseNet.ContentRepository.Schema;
using System.Xml.XPath;
using System.Xml;
using System.Globalization;
using System.Web;
using SenseNet.ContentRepository.Storage;

namespace SenseNet.ContentRepository.Fields
{
	public class NumberFieldSetting : FieldSetting
	{
		public const string MaxValueName = "MaxValue";
		public const string MinValueName = "MinValue";
		public const string DigitsName = "Digits";
	    public const string ShowAsPercentageName = "ShowAsPercentage";
        public const string StepName = "Step";

		private decimal? _minValue;
		private decimal? _maxValue;
		private int? _digits;
	    private bool? _showAsPercentage;
        private decimal? _step;

        private decimal _slotMinValue;
        private decimal _slotMaxValue;

		public decimal? MinValue
		{
			get
			{
				if (_minValue.HasValue)
					return _minValue.Value;

				return this.ParentFieldSetting == null ? null : 
                    ((NumberFieldSetting)this.ParentFieldSetting).MinValue;
			}
            set
            {
                if (!_mutable)
                    throw new InvalidOperationException("Setting MinValue is not allowed within readonly instance.");

                if (_slotMinValue == default(decimal))
                    Initialize();

                _minValue = value;
            }
		}
		public decimal? MaxValue
		{
			get
			{
				if (_maxValue.HasValue)
					return _maxValue.Value;
				return this.ParentFieldSetting == null ? null : 
                    ((NumberFieldSetting)this.ParentFieldSetting).MaxValue;
			}
            set
            {
                if (!_mutable)
                    throw new InvalidOperationException("Setting MaxValue is not allowed within readonly instance.");

                if (_slotMinValue == default(decimal))
                    Initialize();

                _maxValue = value;
            }
		}
		public int? Digits
		{
			get
			{
				if (_digits != null)
					return (int)_digits;
				if (this.ParentFieldSetting == null)
					return null;
				return ((NumberFieldSetting)this.ParentFieldSetting).Digits;
			}
            set
            {
                if (!_mutable)
                    throw new InvalidOperationException("Setting Digits is not allowed within readonly instance.");
                _digits = value;
            }
		}
        public bool? ShowAsPercentage
        {
            get
            {
                if (_showAsPercentage.HasValue)
                    return _showAsPercentage.Value;

                return this.ParentFieldSetting == null ? null : 
                    ((NumberFieldSetting)this.ParentFieldSetting).ShowAsPercentage;
            }
            set
            {
                if (!_mutable)
                    throw new InvalidOperationException("Setting ShowAsPercentage is not allowed within readonly instance.");
                _showAsPercentage = value;
            }
        }
        public decimal? Step
        {
            get
            {
                if (_step.HasValue)
                    return _step.Value;

                return ParentFieldSetting == null ? null : ((NumberFieldSetting)this.ParentFieldSetting).Step;
            }
            set
            {
                if (!_mutable)
                    throw new InvalidOperationException("Setting Step is not allowed within readonly instance.");

                _step = value;
            }
        }

        protected override void ParseConfiguration(XPathNavigator configurationElement, IXmlNamespaceResolver xmlNamespaceResolver, ContentType contentType)
		{
			// <MinValue>-6</MinValue>
			// <MaxValue>42</MaxValue>
			foreach (XPathNavigator node in configurationElement.SelectChildren(XPathNodeType.Element))
			{
				switch (node.LocalName)
				{
					case MinValueName:
						decimal minValue;
						if (Decimal.TryParse(node.InnerXml, NumberStyles.Number, CultureInfo.InvariantCulture, out minValue))
							_minValue = minValue;
						break;
					case MaxValueName:
						decimal maxValue;
						if (Decimal.TryParse(node.InnerXml, NumberStyles.Number, CultureInfo.InvariantCulture, out maxValue))
							_maxValue = maxValue;
						break;
					case DigitsName:
						int digits;
						if (Int32.TryParse(node.InnerXml, NumberStyles.Number, CultureInfo.InvariantCulture, out digits))
							_digits = digits;
						break;
                    case ShowAsPercentageName:
                        bool perc;
                        if (Boolean.TryParse(node.InnerXml, out perc))
                            _showAsPercentage = perc;
                        break;
                    case StepName:
                        decimal step;
                        if (Decimal.TryParse(node.InnerXml, out step))
                            _step = step;
                        break;
                }
			}
		}
        protected override void ParseConfiguration(Dictionary<string, object> info)
        {
            base.ParseConfiguration(info);
            _minValue = GetConfigurationNullableValue<decimal>(info, MinValueName, null);
            _maxValue = GetConfigurationNullableValue<decimal>(info, MaxValueName, null);
            _digits = GetConfigurationNullableValue<int>(info, DigitsName, null);
            _showAsPercentage = GetConfigurationNullableValue<bool>(info, ShowAsPercentageName, null);
            _step = GetConfigurationNullableValue<int>(info, StepName, null);
        }
        protected override Dictionary<string, object> WriteConfiguration()
        {
            var result = base.WriteConfiguration();
            result.Add(MinValueName, _minValue);
            result.Add(MaxValueName, _maxValue);
            result.Add(DigitsName, _digits);
            result.Add(ShowAsPercentageName, _showAsPercentage);
            result.Add(StepName, _step);
            return result;
        }

        public override void Initialize()
        {
            SetSlotMinMaxValues();
        }
		protected override void SetDefaults()
		{
			_minValue = null;
			_maxValue = null;
		    _digits = null;
		    _showAsPercentage = null;
            _step = null;
        }

		public override FieldValidationResult ValidateData(object value, Field field)
		{
            if ((value == null) && (this.Compulsory ?? false))
                return new FieldValidationResult(CompulsoryName);

            if (value != null)
            {
                var decimalValue = Convert.ToDecimal(value);

                if (this.MinValue != null)
                {
                    if (decimalValue < (decimal) this.MinValue)
                    {
                        var result = new FieldValidationResult(MinValueName);
                        result.AddParameter(MinValueName, this.MinValue);
                        return result;
                    }
                }
                if (this.MaxValue != null)
                {
                    if (decimalValue > (decimal) this.MaxValue)
                    {
                        var result = new FieldValidationResult(MaxValueName);
                        result.AddParameter(MaxValueName, this.MaxValue);
                        return result;
                    }
                }
            }

		    return FieldValidationResult.Successful;
		}

        protected override void CopyPropertiesFrom(FieldSetting source)
        {
            base.CopyPropertiesFrom(source);

            var fsSource = (NumberFieldSetting) source;

            MinValue = fsSource.MinValue;
            MaxValue = fsSource.MaxValue;
            Digits = fsSource.Digits;
            ShowAsPercentage = fsSource.ShowAsPercentage;
            Step = fsSource.Step;
        }

	    private void SetSlotMinMaxValues()
        {
            var propertyType = GetHandlerSlot(0);
            if (propertyType == typeof(Int32))
            {
                _slotMinValue = Int32.MinValue;
                _slotMaxValue = Int32.MaxValue;
            }
            else if (propertyType == typeof(Byte))
            {
                _slotMinValue = Byte.MinValue;
                _slotMaxValue = Byte.MaxValue;
            }
            else if (propertyType == typeof(Int16))
            {
                _slotMinValue = Int16.MinValue;
                _slotMaxValue = Int16.MaxValue;
            }

            else if (propertyType == typeof(Int64))
            {
                _slotMinValue = Int64.MinValue;
                _slotMaxValue = Int64.MaxValue;
            }
            else if (propertyType == typeof(Single))
            {
                _slotMinValue = ActiveSchema.DecimalMinValue;
                _slotMaxValue = ActiveSchema.DecimalMaxValue;
            }
            else if (propertyType == typeof(Double))
            {
                _slotMinValue = ActiveSchema.DecimalMinValue;
                _slotMaxValue = ActiveSchema.DecimalMaxValue;
            }
            else if (propertyType == typeof(Decimal))
            {
                _slotMinValue = ActiveSchema.DecimalMinValue;
                _slotMaxValue = ActiveSchema.DecimalMaxValue;
            }
            else if (propertyType == typeof(UInt32))
            {
                _slotMinValue = UInt32.MinValue;
                _slotMaxValue = UInt32.MaxValue;
            }
            else if (propertyType == typeof(UInt64))
            {
                _slotMinValue = UInt64.MinValue;
                _slotMaxValue = UInt64.MaxValue;
            }

            else if (propertyType == typeof(SByte))
            {
                _slotMinValue = SByte.MinValue;
                _slotMaxValue = SByte.MaxValue;
            }
            else if (propertyType == typeof(UInt16))
            {
                _slotMinValue = UInt16.MinValue;
                _slotMaxValue = UInt16.MaxValue;
            }
            else
            {
                throw new SnNotSupportedException();
            }
        }

        protected override void WriteConfiguration(XmlWriter writer)
        {
            WriteElement(writer, this._minValue, MinValueName);
            WriteElement(writer, this._maxValue, MaxValueName);
            WriteElement(writer, this._digits, DigitsName);
            WriteElement(writer, this._showAsPercentage, ShowAsPercentageName);
            WriteElement(writer, this._step, StepName);
        }

        public override IDictionary<string, FieldMetadata> GetFieldMetadata()
        {
            var fmd = base.GetFieldMetadata();
            var digitsFs = new IntegerFieldSetting
                              {
                                  Name = DigitsName,
                                  DisplayName = GetTitleString(DigitsName),
                                  Description = GetDescString(DigitsName),
                                  ShortName = "Integer",
                                  FieldClassName = typeof (IntegerField).FullName
                              };


            digitsFs.Initialize();
            digitsFs.MinValue = 0;
            digitsFs.MaxValue = 10;

            fmd.Add(MinValueName, new FieldMetadata
            {
                FieldName = MinValueName,
                CanRead = true,
                CanWrite = true,
                FieldSetting = new NumberFieldSetting
                {
                    Name = MinValueName,
                    DisplayName = GetTitleString(MinValueName),
                    Description = GetDescString(MinValueName),
                    ShortName = "Number",
                    FieldClassName = typeof(NumberField).FullName,
                    MinValue = ActiveSchema.DecimalMinValue,
                    MaxValue = ActiveSchema.DecimalMaxValue,
                    Digits = 2
                }
            });

            fmd.Add(MaxValueName, new FieldMetadata
            {
                FieldName = MaxValueName,
                CanRead = true,
                CanWrite = true,
                FieldSetting = new NumberFieldSetting
                {
                    Name = MaxValueName,
                    DisplayName = GetTitleString(MaxValueName),
                    Description = GetDescString(MaxValueName),
                    ShortName = "Number",
                    FieldClassName = typeof(NumberField).FullName,
                    MinValue = ActiveSchema.DecimalMinValue,
                    MaxValue = ActiveSchema.DecimalMaxValue,
                    Digits = 2
                }
            });

            fmd.Add(DigitsName, new FieldMetadata
            {
                FieldName = DigitsName,
                PropertyType = typeof(int?),
                FieldType = DynamicContentTools.GetSuggestedFieldType(typeof(int?)),
                DisplayName = GetTitleString(DigitsName),
                Description = GetDescString(DigitsName),
                CanRead = true,
                CanWrite = true,
                FieldSetting = digitsFs
            });

            fmd.Add(StepName, new FieldMetadata {

                FieldName = StepName,
                CanRead = true,
                CanWrite = true,
                FieldSetting = new NumberFieldSetting
                {
                    Name = StepName,
                    DisplayName = GetTitleString(StepName),
                    Description = GetDescString(StepName),
                    ShortName = "Number",
                    FieldClassName = typeof(NumberField).FullName
                }
            });

            fmd.Add(ShowAsPercentageName, new FieldMetadata
            {
                FieldName = ShowAsPercentageName,
                CanRead = true,
                CanWrite = true,
                FieldSetting = new YesNoFieldSetting
                {
                    Name = ShowAsPercentageName,
                    DisplayName = GetTitleString(ShowAsPercentageName),
                    Description = GetDescString(ShowAsPercentageName),
                    DisplayChoice = DisplayChoice.RadioButtons,
                    DefaultValue = YesNoFieldSetting.NoValue,
                    FieldClassName = typeof(YesNoField).FullName,
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
                    case ShowAsPercentageName:
                        found = true;
                        if (_showAsPercentage.HasValue)
                            val = _showAsPercentage.Value ? YesNoFieldSetting.YesValue : YesNoFieldSetting.NoValue;
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
                    case ShowAsPercentageName:
                        found = true;
                        if (value != null)
                            _showAsPercentage = YesNoFieldSetting.YesValue.CompareTo(value as string) == 0;
                        break;
                }
            }

            return found;
        }

        protected override SenseNet.Search.Indexing.FieldIndexHandler CreateDefaultIndexFieldHandler()
        {
            return new SenseNet.Search.Indexing.NumberIndexHandler();
        }
    }
}