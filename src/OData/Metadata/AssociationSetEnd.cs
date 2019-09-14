using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace SenseNet.OData.Metadata
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
