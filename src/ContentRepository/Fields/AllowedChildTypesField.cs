using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository.Schema;
using SenseNet.Diagnostics;

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
            var ctList = new List<ContentType>();
            List<string> missingTypes = null;

            foreach (var contentTypeName in x)
            {
                var ctName = contentTypeName.Trim();
                var ct = ContentType.GetByName(ctName);
                if (ct != null)
                {
                    ctList.Add(ct);
                }
                else
                {
                    // allocate this list only if necessary
                    if (missingTypes == null)
                        missingTypes = new List<string>();

                    missingTypes.Add(ctName);
                }
            }

            if (missingTypes != null && missingTypes.Any())
            {
                SnLog.WriteWarning("Missing Content Types in an Allowed child types field.", 
                    properties: new Dictionary<string, object>
                    {
                        { "Missing types: ", string.Join(", ", missingTypes.Distinct())},
                        { "Content", this.Content.Path }
                    });
            }

            SetData(ctList.ToArray());
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
