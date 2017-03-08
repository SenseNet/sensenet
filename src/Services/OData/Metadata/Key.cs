using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace SenseNet.Portal.OData.Metadata
{
    public class Key : SchemaItem
    {
        public static Key IdKey = new Key { PropertyRef = new PropertyRef { Name = "Id" } };

        public PropertyRef PropertyRef;

        public override void WriteXml(TextWriter writer)
        {
            writer.Write("        <Key>");
            if (PropertyRef != null)
                PropertyRef.WriteXml(writer);
            writer.WriteLine("</Key>");
        }
    }
}
