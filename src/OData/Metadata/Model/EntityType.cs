using System.Collections.Generic;
using System.IO;

namespace SenseNet.OData.Metadata.Model
{
    public class EntityType : ComplexType
    {
        public string BaseType;
        public bool? HasStream;
        public Key Key;
        public List<NavigationProperty> NavigationProperties;

        public override void WriteXml(TextWriter writer)
        {
            writer.Write("      <EntityType");
            WriteAttribute(writer, "Name", Name);
            if (BaseType != null)
                WriteAttribute(writer, "BaseType", BaseType);
            if (HasStream.HasValue)
                WriteAttribute(writer, "HasStream", HasStream.Value.ToString().ToLower());
            writer.WriteLine(">");

            Key?.WriteXml(writer);

            WriteCollectionXml(writer, Properties);
            WriteCollectionXml(writer, NavigationProperties);

            writer.WriteLine("      </EntityType>");
        }
    }
}
