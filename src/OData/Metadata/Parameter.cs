using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace SenseNet.OData.Metadata
{
    public class Parameter : NamedItem
    {
        public string Type;
        public string Mode; // In, Out, InOut

        public override void WriteXml(TextWriter writer)
        {
            writer.Write("          <Parameter");
            WriteAttribute(writer, "Name", Name);
            WriteAttribute(writer, "Type", Type);
            if (Mode != null)
                WriteAttribute(writer, "Mode", Mode);
            writer.WriteLine("/>");
        }
    }
}
