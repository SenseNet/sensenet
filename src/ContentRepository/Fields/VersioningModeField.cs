using System;
using System.Collections.Generic;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Versioning;

namespace SenseNet.ContentRepository.Fields
{
	[ShortName("VersioningMode")]
	[DataSlot(0, RepositoryDataType.Int, typeof(VersioningType))]
	[DefaultFieldSetting(typeof(ChoiceFieldSetting))]
	[DefaultFieldControl("SenseNet.Portal.UI.Controls.DropDown")]
	public class VersioningModeField : ChoiceField
	{
		protected override bool HasExportData
		{
			get
			{
				var data = (List<string>)GetData();
				if (data.Count == 0)
					return false;
				if (data.Count > 1)
					return true;
				if (data[0] == ((int)VersioningType.Inherited).ToString())
					return false;
				return true;
			}
		}

		protected override void ImportData(System.Xml.XmlNode fieldNode, ImportContext context)
		{
			this.SetData(new List<string>(new string[] { fieldNode.InnerXml }));
		}

		protected override object ConvertTo(object[] handlerValues)
		{
			List<string> valueAsList = new List<string>(1);
			if ((this.Content.ContentHandler as GenericContent).InheritedVersioning)
				valueAsList.Add("0");
			else
				valueAsList.Add(((int)handlerValues[0]).ToString());
			return valueAsList;
		}
		protected override object[] ConvertFrom(object value)
		{
			return new object[] { ConvertFromControlInner(value) };
		}
		private object ConvertFromControlInner(object value)
        {
            string stringValue = null;
            int intValue = 0;

            if (value is int @int)
                intValue = @int;
            else if (value is int[] intArray && intArray.Length > 0)
                intValue = intArray[0];
            else if (value is List<int> intList && intList.Count > 0)
                intValue = intList[0];
            else if (value is string @string)
				stringValue = @string;
            else if (value is string[] stringArray && stringArray.Length > 0)
                stringValue = stringArray[0];
            else if (value is List<string> stringList && stringList.Count > 0)
                stringValue = stringList[0];
			else
                throw GetParsingError();

            if (stringValue != null && !int.TryParse(stringValue, out intValue))
            {
                if (Enum.TryParse<VersioningType>(stringValue, true, out var parsed))
                    return (VersioningType) parsed;
				else
                    throw GetParsingError();
            }

            if (Enum.IsDefined(typeof(VersioningType), intValue))
                return (VersioningType) intValue;

            throw GetParsingError();
        }

        private Exception GetParsingError()
        {
			return new ArgumentException("Cannot parse a VersioningModeField: invalid VersioningType value.");
        }
    }
}