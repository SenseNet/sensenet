using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace SenseNet.OData.Metadata
{
    public class EntitySet : NamedItem
    {
        public string EntityType; // "Self.Product"

        public override void WriteXml(TextWriter writer)
        {
            writer.Write("        <EntitySet");
            WriteAttribute(writer, "Name", Name);
            WriteAttribute(writer, "EntityType", EntityType);
            writer.WriteLine("/>");
        }
    }
}
