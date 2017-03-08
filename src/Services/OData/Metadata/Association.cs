using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace SenseNet.Portal.OData.Metadata
{
    public class Association : NamedItem
    {
        public AssociationEnd End1;
        public AssociationEnd End2;

        public override void WriteXml(TextWriter writer)
        {
            writer.Write("      <Association");
            WriteAttribute(writer, "Name", Name);
            writer.WriteLine(">");

            End1.WriteXml(writer);
            End2.WriteXml(writer);

            writer.WriteLine("      </Association>");
        }
    }
}
