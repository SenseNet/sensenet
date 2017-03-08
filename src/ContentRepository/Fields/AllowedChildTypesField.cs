using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository.Schema;

namespace SenseNet.ContentRepository.Fields
{
    [ShortName("AllowedChildTypes")]
    [DataSlot(0, RepositoryDataType.Text, typeof(IEnumerable<ContentType>))]
    [DefaultFieldSetting(typeof(NullFieldSetting))]
    [DefaultFieldControl("SenseNet.Portal.UI.Controls.AllowedChildTypes")]
    [FieldDataType(typeof(IEnumerable<ContentType>))]
    public class AllowedChildTypesField : Field
    {
        protected override bool HasExportData
        {
            get
            {
                var data = (IEnumerable<ContentType>)GetData();
                return data != null && data.Count() > 0;
            }
        }
        protected override void ExportData(System.Xml.XmlWriter writer, ExportContext context)
        {
            WriteXmlData(writer);
        }
        protected override void ImportData(System.Xml.XmlNode fieldNode, ImportContext context)
        {
            ParseValue(fieldNode.InnerText);
        }
        protected override bool ParseValue(string value)
        {
            var x = value.Split(ContentType.XmlListSeparators, StringSplitOptions.RemoveEmptyEntries);
            var ctList = new ContentType[x.Length];
            for (int i = 0; i < x.Length; i++)
			{
                var ct = ContentType.GetByName(x[i].Trim());
                if (ct != null)
                    ctList[i] = ct;
			}
            SetData(ctList);
            return true;
        }

        protected override string GetXmlData()
        {
            var data = (IEnumerable<ContentType>)GetData();
            return String.Join(" ", data.Select(s => s.Name));
        }
        protected override void WriteXmlData(System.Xml.XmlWriter writer)
        {
            writer.WriteString(GetXmlData());
        }
    }
}
