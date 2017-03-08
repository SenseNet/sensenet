using System;
using System.Collections.Generic;
using System.Text;

using SenseNet.ContentRepository.Schema;
using SenseNet.Portal;

namespace SenseNet.ContentRepository.Fields
{
    [ShortName("UrlList")]
    [DataSlot(0, RepositoryDataType.Text, typeof(IDictionary<string, string>))]
    [DefaultFieldSetting(typeof(NullFieldSetting))]
    [DefaultFieldControl("SenseNet.Portal.UI.Controls.UrlList")]
    public class UrlListField : Field, SenseNet.ContentRepository.Xpath.IRawXmlContainer
    {
        protected override bool HasExportData { get { return true; } }
        protected override void ExportData(System.Xml.XmlWriter writer, ExportContext context)
        {
            // For exmple:
            // <Url authType="Forms">localhost:1315/</Url>
            // <Url authType="Windows">name.server.xy</Url>

            writer.WriteRaw(GetRawXml());
        }
        protected override void ImportData(System.Xml.XmlNode fieldNode, ImportContext context)
        {
            this.SetData(Site.ParseUrlList(fieldNode.InnerXml));
        }

        public string GetRawXml()
        {
            return Site.UrlListToString((Dictionary<string, string>)GetData());
        }
        protected override void WriteXmlData(System.Xml.XmlWriter writer)
        {
            writer.WriteRaw(GetRawXml());
        }
    }
}