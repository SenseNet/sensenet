using System;
using System.Collections.Generic;
using System.Text;
using  SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage.Security;

namespace SenseNet.ContentRepository.Fields
{
	[ShortName("Security")]
	[DataSlot(0, RepositoryDataType.NotDefined, typeof(SecurityHandler))]
	[DefaultFieldSetting(typeof(NullFieldSetting))]
	[DefaultFieldControl("SenseNet.Portal.UI.Controls.ShortText")]
	public class SecurityField : Field
	{
		protected override void ImportData(System.Xml.XmlNode fieldNode, ImportContext context)
		{
			throw new NotSupportedException("The ImportData operation is not supported on SecurityField.");
		}
	}
}