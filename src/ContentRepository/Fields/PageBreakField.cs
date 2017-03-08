using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository.Schema;

namespace SenseNet.ContentRepository.Fields
{
    [ShortName("PageBreak")]
    [DataSlot(0, RepositoryDataType.String, typeof(string))]
    [DefaultFieldSetting(typeof(PageBreakFieldSetting))]
    [DefaultFieldControl("SenseNet.Portal.UI.Controls.EmptyControl")]
    public class PageBreakField : Field
    {
        protected override bool HasExportData { get { return true; } }
        protected override void ExportData(System.Xml.XmlWriter writer, ExportContext context)
        {
            writer.WriteString((string)GetData());
        }
        protected override void ImportData(System.Xml.XmlNode fieldNode, ImportContext context)
        {
            this.SetData(fieldNode.InnerXml);
        }
        protected override bool ParseValue(string value)
        {
            this.SetData(value);
            return true;
        }
    }
}
