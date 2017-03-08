using System;
using System.Collections.Generic;
using System.Text;

using  SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage.Schema;

namespace SenseNet.ContentRepository.Fields
{
	[ShortName("NodeType")]
	[DataSlot(0, RepositoryDataType.NotDefined, typeof(NodeType))]
	[DefaultFieldSetting(typeof(NullFieldSetting))]
	[DefaultFieldControl("SenseNet.Portal.UI.Controls.ShortText")]
	public class NodeTypeField : Field
	{
		protected override void ImportData(System.Xml.XmlNode fieldNode, ImportContext context)
		{
			throw new NotSupportedException("The ImportData operation is not supported on NodeTypeField.");
		}
        protected override void WriteXmlData(System.Xml.XmlWriter writer)
        {
            writer.WriteString(this.Content.ContentHandler.NodeType.Name);
        }

        protected override string GetXmlData()
        {
            return this.Content.ContentHandler.NodeType.Name;
        }
	}
}