using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
//
using  SenseNet.ContentRepository.Schema;
using System.Xml;
using SenseNet.ContentRepository.Versioning;
using SenseNet.Diagnostics;

namespace SenseNet.ContentRepository.Fields
{
	[ShortName("Number")]
	[DataSlot(0, RepositoryDataType.Currency, typeof(decimal), typeof(byte), typeof(Int16), typeof(Int32), typeof(Int64),
			typeof(Single), typeof(Double), typeof(SByte), typeof(UInt16), typeof(UInt32), typeof(UInt64),
			typeof(VersioningType), typeof(InheritableVersioningType), typeof(ApprovingType))]
	[DefaultFieldSetting(typeof(NumberFieldSetting))]
	[DefaultFieldControl("SenseNet.Portal.UI.Controls.Number")]
	public class NumberField : Field
	{
		protected override bool HasExportData { get { return true; } }
		protected override void ExportData(System.Xml.XmlWriter writer, ExportContext context)
		{
			writer.WriteString(GetXmlData());
		}
		protected override void ImportData(System.Xml.XmlNode fieldNode, ImportContext context)
		{
			this.SetData(ConvertFromControlInner(fieldNode.InnerXml));
		}

        protected override string GetXmlData()
        {
            return XmlConvert.ToString(Convert.ToDecimal(GetData()));
        }
		protected override object ConvertTo(object[] handlerValues)
		{
			if (handlerValues[0] == null) 
                return null;

            return Convert.ToDecimal(handlerValues[0]);
		}
		protected override object[] ConvertFrom(object value)
		{
			return new object[] { ConvertFromControlInner(value) };
		}
		private object ConvertFromControlInner(object value)
		{
            if (value == null)
                return null;

			Type propertyType = this.GetHandlerSlot(0);
            var ci = System.Globalization.CultureInfo.InvariantCulture;

			if (propertyType == typeof(Int32))
                return Convert.ToInt32(value, ci);
			else if (propertyType == typeof(Byte))
                return Convert.ToByte(value, ci);
			else if (propertyType == typeof(Int16))
                return Convert.ToInt16(value, ci);
			else if (propertyType == typeof(Int64))
                return Convert.ToInt64(value, ci);
			else if (propertyType == typeof(Single))
                return Convert.ToSingle(value, ci);
			else if (propertyType == typeof(Double))
                return Convert.ToDouble(value, ci);
			else if (propertyType == typeof(Decimal))
                return Convert.ToDecimal(value, ci);

			else if (propertyType == typeof(SByte))
                return Convert.ToSByte(value, ci);
			else if (propertyType == typeof(UInt16))
                return Convert.ToUInt16(value, ci);
			else if (propertyType == typeof(UInt32))
                return Convert.ToUInt32(value, ci);
			else if (propertyType == typeof(UInt64))
                return Convert.ToUInt64(value, ci);

			return null;
		}

        protected override bool ParseValue(string value)
        {
            decimal decimalValue;
            if (Decimal.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out decimalValue))
            {
                this.SetData(decimalValue);
                return true;
            }
            return false;
        }

        public override string GetFormattedValue()
        {
            var val = Convert.ToDecimal(this.GetData());
            var fs = this.FieldSetting as NumberFieldSetting;
            var digits = Math.Min(fs == null || !fs.Digits.HasValue ? 0 : fs.Digits.Value, 29);
            var specifier = (fs != null && fs.ShowAsPercentage.HasValue && fs.ShowAsPercentage.Value) ? "p0" : "n" + digits;
            
            return val.ToString(specifier);
        }
	}
}