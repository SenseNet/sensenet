using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Globalization;
using  SenseNet.ContentRepository.Schema;
using SenseNet.Diagnostics;

namespace SenseNet.ContentRepository.Fields
{
	[ShortName("Color")]
	[DataSlot(0, RepositoryDataType.String, typeof(string), typeof(Color))]
	[DefaultFieldSetting(typeof(ColorFieldSetting))]
	[DefaultFieldControl("SenseNet.Portal.UI.Controls.ColorPicker")]
	public class ColorField : Field
	{
		protected override bool HasExportData { get { return true; } }
		protected override void ExportData(System.Xml.XmlWriter writer, ExportContext context)
		{
            writer.WriteString(GetXmlData());
		}
		protected override void ImportData(System.Xml.XmlNode fieldNode, ImportContext context)
		{
			this.SetData(ColorFromString(fieldNode.InnerXml));
		}

        protected override string GetXmlData()
        {
            return ColorToString((Color)GetData());
        }

		protected override object ConvertTo(object[] handlerValues)
		{
			if (handlerValues[0] == null)
				return Color.Black;
			Type propertyType = this.GetHandlerSlot(0);
			if (propertyType == typeof(Color))
				return handlerValues[0];
			if (propertyType == typeof(string))
				return ColorFromString((string)handlerValues[0]);
			throw new NotSupportedException(String.Concat("ColorField not supports this conversion: ", propertyType.FullName, " to ", typeof(Color).FullName));
		}
		protected override object[] ConvertFrom(object value)
		{
			return new object[] { ConvertFromControlInner(value) };
		}
		private object ConvertFromControlInner(object value)
		{
			Type propertyType = this.GetHandlerSlot(0);
			if (value == null)
			{
				if (propertyType == typeof(Color))
					return Color.Empty;
				if (propertyType == typeof(string))
					return null;
			}
			if (value.GetType() == typeof(string))
			{
				if (propertyType == typeof(Color))
					return ColorFromString((string)value);
				if (propertyType == typeof(string))
					return value;
			}
			if (value.GetType() == typeof(Color))
			{
				if (propertyType == typeof(Color))
					return value;
				if (propertyType == typeof(string))
					return ColorToString((Color)value);
			}
			throw new NotSupportedException(String.Concat("ColorField not supports this conversion: ", typeof(Color).FullName, " to ", propertyType.FullName));
		}

		public static string ColorToString(Color color)
		{
			if (color == Color.Empty)
				return "";
			return String.Concat("#", color.R.ToString("X2"), color.G.ToString("X2"), color.B.ToString("X2"));
		}
		public static Color ColorFromString(string value)
		{
			if (String.IsNullOrEmpty(value))
				return Color.Empty;

			if (value.StartsWith("#"))
			{
				try
				{
					return Color.FromArgb(
						Byte.Parse(value.Substring(1, 2), NumberStyles.HexNumber),
						Byte.Parse(value.Substring(3, 2), NumberStyles.HexNumber),
						Byte.Parse(value.Substring(5, 2), NumberStyles.HexNumber));
				}
				catch(Exception e) // logged
				{
                    SnLog.WriteException(e);
					return Color.Empty;
				}
			}

			var colorString = value;
			// "Color [Red]"
			colorString = colorString.Replace("Color [", "").Replace("]", "");
			if (colorString == "Empty")
				return Color.Empty;

			try
			{
				return Color.FromKnownColor((KnownColor)Enum.Parse(typeof(KnownColor), colorString, true));
			}
            catch (Exception e) // logged
            {
                SnLog.WriteException(e);
                return Color.Empty;
			}
		}

        protected override bool ParseValue(string value)
        {
            var colorValue = ColorFromString(value);
            if (colorValue == Color.Empty)
                return false;
            this.SetData(colorValue);
            return true;
        }

        protected override void WriteXmlData(System.Xml.XmlWriter writer)
        {
            ExportData(writer, null);
        }
	}
}