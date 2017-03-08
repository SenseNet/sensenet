using System;
using System.Collections.Generic;
using System.Text;

using  SenseNet.ContentRepository.Schema;

namespace SenseNet.ContentRepository.Fields
{
	[ShortName("SiteRelativeUrl")]
	[DefaultFieldSetting(typeof(NullFieldSetting))]
	[DefaultFieldControl("SenseNet.Portal.UI.Controls.SiteRelativeUrl")]
	public class SiteRelativeUrlField : Field
	{
		public override bool ReadOnly { get { return true; } }
		protected override void ImportData(System.Xml.XmlNode fieldNode, ImportContext context)
		{
			throw new NotSupportedException("The ImportData operation is not supported on SiteRelativeUrlField.");
		}

		protected override object ConvertTo(object[] handlerValues)
		{
			return null;
		}
	}
}