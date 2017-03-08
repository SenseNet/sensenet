using System;
using System.Collections.Generic;
using System.Text;
using System.Web.UI.WebControls;
using System.Xml.XPath;

using  SenseNet.ContentRepository.Schema;
using System.Xml;
using SenseNet.ContentRepository.Versioning;

namespace SenseNet.ContentRepository.Fields
{
	[ShortName("ApprovingMode")]
	[DataSlot(0, RepositoryDataType.Int, typeof(ApprovingType))]
	[DefaultFieldSetting(typeof(ChoiceFieldSetting))]
	[DefaultFieldControl("SenseNet.Portal.UI.Controls.DropDown")]
	public class ApprovingModeField : ChoiceField
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
				if (data[0] == ((int)ApprovingType.Inherited).ToString())
					return false;
				return true;
			}
		}

		protected override object ConvertTo(object[] handlerValues)
		{
			List<string> valueAsList = new List<string>(1);
			if ((this.Content.ContentHandler as GenericContent).InheritedApproving)
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
			List<string> listValue = value as List<string>;
			string stringValue = listValue[0];
			int intValue;
			if (Int32.TryParse(stringValue, out intValue))
				return (ApprovingType)intValue;
			return (ApprovingType)Enum.Parse(typeof(ApprovingType), stringValue);

		}

		protected override void ImportData(System.Xml.XmlNode fieldNode, ImportContext context)
		{
			this.SetData(new List<string>(new string[] { fieldNode.InnerXml }));
		}
	}
}