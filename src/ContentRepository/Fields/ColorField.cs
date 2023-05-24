using System;
using System.Collections.Generic;
using System.Text;
using SkiaSharp;
using System.Globalization;
using  SenseNet.ContentRepository.Schema;
using SenseNet.Diagnostics;

namespace SenseNet.ContentRepository.Fields
{
	[ShortName("Color")]
	[DataSlot(0, RepositoryDataType.String, typeof(string), typeof(SKColor))]
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
            return ColorToString((SKColor)GetData());
        }

		protected override object ConvertTo(object[] handlerValues)
		{
			if (handlerValues[0] == null)
				return SKColors.Black;
			Type propertyType = this.GetHandlerSlot(0);
			if (propertyType == typeof(SKColor))
				return handlerValues[0];
			if (propertyType == typeof(string))
				return ColorFromString((string)handlerValues[0]);
			throw new NotSupportedException(String.Concat("ColorField not supports this conversion: ", propertyType.FullName, " to ", typeof(SKColor).FullName));
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
				if (propertyType == typeof(SKColor))
					return SKColor.Empty;
				if (propertyType == typeof(string))
					return null;
			}
			if (value.GetType() == typeof(string))
			{
				if (propertyType == typeof(SKColor))
					return ColorFromString((string)value);
				if (propertyType == typeof(string))
					return value;
			}
			if (value.GetType() == typeof(SKColor))
			{
				if (propertyType == typeof(SKColor))
					return value;
				if (propertyType == typeof(string))
					return ColorToString((SKColor)value);
			}
			throw new NotSupportedException(String.Concat("ColorField not supports this conversion: ", typeof(SKColor).FullName, " to ", propertyType.FullName));
		}

		public static string ColorToString(SKColor color)
		{
			if (color == SKColor.Empty)
				return "";
			return string.Concat("#",
                color.Red.ToString("X2"), color.Green.ToString("X2"), color.Blue.ToString("X2"));
		}
		public static SKColor ColorFromString(string value)
		{
			if (String.IsNullOrEmpty(value))
				return SKColor.Empty;

            if (value.StartsWith("#"))
            {
                try
                {
                    if (value.Length == 7)
                    {
                        return new SKColor(byte.Parse(value.Substring(1, 2), NumberStyles.HexNumber),
                            byte.Parse(value.Substring(3, 2), NumberStyles.HexNumber),
                            byte.Parse(value.Substring(5, 2), NumberStyles.HexNumber));
                    }
                    var r = char.ToString(value[1]);
                    var g = char.ToString(value[2]);
                    var b = char.ToString(value[3]);
                    return new SKColor(byte.Parse(r + r, NumberStyles.HexNumber),
                        byte.Parse(g + g, NumberStyles.HexNumber),
                        byte.Parse(b + b, NumberStyles.HexNumber));
                }
                catch (Exception e) // logged
                {
                    SnLog.WriteException(e);
                    return SKColor.Empty;
                }
            }

			// "Color [Red]"
			var colorString = value.Replace("Color [", "").Replace("]", "");
			if (colorString == "Empty")
				return SKColor.Empty;

			try
            {
                return !ColorTable.TryGetValue(colorString, out var colorCode) ? SKColor.Empty : SKColor.Parse(colorCode);
            }
            catch (Exception e) // logged
            {
                SnLog.WriteException(e);
                return SKColor.Empty;
			}
		}

        protected override bool ParseValue(string value)
        {
            var colorValue = ColorFromString(value);
            if (colorValue == SKColor.Empty)
                return false;
            this.SetData(colorValue);
            return true;
        }

        protected override void WriteXmlData(System.Xml.XmlWriter writer)
        {
            ExportData(writer, null);
        }

        private static readonly Dictionary<string, string> ColorTable = new()
        {
            {"transparent", "00FFFFFF"},
            {"aliceblue", "FFF0F8FF"},
            {"antiquewhite", "FFFAEBD7"},
            {"aqua", "FF00FFFF"},
            {"aquamarine", "FF7FFFD4"},
            {"azure", "FFF0FFFF"},
            {"beige", "FFF5F5DC"},
            {"bisque", "FFFFE4C4"},
            {"black", "FF000000"},
            {"blanchedalmond", "FFFFEBCD"},
            {"blue", "FF0000FF"},
            {"blueviolet", "FF8A2BE2"},
            {"brown", "FFA52A2A"},
            {"burlywood", "FFDEB887"},
            {"cadetblue", "FF5F9EA0"},
            {"chartreuse", "FF7FFF00"},
            {"chocolate", "FFD2691E"},
            {"coral", "FFFF7F50"},
            {"cornflowerblue", "FF6495ED"},
            {"cornsilk", "FFFFF8DC"},
            {"crimson", "FFDC143C"},
            {"cyan", "FF00FFFF"},
            {"darkblue", "FF00008B"},
            {"darkcyan", "FF008B8B"},
            {"darkgoldenrod", "FFB8860B"},
            {"darkgray", "FFA9A9A9"},
            {"darkgreen", "FF006400"},
            {"darkkhaki", "FFBDB76B"},
            {"darkmagenta", "FF8B008B"},
            {"darkolivegreen", "FF556B2F"},
            {"darkorange", "FFFF8C00"},
            {"darkorchid", "FF9932CC"},
            {"darkred", "FF8B0000"},
            {"darksalmon", "FFE9967A"},
            {"darkseagreen", "FF8FBC8F"},
            {"darkslateblue", "FF483D8B"},
            {"darkslategray", "FF2F4F4F"},
            {"darkturquoise", "FF00CED1"},
            {"darkviolet", "FF9400D3"},
            {"deeppink", "FFFF1493"},
            {"deepskyblue", "FF00BFFF"},
            {"dimgray", "FF696969"},
            {"dodgerblue", "FF1E90FF"},
            {"firebrick", "FFB22222"},
            {"floralwhite", "FFFFFAF0"},
            {"forestgreen", "FF228B22"},
            {"fuchsia", "FFFF00FF"},
            {"gainsboro", "FFDCDCDC"},
            {"ghostwhite", "FFF8F8FF"},
            {"gold", "FFFFD700"},
            {"goldenrod", "FFDAA520"},
            {"gray", "FF808080"},
            {"green", "FF008000"},
            {"greenyellow", "FFADFF2F"},
            {"honeydew", "FFF0FFF0"},
            {"hotpink", "FFFF69B4"},
            {"indianred", "FFCD5C5C"},
            {"indigo", "FF4B0082"},
            {"ivory", "FFFFFFF0"},
            {"khaki", "FFF0E68C"},
            {"lavender", "FFE6E6FA"},
            {"lavenderblush", "FFFFF0F5"},
            {"lawngreen", "FF7CFC00"},
            {"lemonchiffon", "FFFFFACD"},
            {"lightblue", "FFADD8E6"},
            {"lightcoral", "FFF08080"},
            {"lightcyan", "FFE0FFFF"},
            {"lightgoldenrodyellow", "FFFAFAD2"},
            {"lightgray", "FFD3D3D3"},
            {"lightgreen", "FF90EE90"},
            {"lightpink", "FFFFB6C1"},
            {"lightsalmon", "FFFFA07A"},
            {"lightseagreen", "FF20B2AA"},
            {"lightskyblue", "FF87CEFA"},
            {"lightslategray", "FF778899"},
            {"lightsteelblue", "FFB0C4DE"},
            {"lightyellow", "FFFFFFE0"},
            {"lime", "FF00FF00"},
            {"limegreen", "FF32CD32"},
            {"linen", "FFFAF0E6"},
            {"magenta", "FFFF00FF"},
            {"maroon", "FF800000"},
            {"mediumaquamarine", "FF66CDAA"},
            {"mediumblue", "FF0000CD"},
            {"mediumorchid", "FFBA55D3"},
            {"mediumpurple", "FF9370DB"},
            {"mediumseagreen", "FF3CB371"},
            {"mediumslateblue", "FF7B68EE"},
            {"mediumspringgreen", "FF00FA9A"},
            {"mediumturquoise", "FF48D1CC"},
            {"mediumvioletred", "FFC71585"},
            {"midnightblue", "FF191970"},
            {"mintcream", "FFF5FFFA"},
            {"mistyrose", "FFFFE4E1"},
            {"moccasin", "FFFFE4B5"},
            {"navajowhite", "FFFFDEAD"},
            {"navy", "FF000080"},
            {"oldlace", "FFFDF5E6"},
            {"olive", "FF808000"},
            {"olivedrab", "FF6B8E23"},
            {"orange", "FFFFA500"},
            {"orangered", "FFFF4500"},
            {"orchid", "FFDA70D6"},
            {"palegoldenrod", "FFEEE8AA"},
            {"palegreen", "FF98FB98"},
            {"paleturquoise", "FFAFEEEE"},
            {"palevioletred", "FFDB7093"},
            {"papayawhip", "FFFFEFD5"},
            {"peachpuff", "FFFFDAB9"},
            {"peru", "FFCD853F"},
            {"pink", "FFFFC0CB"},
            {"plum", "FFDDA0DD"},
            {"powderblue", "FFB0E0E6"},
            {"purple", "FF800080"},
            {"red", "FFFF0000"},
            {"rosybrown", "FFBC8F8F"},
            {"royalblue", "FF4169E1"},
            {"saddlebrown", "FF8B4513"},
            {"salmon", "FFFA8072"},
            {"sandybrown", "FFF4A460"},
            {"seagreen", "FF2E8B57"},
            {"seashell", "FFFFF5EE"},
            {"sienna", "FFA0522D"},
            {"silver", "FFC0C0C0"},
            {"skyblue", "FF87CEEB"},
            {"slateblue", "FF6A5ACD"},
            {"slategray", "FF708090"},
            {"snow", "FFFFFAFA"},
            {"springgreen", "FF00FF7F"},
            {"steelblue", "FF4682B4"},
            {"tan", "FFD2B48C"},
            {"teal", "FF008080"},
            {"thistle", "FFD8BFD8"},
            {"tomato", "FFFF6347"},
            {"turquoise", "FF40E0D0"},
            {"violet", "FFEE82EE"},
            {"wheat", "FFF5DEB3"},
            {"white", "FFFFFFFF"},
            {"whitesmoke", "FFF5F5F5"},
            {"yellow", "FFFFFF00"},
            {"yellowgreen", "FF9ACD32"},
            {"rebeccapurple", "FF663399"},
        };
    }
}