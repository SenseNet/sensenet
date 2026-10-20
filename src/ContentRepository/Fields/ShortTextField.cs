using System;
using System.Collections.Generic;
using System.Text;

using  SenseNet.ContentRepository.Schema;

namespace SenseNet.ContentRepository.Fields
{
	[ShortName("ShortText")]
	[DataSlot(0, RepositoryDataType.String, typeof(string))]
	[DefaultFieldSetting(typeof(ShortTextFieldSetting))]
	[DefaultFieldControl("SenseNet.Portal.UI.Controls.ShortText")]
	public class ShortTextField : Field
	{
        public override object GetData(bool localized)
        {
            // Row identifiers may change after the first save or when selecting another version.
            // Read their computed bindings instead of retaining a previous value in the field cache.
            if (Content.ContentHandler is GenericContent && FieldSetting.Bindings?.Count == 1 &&
                FieldSetting.Bindings[0] is nameof(GenericContent.NodeGuidText) or nameof(GenericContent.VersionGuidText))
                return ReadProperties();

            return base.GetData(localized);
        }

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
	}
}
