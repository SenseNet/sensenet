using System;
using System.Collections.Generic;
using System.Text;
using  SenseNet.ContentRepository.Schema;
using System.Xml;
using SenseNet.ContentRepository.Versioning;
using System.Globalization;

namespace SenseNet.ContentRepository.Fields
{
	[ShortName("Integer")]
	[DataSlot(0, RepositoryDataType.Int, typeof(Int32), typeof(Byte), typeof(Int16), typeof(SByte), typeof(UInt16),
			typeof(VersioningType), typeof(InheritableVersioningType), typeof(ApprovingType))]
	[DefaultFieldSetting(typeof(IntegerFieldSetting))]
	[DefaultFieldControl("SenseNet.Portal.UI.Controls.WholeNumber")]
	public class IntegerField : Field
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
            return ((int)GetData()).ToString();
        }

		protected override object ConvertTo(object[] handlerValues)
		{
            if (handlerValues[0] == null)
                return null;

			return Convert.ToInt32(handlerValues[0]);
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

			if (propertyType == typeof(Int32))
				return Convert.ToInt32(value);
			else if (propertyType == typeof(Byte))
				return Convert.ToByte(value);
			else if (propertyType == typeof(Int16))
				return Convert.ToInt16(value);

			else if (propertyType == typeof(SByte))
				return Convert.ToSByte(value);
			else if (propertyType == typeof(UInt16))
				return Convert.ToUInt16(value);

			return null;
		}

        protected override bool ParseValue(string value)
        {
            Int32 int32Value;
            if (Int32.TryParse(value, out int32Value))
            {
                this.SetData(int32Value);
                return true;
            }
            return false;
        }

        public override string GetFormattedValue()
        {
            var val = Convert.ToInt32(this.GetData()).ToString("n0");
            var fs = this.FieldSetting as IntegerFieldSetting;
            if (fs != null && fs.ShowAsPercentage.HasValue && fs.ShowAsPercentage.Value)
            {
                val += NumberFormatInfo.CurrentInfo.PercentSymbol;
            }

            return val;
        }
	}
}