using System.Collections.Generic;
using System.IO;

namespace SenseNet.OData.Metadata.Model
{
    public class ComplexType : NamedItem
    {
        public List<Property> Properties;

        public override void WriteXml(TextWriter writer)
        {
            writer.Write("      <ComplexType");
            WriteAttribute(writer, "Name", Name);
            writer.WriteLine(">");

            WriteCollectionXml(writer, Properties);

            writer.WriteLine("      </ComplexType>");
        }
    }
}
