using System.IO;

namespace SenseNet.OData.Metadata.Model
{
    public class PropertyRef : NamedItem
    {
        public override void WriteXml(TextWriter writer)
        {
            writer.Write("<PropertyRef Name=\"");
            writer.Write(Name);
            writer.Write("\"/>");
        }
    }
}
