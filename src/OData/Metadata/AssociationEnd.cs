using System.IO;

namespace SenseNet.OData.Metadata
{
    public class AssociationEnd : SchemaItem
    {
        public string Type;
        public string Role;
        public string Multiplicity; // 0..1, 1, *

        public override void WriteXml(TextWriter writer)
        {
            writer.Write("        <End");
            WriteAttribute(writer, "Type", Type);
            WriteAttribute(writer, "Role", Role);
            WriteAttribute(writer, "Multiplicity", Multiplicity);
            writer.WriteLine("/>");
        }
    }
}
