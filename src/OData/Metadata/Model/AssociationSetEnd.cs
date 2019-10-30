using System.IO;

namespace SenseNet.OData.Metadata.Model
{
    public class AssociationSetEnd : SchemaItem
    {
        public string Role;
        public string EntitySet;

        public override void WriteXml(TextWriter writer)
        {
            writer.Write("          <End");
            WriteAttribute(writer, "Role", Role);
            WriteAttribute(writer, "EntitySet", EntitySet);
            writer.WriteLine("/>");
        }
    }
}
