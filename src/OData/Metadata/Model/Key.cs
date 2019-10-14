using System.IO;

namespace SenseNet.OData.Metadata.Model
{
    public class Key : SchemaItem
    {
        public static Key IdKey = new Key { PropertyRef = new PropertyRef { Name = "Id" } };

        public PropertyRef PropertyRef;

        public override void WriteXml(TextWriter writer)
        {
            writer.Write("        <Key>");
            PropertyRef?.WriteXml(writer);
            writer.WriteLine("</Key>");
        }
    }
}
