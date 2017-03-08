using System;
using System.Collections.Generic;
using System.Text;
using  SenseNet.ContentRepository.Schema;

namespace SenseNet.ContentRepository.Fields
{
	[ShortName("LongText")]
	[DataSlot(0, RepositoryDataType.Text, typeof(string))]
    [DefaultFieldSetting(typeof(LongTextFieldSetting))]
	[DefaultFieldControl("SenseNet.Portal.UI.Controls.LongText")]
	public class LongTextField : Field
	{
		protected override bool HasExportData { get { return true; } }
		protected override void ExportData(System.Xml.XmlWriter writer, ExportContext context)
		{
            writer.WriteCData((string)GetData(false));
		}
		protected override void ImportData(System.Xml.XmlNode fieldNode, ImportContext context)
		{
			this.SetData(fieldNode.InnerText);
		}
        protected override bool ParseValue(string value)
        {
            this.SetData(value);
            return true;
        }

        protected override string GetXmlData()
        {
            return (string)GetData();
        }
        protected override void WriteXmlData(System.Xml.XmlWriter writer)
        {
            writer.WriteCData((string)GetData());
        }
    }
}