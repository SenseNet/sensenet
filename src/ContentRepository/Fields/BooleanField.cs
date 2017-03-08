using System;
using System.Collections.Generic;
using System.Text;

using  SenseNet.ContentRepository.Schema;

namespace SenseNet.ContentRepository.Fields
{
	[ShortName("Boolean")]
	[DataSlot(0, RepositoryDataType.Int, typeof(bool), typeof(Int32), typeof(decimal), typeof(byte),
			typeof(Int16), typeof(Int64), typeof(Single), typeof(Double), typeof(SByte), typeof(UInt16), typeof(UInt32), typeof(UInt64))]
	[DefaultFieldSetting(typeof(NullFieldSetting))]
	[DefaultFieldControl("SenseNet.Portal.UI.Controls.Boolean")]
	public class BooleanField : Field
	{
		protected override bool HasExportData { get { return true; } }
		protected override void ExportData(System.Xml.XmlWriter writer, ExportContext context)
		{
			writer.WriteString((bool)GetData() ? "true" : "false");
		}
		protected override void ImportData(System.Xml.XmlNode fieldNode, ImportContext context)
		{
			this.SetData(fieldNode.InnerXml == "true");
		}

        protected override string GetXmlData()
        {
            return (bool)GetData() ? "True" : "False";
        }

		protected override object ConvertTo(object[] handlerValues)
		{
			Type propertyType = this.GetHandlerSlot(0);
			if (propertyType == typeof(bool))
				return Convert.ToInt32(handlerValues[0]) == 1;
			else if (propertyType == typeof(Int32))
				return (Int32)handlerValues[0] != 0;
			else if (propertyType == typeof(Byte))
				return (Byte)handlerValues[0] != 0;
			else if (propertyType == typeof(Int16))
				return (Int16)handlerValues[0] != 0;
			else if (propertyType == typeof(Int64))
				return (Int64)handlerValues[0] != 0;
			else if (propertyType == typeof(Single))
				return (Single)handlerValues[0] != 0;
			else if (propertyType == typeof(Double))
				return (Double)handlerValues[0] != 0;
			else if (propertyType == typeof(Decimal))
				return (Decimal)handlerValues[0] != 0;
			return null;
		}
		protected override object[] ConvertFrom(object value)
		{
			return new object[] { ConvertFromControlInner(value) };
		}
		private object ConvertFromControlInner(object value)
		{
            bool boolValue = false;
            if (value != null && value is bool)
			    boolValue = (bool)value;

			Type propertyType = this.GetHandlerSlot(0);
			if (propertyType == typeof(bool))
				return boolValue;
			else if (propertyType == typeof(Int32))
				return (Int32)(boolValue ? 1 : 0);
			else if (propertyType == typeof(Byte))
				return (Byte)(boolValue ? 1 : 0);
			else if (propertyType == typeof(Int16))
				return (Int16)(boolValue ? 1 : 0);
			else if (propertyType == typeof(Int64))
				return (Int64)(boolValue ? 1 : 0);
			else if (propertyType == typeof(Single))
				return (Single)(boolValue ? 1 : 0);
			else if (propertyType == typeof(Double))
				return (Double)(boolValue ? 1 : 0);
			else if (propertyType == typeof(Decimal))
				return (Decimal)(boolValue ? 1 : 0);

			return null;
		}

        protected override bool ParseValue(string value)
        {
            var boolValue = ParseString(value);
            this.SetData(boolValue);
            return true;
        }
        private static readonly List<string> FalseValues = new List<string>(new string[] { "FALSE", "NO", String.Empty });
        private bool ParseString(string value)
        {
            if (value == null)
                return false;

            double number;
            if (double.TryParse(value, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out number))
                return number != 0.0;

            if (FalseValues.Contains(value.ToUpper()))
                return false;
            return true;
        }
	}
}