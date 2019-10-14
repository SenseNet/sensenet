using System.Collections.Generic;
using System.IO;

namespace SenseNet.OData.Metadata.Model
{
    public class Schema : SchemaItem
    {
        public List<EntityType> EntityTypes;
        public List<ComplexType> ComplexTypes;
        public List<EnumType> EnumTypes;
        public List<Association> Associations;
        public EntityContainer EntityContainer;

        public override void WriteXml(TextWriter writer)
        {
            writer.Write(@"    <Schema Namespace=""");
            writer.Write(MetaGenerator.schemaNamespace);
            writer.WriteLine(@""" xmlns:d=""http://schemas.microsoft.com/ado/2007/08/dataservices"" xmlns:m=""http://schemas.microsoft.com/ado/2007/08/dataservices/metadata"" xmlns=""http://schemas.microsoft.com/ado/2007/05/edm"">");

            WriteCollectionXml(writer, EntityTypes);
            WriteCollectionXml(writer, ComplexTypes);
            WriteCollectionXml(writer, EnumTypes);
            WriteCollectionXml(writer, Associations);
            EntityContainer?.WriteXml(writer);

            writer.WriteLine("    </Schema>");
        }

    }
}
